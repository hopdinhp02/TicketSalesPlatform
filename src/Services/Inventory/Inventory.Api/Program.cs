using SharedKernel.Extensions;
using TicketSalesPlatform.Inventory.Api.Data;
using TicketSalesPlatform.Inventory.Api.Endpoints;
using TicketSalesPlatform.Inventory.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddPresentationServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddSecurityServices(builder.Configuration, builder.Environment);

builder.AddObservability(builder.Environment.ApplicationName);

var app = builder.Build();

app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapInventoryEndpoints();

app.Run();
