using MassTransit;
using Microsoft.EntityFrameworkCore;
using TicketSalesPlatform.Payments.Api.Data;
using TicketSalesPlatform.Payments.Api.Endpoints;
using TicketSalesPlatform.Payments.Api.Infrastructure.Authentication;
using TicketSalesPlatform.Payments.Api.Infrastructure.Clients.Order;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder
    .Services.AddAuthentication("Bearer")
    .AddJwtBearer(
        "Bearer",
        options =>
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = "payments";
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );

builder.Services.AddAuthorization();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var connectionString = builder.Configuration.GetConnectionString("Database");
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payment", false));

    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq(
        (context, cfg) =>
        {
            var host = builder.Configuration["MessageBroker:Host"];
            cfg.Host(
                host,
                "/",
                h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                }
            );

            cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            cfg.ConfigureEndpoints(context);
        }
    );
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<TokenPropagationHandler>();
builder
    .Services.AddHttpClient<IOrderClient, OrderClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:OrderApiUrl"]!);
    })
    .AddHttpMessageHandler<TokenPropagationHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapPaymentEndpoints();

app.Run();
