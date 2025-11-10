using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GW.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class CustomHeaderDelegatingHandler : DelegatingHandler
{
   private const string TraceHeader = "X-Trace-Id";
   private readonly ILogger<CustomHeaderDelegatingHandler> _logger;
   private readonly IHttpContextAccessor _httpContextAccessor;

   public CustomHeaderDelegatingHandler(
       ILogger<CustomHeaderDelegatingHandler> logger,
       IHttpContextAccessor httpContextAccessor)
   {
      _logger = logger;
      _httpContextAccessor = httpContextAccessor;
   }

   protected override async Task<HttpResponseMessage> SendAsync(
       HttpRequestMessage request, CancellationToken cancellationToken)
   {
      var httpContext = _httpContextAccessor.HttpContext!;
      var traceId = httpContext.TraceIdentifier;

      if (!request.Headers.Contains(TraceHeader))
         request.Headers.Add(TraceHeader, traceId);
      if (!request.Headers.Contains(Auth.ClientHeaderName))
         request.Headers.Add(Auth.ClientHeaderName, "User");


      var stopwatch = Stopwatch.StartNew();
      _logger.LogInformation("Request ▶ {Method} {Path} TraceId={TraceId}",
          request.Method, request.RequestUri, traceId);

      var response = await base.SendAsync(request, cancellationToken);

      stopwatch.Stop();
      _logger.LogInformation(
          "Response ◀ {StatusCode} for {Method} {Path} TraceId={TraceId} Duration={Ms}ms",
          (int)response.StatusCode, request.Method, request.RequestUri,
          traceId, stopwatch.ElapsedMilliseconds);

      if (!response.Headers.Contains(TraceHeader))
         response.Headers.Add(TraceHeader, traceId);
      return response;
   }
}