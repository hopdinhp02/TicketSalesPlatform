namespace TicketSalesPlatform.Payments.Api.Infrastructure.Clients.Order
{
    public class OrderClient : IOrderClient
    {
        private readonly HttpClient _httpClient;

        public OrderClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OrderDto?> GetOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<OrderDto>(
                    $"api/orders/{orderId}",
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
    }
}
