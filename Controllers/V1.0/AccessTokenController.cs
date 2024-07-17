using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text.Encodings.Web;

[Route("v1.0/access-token")]
public class AccessTokenController : ControllerBase
{
    private readonly IJwtTokenGeneratorService _jwtTokenGeneratorService;
    private readonly IConfiguration _config;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    public AccessTokenController(IJwtTokenGeneratorService jwtTokenGeneratorService, IConfiguration config, ISqlConnectionFactory sqlConnectionFactory)
    {
        _jwtTokenGeneratorService = jwtTokenGeneratorService;
        _config = config;
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    [HttpPost("[action]")]
    public IActionResult B2B([FromBody] B2BRequest request)
    {
        HTTPHeader headers = RequestHeaderHelper.GetHeader(Request);
        bool ok = false;

        string token = string.Empty;
        int expiry_minutes = 0;
        int.TryParse(_config["TOKEN:EXPIRY"], out expiry_minutes);

        string client_id = _config["CLIENT_ID"];
        RSA public_key = RSAKeyExtractor.GetPublicKey(_config["PUBLIC_KEY"]);
        AccessTokenResponse success_response = new();
        APIBaseResponse response = new();

        response.responseCode = "4012400";
        response.responseMessage = "Unauthorized Signature";
        if (client_id == headers.X_CLIENT_KEY)
        {

            string tosign = string.Concat(client_id, "|", headers.X_TIMESTAMP);

            if (true)
            // if (SignatureVerifier.VerifySignatureSHA256(tosign, public_key, headers.X_SIGNATURE))
            {

                token = _jwtTokenGeneratorService.GenerateJwtToken(expiry_minutes);
                success_response.accessToken = token;
                success_response.responseCode = "2000100";
                success_response.responseMessage = "Successful";
                success_response.tokenType = "Bearer";
                success_response.expiresIn = (expiry_minutes * 60).ToString();
                ok = true;
            }
            else
            {
                ok = false;
                response.responseCode = "4012400";
                response.responseMessage = "Unauthorized Signature";
            }
        }
        return ok ? Ok(success_response) : BadRequest(response);

    }
    [HttpPost("[action]")]
    public IActionResult GetB2BSignature()
    {
        HTTPHeader headers = RequestHeaderHelper.GetHeader(Request);
        string client_id = _config["CLIENT_ID"];
        RSA public_key = RSAKeyExtractor.GetPublicKey(_config["PUBLIC_KEY"]);
        RSA PRIVATE_KEY = RSAKeyExtractor.GetPrivateKey(_config["PRIVATE_KEY"]);
        string tosign = string.Concat(client_id, "|", headers.X_TIMESTAMP);
        return Ok(SignatureVerifier.CreateSignatureSHA256(tosign, PRIVATE_KEY));
    }

    [HttpPost("[action]")]
    public IActionResult VerifyB2BSignature()
    {
        HTTPHeader headers = RequestHeaderHelper.GetHeader(Request);
        string client_id = _config["CLIENT_ID"];
        RSA public_key = RSAKeyExtractor.GetPublicKey(_config["PUBLIC_KEY"]);
        RSA PRIVATE_KEY = RSAKeyExtractor.GetPrivateKey(_config["PRIVATE_KEY"]);
        string tosign = string.Concat(client_id, "|", headers.X_TIMESTAMP);
        string signeddata = SignatureVerifier.CreateSignatureSHA256(tosign, PRIVATE_KEY);
        return Ok(new { OK = SignatureVerifier.VerifySignatureSHA256(tosign, public_key, signeddata), SignedData = signeddata });
    }
}