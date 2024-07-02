using Microsoft.Extensions.Primitives;

namespace HankoTest.SecondApi;

public class HttpTrackerHandler(IHttpContextAccessor context) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (context.HttpContext != null && context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues jwt))
            request.Headers.Add("Authorization", jwt.FirstOrDefault());

        if (context.HttpContext != null && context.HttpContext.Request.Headers.TryGetValue("TraceId", out StringValues traceid))
            request.Headers.Add("TraceId", traceid.FirstOrDefault());

        return base.SendAsync(request, cancellationToken);
    }
}