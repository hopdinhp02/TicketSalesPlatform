using System.Net.Http.Json;
using TicketSalesPlatform.Orders.Application.Clients;

namespace TicketSalesPlatform.Orders.Infrastructure.Clients
{
    public class InventoryClient : IInventoryClient
    {
        private readonly HttpClient _httpClient;

        public InventoryClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> CheckStockAsync(
            Guid ticketTypeId,
            int quantity,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<InventoryAvailabilityDto>(
                    $"api/inventory/ticket-types/{ticketTypeId}/availability",
                    cancellationToken
                );

                return response != null && response.AvailableQuantity >= quantity;
            }
            catch (HttpRequestException ex)
                when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal record InventoryAvailabilityDto(Guid TicketTypeId, int AvailableQuantity);
    }
}
