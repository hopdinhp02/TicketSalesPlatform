using SharedKernel.Extensions;
using TicketSalesPlatform.Payments.Api.Data;
using TicketSalesPlatform.Payments.Api.Endpoints;
using TicketSalesPlatform.Payments.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddPresentationServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddSecurityServices(builder.Configuration, builder.Environment)
    .AddExternalClients(builder.Configuration);

builder.AddObservability(builder.Environment.ApplicationName);

var app = builder.Build();

app.UseObservability();

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
