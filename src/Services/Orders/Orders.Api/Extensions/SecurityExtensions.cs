namespace TicketSalesPlatform.Orders.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment
        )
        {
            services
                .AddAuthentication("Bearer")
                .AddJwtBearer(
                    "Bearer",
                    options =>
                    {
                        options.Authority = configuration["Authentication:Authority"];
                        options.Audience = "orders";

                        if (environment.IsDevelopment())
                        {
                            options.RequireHttpsMetadata = false;
                        }
                    }
                );

            services.AddAuthorization();

            return services;
        }
    }
}
