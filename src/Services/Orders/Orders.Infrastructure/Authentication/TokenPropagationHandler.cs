using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace TicketSalesPlatform.Orders.Infrastructure.Authentication
{
    /// <summary>
    /// An HttpMessageHandler that intercepts outgoing requests and propagates the
    /// Authorization header from the incoming request.
    /// </summary>
    public class TokenPropagationHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenPropagationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            // Get the Authorization header from the current incoming request.
            var authorizationHeader = _httpContextAccessor
                .HttpContext?.Request.Headers["Authorization"]
                .ToString();

            // If the header exists, add it to the outgoing request.
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(
                    authorizationHeader
                );
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
