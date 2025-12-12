using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AppBackend.ApiCore.Swagger;

/// <summary>
/// Operation filter to properly handle file upload parameters in Swagger UI
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || 
                       p.ParameterType == typeof(IEnumerable<IFormFile>) ||
                       p.ParameterType == typeof(List<IFormFile>))
            .ToList();

        if (!fileParameters.Any())
            return;

        // Check if this endpoint consumes multipart/form-data
        var consumesMultipart = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Mvc.ConsumesAttribute>()
            .Any(attr => attr.ContentTypes.Contains("multipart/form-data"));

        if (!consumesMultipart)
            return;

        // Clear existing parameters
        operation.Parameters?.Clear();

        // Create proper request body for multipart/form-data
        if (operation.RequestBody == null)
        {
            operation.RequestBody = new OpenApiRequestBody();
        }

        var properties = new Dictionary<string, OpenApiSchema>();

        // Add all FromForm parameters
        foreach (var parameter in context.MethodInfo.GetParameters())
        {
            var fromFormAttr = parameter.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any();
            
            if (!fromFormAttr)
                continue;

            if (parameter.ParameterType == typeof(IFormFile))
            {
                // File parameter
                properties.Add(parameter.Name!, new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = $"Upload file (parameter: {parameter.Name})"
                });
            }
            else if (parameter.ParameterType == typeof(IEnumerable<IFormFile>) || 
                     parameter.ParameterType == typeof(List<IFormFile>))
            {
                // Multiple files parameter
                properties.Add(parameter.Name!, new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    },
                    Description = $"Upload multiple files (parameter: {parameter.Name})"
                });
            }
            else
            {
                // Other form parameters (int, string, etc.)
                var schema = new OpenApiSchema();
                
                if (parameter.ParameterType == typeof(int) || parameter.ParameterType == typeof(int?))
                {
                    schema.Type = "integer";
                    schema.Format = "int32";
                }
                else if (parameter.ParameterType == typeof(long) || parameter.ParameterType == typeof(long?))
                {
                    schema.Type = "integer";
                    schema.Format = "int64";
                }
                else if (parameter.ParameterType == typeof(bool) || parameter.ParameterType == typeof(bool?))
                {
                    schema.Type = "boolean";
                }
                else if (parameter.ParameterType == typeof(decimal) || parameter.ParameterType == typeof(decimal?))
                {
                    schema.Type = "number";
                    schema.Format = "decimal";
                }
                else if (parameter.ParameterType == typeof(double) || parameter.ParameterType == typeof(double?))
                {
                    schema.Type = "number";
                    schema.Format = "double";
                }
                else
                {
                    schema.Type = "string";
                }

                // Check if parameter is optional (nullable or has default value)
                var isOptional = Nullable.GetUnderlyingType(parameter.ParameterType) != null || 
                                parameter.HasDefaultValue;
                
                if (!isOptional)
                {
                    schema.Nullable = false;
                }

                schema.Description = parameter.Name;
                properties.Add(parameter.Name!, schema);
            }
        }

        operation.RequestBody.Content = new Dictionary<string, OpenApiMediaType>
        {
            ["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = properties,
                    Required = new HashSet<string>(
                        context.MethodInfo.GetParameters()
                            .Where(p => 
                            {
                                var fromFormAttr = p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.FromFormAttribute), false).Any();
                                if (!fromFormAttr) return false;
                                
                                var isOptional = Nullable.GetUnderlyingType(p.ParameterType) != null || 
                                               p.HasDefaultValue;
                                return !isOptional;
                            })
                            .Select(p => p.Name!)
                    )
                }
            }
        };
    }
}
