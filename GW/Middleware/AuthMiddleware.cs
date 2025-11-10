using System.Security.Claims;
using System.Text.Encodings.Web;
using GW.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;


namespace GW.Middleware;

public class AuthMiddleware : AuthenticationHandler<AuthenticationSchemeOptions>
{


   public AuthMiddleware(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
   {
   }

   protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
   {
      if (!Request.Headers.TryGetValue(Auth.ClientHeaderName, out var headerValues))
      {
         // If the header is missing, we don't authenticate but don't fail (allowing anonymous access if no policy is enforced)
         return AuthenticateResult.NoResult();
      }

      var clientTypeValue = headerValues.FirstOrDefault();

      if (string.IsNullOrWhiteSpace(clientTypeValue))
      {
         // Header is present but empty
         return AuthenticateResult.Fail($"Header '{Auth.ClientHeaderName}' value is missing.");
      }
      // 2. Create the Identity and Principal
      // This is where we create the claim based on the header value
      var claims = new[] {
            new Claim(ClaimTypes.Name, clientTypeValue),
            // The crucial claim Ocelot will check against RouteClaimsRequirement:
            new Claim(Auth.ClientClaimType, clientTypeValue, ClaimValueTypes.String, "Self")
        };

      var identity = new ClaimsIdentity(claims, Auth.ApiKeyScheme);
      var principal = new ClaimsPrincipal(identity);
      var ticket = new AuthenticationTicket(principal, Auth.ApiKeyScheme);

      // 3. Return success
      return AuthenticateResult.Success(ticket);
   }
}