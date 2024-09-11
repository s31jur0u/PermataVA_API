using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;

namespace VA_API.Controllers.V1._0;

[Route("v1.0/access-token/[action]")]
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

    [HttpPost("")]
    public IActionResult B2B([FromBody] B2BRequest request)
    {
        HttpHeader headers = RequestHeaderHelper.GetHeader(Request);
        bool ok = false;

        string token;
        int expiryMinutes = 0;
        int.TryParse(_config["TOKEN:EXPIRY"], out expiryMinutes);

        string? clientId = _config["CLIENT_ID"];
        RSA publicKey = RsaKeyExtractor.GetPublicKey(_config["PUBLIC_KEY"]);
        AccessTokenResponse successResponse = new();
        ApiBaseResponse response = new();

        response.responseCode = "4012400";
        response.responseMessage = "Unauthorized Signature";
        if (clientId == headers.xClientKey)
        {

            string tosign = string.Concat(clientId, "|", headers.xTimestamp);

            if (true)
                // if (SignatureVerifier.VerifySignatureSHA256(tosign, public_key, headers.X_SIGNATURE))
            {

                token = _jwtTokenGeneratorService.GenerateJwtToken(expiryMinutes);
                successResponse.accessToken = token;
                successResponse.responseCode = "2000100";
                successResponse.responseMessage = "Successful";
                successResponse.tokenType = "Bearer";
                successResponse.expiresIn = (expiryMinutes * 60).ToString();
                ok = true;
            }
            else
            {
                ok = false;
                response.responseCode = "4012400";
                response.responseMessage = "Unauthorized Signature";
            }
        }
        return ok ? Ok(successResponse) : BadRequest(response);

    }
    
    [HttpPost("")]
    public IActionResult GetB2BSignature()
    {
        HttpHeader headers = RequestHeaderHelper.GetHeader(Request);
        string clientId = _config["CLIENT_ID"];
        RSA publicKey = RsaKeyExtractor.GetPublicKey(_config["PUBLIC_KEY"]);
        RSA privateKey = RsaKeyExtractor.GetPrivateKey(_config["PRIVATE_KEY"]);
        string tosign = string.Concat(clientId, "|", headers.xTimestamp);
        return Ok(SignatureVerifier.CreateSignatureSha256(tosign, privateKey));
    }

    [HttpPost("")]
    public IActionResult VerifyB2BSignature()
    {
        HttpHeader headers = RequestHeaderHelper.GetHeader(Request);
        string? clientId = _config["CLIENT_ID"];
        RSA publicKey = RsaKeyExtractor.GetPublicKey(_config["PUBLIC_KEY"]);
        RSA privateKey = RsaKeyExtractor.GetPrivateKey(_config["PRIVATE_KEY"]);
        string tosign = string.Concat(clientId, "|", headers.xTimestamp);
        string signeddata = SignatureVerifier.CreateSignatureSha256(tosign, privateKey);
        return Ok(new { OK = SignatureVerifier.VerifySignatureSha256(tosign, publicKey, signeddata), SignedData = signeddata });
    }
}