using System.Net.Http.Json;
using TicketSalesPlatform.Orders.Application.Clients;

namespace TicketSalesPlatform.Orders.Infrastructure.Clients
{
    public class EventsClient : IEventsClient
    {
        private readonly HttpClient _httpClient;

        public EventsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TicketTypeDto?> GetTicketTypeAsync(
            Guid ticketTypeId,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<TicketTypeDto>(
                    $"api/events/ticket-types/{ticketTypeId}",
                    cancellationToken
                );
                return response;
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error communicating with Event Service: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<TicketTypeDto>> GetTicketTypesBulkAsync(
            IEnumerable<Guid> ticketTypeIds,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "api/events/ticket-types/batch",
                    ticketTypeIds,
                    cancellationToken
                );

                response.EnsureSuccessStatusCode();

                var ticketTypes = await response.Content.ReadFromJsonAsync<IEnumerable<TicketTypeDto>>(cancellationToken: cancellationToken);
                return ticketTypes ?? Array.Empty<TicketTypeDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error communicating with Event Service bulk endpoint: {ex.Message}", ex);
            }
        }
    }
}
