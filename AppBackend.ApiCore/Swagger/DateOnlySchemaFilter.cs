using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AppBackend.ApiCore.Swagger
{
    /// <summary>
    /// Swagger schema filter to properly display DateOnly as string format in Swagger UI
    /// </summary>
    public class DateOnlySchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(DateOnly) || context.Type == typeof(DateOnly?))
            {
                schema.Type = "string";
                schema.Format = "date";
                schema.Example = new Microsoft.OpenApi.Any.OpenApiString("2025-06-19");
                schema.Description = "Date in ISO 8601 format (YYYY-MM-DD). Example: '2025-06-19'";
            }
        }
    }
}
