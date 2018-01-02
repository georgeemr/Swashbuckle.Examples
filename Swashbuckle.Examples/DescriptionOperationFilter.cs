using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Http.Description;
using Swashbuckle.Swagger;
using Swashbuckle.Swagger.Annotations;
using System.Reflection;
using Newtonsoft.Json;

namespace Swashbuckle.Examples
{
    public class DescriptionOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            SetResponseModelDescriptions(operation, schemaRegistry, apiDescription);
        }

        private static void SetResponseModelDescriptions(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var responseAttributes = apiDescription.GetControllerAndActionAttributes<SwaggerResponseAttribute>();

            foreach (var attr in responseAttributes)
            {
                if (attr.Type == null)
                {
                    continue;
                }

                var statusCode = attr.StatusCode.ToString();

                var response = operation.responses.FirstOrDefault(r => r.Key == statusCode);

                if (response.Equals(default(KeyValuePair<string, Response>)) == false)
                {
                    if (response.Value != null)
                    {
                        if (schemaRegistry.Definitions.ContainsKey(attr.Type.Name))
                        {
                            RecursivelyParseDescriptions(schemaRegistry, attr.Type);
                        }
                    }
                }
            }
        }

        private static void RecursivelyParseDescriptions(SchemaRegistry schemaRegistry, Type propType)
        {
            Schema definition = schemaRegistry.Definitions[propType.Name];
            foreach (PropertyInfo propertyInfo in propType.GetProperties().Where(prop => prop.IsDefined(typeof(DescriptionAttribute), false)))
            {
                DescriptionAttribute descriptionAttribute = (DescriptionAttribute)propertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
                if (definition.properties.ContainsKey(propertyInfo.Name))
                {
                    definition.properties[propertyInfo.Name].description = descriptionAttribute.Description;
                }
                else
                {
                    var jsonPropertyAttr = (JsonPropertyAttribute)propertyInfo.GetCustomAttributes(typeof(JsonPropertyAttribute), false).FirstOrDefault();
                    if (jsonPropertyAttr != null && definition.properties.ContainsKey(jsonPropertyAttr.PropertyName))
                    {
                        definition.properties[jsonPropertyAttr.PropertyName].description = descriptionAttribute.Description;
                    }
                }
            }
            foreach (PropertyInfo propertyInfo in propType.GetProperties().Where(prop => prop.PropertyType.Assembly == propType.Assembly))
            {
                if (schemaRegistry.Definitions.ContainsKey(propertyInfo.Name))
                    RecursivelyParseDescriptions(schemaRegistry, propertyInfo.PropertyType);
            }
        }
    }
}
