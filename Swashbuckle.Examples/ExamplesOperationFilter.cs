using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.Swagger;

namespace Swashbuckle.Examples
{
    public class ExamplesOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            SetRequestModelExamples(operation, schemaRegistry, apiDescription);
            SetResponseModelExamples(operation, apiDescription);
        }

        private static void SetRequestModelExamples(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var requestAttributes = apiDescription.GetControllerAndActionAttributes<SwaggerRequestExampleAttribute>();

            foreach (var attr in requestAttributes)
            {
                var schema = schemaRegistry.GetOrRegister(attr.RequestType);

                var parameter = operation.parameters.FirstOrDefault(p => p.@in == "body" && p.schema.@ref == schema.@ref);

                if (parameter != null)
                {
                    var provider = (IExamplesProvider)Activator.CreateInstance(attr.ExamplesProviderType);

                    var parts = schema.@ref.Split('/');
                    var name = parts.Last();

                    var definitionToUpdate = schemaRegistry.Definitions[name];

                    if (definitionToUpdate != null)
                    {
                        definitionToUpdate.example = ((dynamic)FormatAsJson(provider))["application/json"];
                    }
                }
            }
        }

        private static void SetResponseModelExamples(Operation operation, ApiDescription apiDescription)
        {
            var responseAttributes = apiDescription.GetControllerAndActionAttributes<SwaggerResponseExampleAttribute>();

            foreach (var attr in responseAttributes)
            {
                var statusCode = ((int)attr.StatusCode).ToString();

                var response = operation.responses.FirstOrDefault(r => r.Key == statusCode);

                if (response.Equals(default(KeyValuePair<string, Response>)) == false)
                {
                    if (response.Value != null)
                    {
                        var provider = (IExamplesProvider)Activator.CreateInstance(attr.ExamplesProviderType);
                        response.Value.examples = FormatAsJson(provider);
                    }
                }
            }
        }

        private static object ConvertToCamelCase(Dictionary<string, object> examples)
        {
            var jsonString = JsonConvert.SerializeObject(examples, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return JsonConvert.DeserializeObject(jsonString);
        }

        private static object FormatAsJson(IExamplesProvider provider)
        {
            var examples = new Dictionary<string, object>
            {
                {
                    "application/json", provider.GetExamples()
                }
            };

            return ConvertToCamelCase(examples);
        }
    }
}