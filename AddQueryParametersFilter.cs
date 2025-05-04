using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class AddQueryParametersFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "status",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "Filter by task status (Pending, InProgress, Completed)"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "category",
            In = ParameterLocation.Query,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "Filter by category name"
        });

      
    }
}