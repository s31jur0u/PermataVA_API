using System.Text;
using Serilog;

namespace VA_API.Services;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.Core.Logger _logger;
    private readonly IConfiguration _config;

    public ApiLoggingMiddleware(RequestDelegate next, IConfiguration config)
    {
        _config = config;
        _next = next;
        _logger = new LoggerConfiguration().WriteTo.File(config["LOG:HTTP_PATH"], rollingInterval: RollingInterval.Day).CreateLogger();
        
    }


    public async Task InvokeAsync(HttpContext context)
    {
        // Log Request
        var request = await FormatRequest(context.Request);
        _logger.Information("Req Inq\n" + request);

        // Copy original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            // Process the request
            await _next(context);

            // Log Response
            var response = await FormatResponse(context.Response);
            _logger.Information("Res Inq\n" + response);

            // Copy the response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();
        var body = string.Empty;

        if (request.Body.CanRead)
        {
            using (var reader = new StreamReader(
                request.Body, Encoding.UTF8, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }
        }

        return $"{request.Method} {request.Path} {request.Protocol}\n" +
               $"{string.Join("\n", request.Headers.Select(h => $"{h.Key}: {h.Value}"))}\n" +
               $"\n{body}";
    }

    private async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        return $"{(int)response.StatusCode} {response.StatusCode}\n" +
               $"{string.Join("\n", response.Headers.Select(h => $"{h.Key}: {h.Value}"))}\n" +
               $"\n{body}";
    }
}
