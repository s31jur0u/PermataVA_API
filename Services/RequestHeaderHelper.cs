using Azure.Core;
using Microsoft.AspNetCore.Mvc;
public class RequestHeaderHelper()
{
    public static HTTPHeader GetHeader(HttpRequest request)
    {
        HTTPHeader result = new();

        result.X_TIMESTAMP = request.Headers["X-TIMESTAMP"];
        result.X_CLIENT_KEY = request.Headers["X-CLIENT-KEY"];
        result.X_SIGNATURE = request.Headers["X-SIGNATURE"];
        result.X_PARTNER_ID = request.Headers["X-PARTNER-ID"];
        result.X_EXTERNAL_ID = request.Headers["X-EXTERNAL-ID"];
        result.CHANNEL_ID = request.Headers["CHANNEL-ID"];
        return result;
    }
}