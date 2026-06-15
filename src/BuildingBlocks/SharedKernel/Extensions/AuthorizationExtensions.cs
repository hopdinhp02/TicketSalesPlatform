using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace SharedKernel.Extensions
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddCustomAuthenticationAndAuthorization(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment,
            string audience
        )
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(
                    options =>
                    {
                        options.Authority = configuration["Authentication:Authority"];
                        options.Audience = audience;
                        options.RequireHttpsMetadata = false;

                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            NameClaimType = "preferred_username",
                            RoleClaimType = "role"
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine($"[AUTH FAILED] {context.Exception.Message}");
                                if (context.Exception.InnerException != null)
                                {
                                    Console.WriteLine($"[AUTH FAILED INNER] {context.Exception.InnerException.Message}");
                                }
                                return Task.CompletedTask;
                            },
                            OnTokenValidated = context =>
                            {
                                Console.WriteLine("[AUTH SUCCESS] Token validated successfully.");
                                return Task.CompletedTask;
                            }
                        };
                    }
                );

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireOrganizerRole", policy => policy.RequireRole("Organizer", "Admin"));
                options.AddPolicy("RequireCustomerRole", policy => policy.RequireRole("Customer"));
            });

            return services;
        }

        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? principal.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var guid) ? guid : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
        public static bool IsOrganizer(this ClaimsPrincipal principal) => principal.IsInRole("Organizer") || principal.IsInRole("Admin");
        public static bool IsCustomer(this ClaimsPrincipal principal) => principal.IsInRole("Customer");
    }
}
