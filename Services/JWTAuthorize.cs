using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

public class JwtAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        string action = context.ActionDescriptor.DisplayName;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtAuthorizeAttribute>>();
        ApiBaseResponse token_resp = new(){
            responseCode="401XX01",
            responseMessage="Access Token Invalid"
        };
        try
        {
            // Check if Authorization header is present
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            {
                logger.LogWarning("Authorization header not found.");
                throw new Exception();
            }

            var bearerToken = authHeaderValues.FirstOrDefault()?.Split(" ").LastOrDefault();
            if (string.IsNullOrEmpty(bearerToken))
            {
                logger.LogWarning("Bearer token not found.");
                throw new Exception();

            }


            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
                {
                    // Resolve the key from a configuration service or another source
                    var serviceProvider = context.HttpContext.RequestServices;
                    var key = serviceProvider.GetRequiredService<SecurityKey>();
                    return new[] { key };
                },
                ValidateIssuer = false, // Set to true if you want to validate the issuer
                ValidateAudience = false, // Set to true if you want to validate the audience
                ValidateLifetime = true
            };

            // Validate the token
            var principal = tokenHandler.ValidateToken(bearerToken, tokenValidationParameters, out var validatedToken);

            // Set the validated principal on the HttpContext
            context.HttpContext.User = principal;

            // Continue to the controller action
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating JWT token.");
            string service_id = "24";

      
            service_id = action.ToLower() switch
            {
                "inquiry" => "24",
                "payment" => "25",
                _ => "00",
            };
            
            token_resp.responseCode =    token_resp.responseCode.Replace("XX", service_id);
            
            context.Result = new OkObjectResult(token_resp);
        }
    }

    
}
