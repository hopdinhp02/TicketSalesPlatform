using System.Net.Http.Json;
using TicketFlow.Orders.Application.Abstractions;

namespace TicketFlow.Orders.Infrastructure.Clients
{
    public class EventsClient : IEventsClient
    {
        private readonly HttpClient _httpClient;

        public EventsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TicketAvailabilityDto?> GetTicketAvailabilityAsync(
            Guid eventId,
            Guid ticketTypeId,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var path = $"/api/events/{eventId}/tickets/{ticketTypeId}/availability";
                return await _httpClient.GetFromJsonAsync<TicketAvailabilityDto>(
                    path,
                    cancellationToken
                );
            }
            catch (HttpRequestException) // Catches non-2xx responses
            {
                // logging here.
                return null;
            }
        }
    }
}
