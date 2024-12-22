using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class ExternalIdValidatorAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public ExternalIdValidatorAttribute(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        string action = context.ActionDescriptor.DisplayName;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExternalIdValidatorAttribute>>();
        ApiBaseResponse token_resp = new(){
            responseCode="409XX00",
            responseMessage="Conflict"
        };
        try
        {
            // Check if Authorization header is present
            if (!context.HttpContext.Request.Headers.TryGetValue("X-EXTERNAL-ID", out var externalid))
            {
                logger.LogWarning("ExternalId header not found.");
                throw new Exception();
            }
            using SqlConnection sqlConnection = _sqlConnectionFactory.GetOpenConnection();
            
            using SqlCommand sqlCommand = new("SELECT * FROM [dbo].[ExternalIdLogs] WHERE ExternalId = @ExternalId", sqlConnection);
    sqlCommand.Parameters.AddWithValue("@ExternalId", externalid);
            SqlDataReader reader = sqlCommand.ExecuteReader();

            if (reader.HasRows)
            {
                throw new Exception("Conflict");
            }

          
            // Continue to the controller action
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "External Id Duplicate.");
            string service_id = "24";

      
            service_id = action.ToLower() switch
            {
                "inquiry" => "24",
                "payment" => "25",
                _ => "00",
            };
            
            token_resp.responseCode.Replace("XX", service_id);
            
            context.Result = new OkObjectResult(token_resp);
        }
    }
}
