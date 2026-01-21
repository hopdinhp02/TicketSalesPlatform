using SharedKernel.Extensions;
using TicketSalesPlatform.Orders.Api.Endpoints;
using TicketSalesPlatform.Orders.Api.Extensions;

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
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapOrderEndpoints();

app.Run();
