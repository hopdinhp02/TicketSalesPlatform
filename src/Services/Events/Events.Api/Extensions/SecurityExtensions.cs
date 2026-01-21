namespace TicketSalesPlatform.Events.Api.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddSecurityServices(
            this IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment env
        )
        {
            services
                .AddAuthentication("Bearer")
                .AddJwtBearer(
                    "Bearer",
                    options =>
                    {
                        options.Authority = config["Authentication:Authority"];
                        options.Audience = "events";

                        if (env.IsDevelopment())
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
