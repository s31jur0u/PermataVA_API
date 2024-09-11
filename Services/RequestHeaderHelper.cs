using Azure.Core;
using Microsoft.AspNetCore.Mvc;
public class RequestHeaderHelper()
{
    public static HttpHeader GetHeader(HttpRequest request)
    {
        HttpHeader result = new();

        result.xTimestamp = request.Headers["X-TIMESTAMP"];
        result.xClientKey = request.Headers["X-CLIENT-KEY"];
        result.xSignature = request.Headers["X-SIGNATURE"];
        result.xPartnerId = request.Headers["X-PARTNER-ID"];
        result.xExternalId = request.Headers["X-EXTERNAL-ID"];
        result.channelId = request.Headers["CHANNEL-ID"];
        return result;
    }
}