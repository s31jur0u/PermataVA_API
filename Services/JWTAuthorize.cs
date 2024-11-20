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
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtAuthorizeAttribute>>();

        // Check if Authorization header is present
        if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            logger.LogWarning("Authorization header not found.");
            context.Result = new UnauthorizedResult();
            return;
        }

        var bearerToken = authHeaderValues.FirstOrDefault()?.Split(" ").LastOrDefault();
        if (string.IsNullOrEmpty(bearerToken))
        {
            logger.LogWarning("Bearer token not found.");
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
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
                ValidateLifetime = false
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
            ApiBaseResponse response = new(){
                responseCode="4012401",
                responseMessage="Unauthorized"
            };
            context.Result = new OkObjectResult(response);
        }
    }
}
