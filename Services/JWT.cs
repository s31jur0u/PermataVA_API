using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public interface IJwtTokenGeneratorService
{
    string GenerateJwtToken(int expiry_minutes);
}

public class JwtTokenGeneratorService : IJwtTokenGeneratorService
{
    private readonly SecurityKey _securityKey;

    private readonly IConfiguration _configuration;

    public JwtTokenGeneratorService(SecurityKey securityKey, IConfiguration configuration)
    {
        _securityKey = securityKey;
        _configuration = configuration;
    }

    public string GenerateJwtToken(int expiry_minutes)
    {
        string client_id = _configuration["CLIENT_ID"];
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("CLIENT_ID", client_id)
                // Add more claims as needed
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiry_minutes), // Token expiration time
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha512Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
