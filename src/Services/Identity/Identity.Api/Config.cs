using Duende.IdentityServer.Models;

namespace TicketSalesPlatform.Identity.Api;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[] { new IdentityResources.OpenId(), new IdentityResources.Profile() };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            // Define a scope for each of our APIs. A scope is a permission that a client can request.
            new ApiScope("events.api", "Events API"),
            new ApiScope("orders.api", "Orders API"),
            new ApiScope("inventory.api", "Inventory API"),
            new ApiScope("payments.api", "Payments API"),
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            // Define our APIs as resources that can be protected.
            new ApiResource("events", "Events Service API") { Scopes = { "events.api" } },
            new ApiResource("orders", "Orders Service API") { Scopes = { "orders.api" } },
            new ApiResource("inventory", "Inventory Service API") { Scopes = { "inventory.api" } },
            new ApiResource("payments", "Payments Service API") { Scopes = { "payments.api" } },
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Define a client for testing purposes (e.g., for Postman or Swagger).
            // This client will use the "client credentials" flow, which is for machine-to-machine communication.
            new Client
            {
                ClientId = "test.client",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.ClientCredentials,

                // Scopes that client has access to
                AllowedScopes = { "events.api", "orders.api", "inventory.api", "payments.api" },
            },
        };
}
