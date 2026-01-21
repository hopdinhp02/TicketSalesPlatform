using SharedKernel.Extensions;
using TicketSalesPlatform.Events.Api.Endpoints;
using TicketSalesPlatform.Events.Api.Extensions;

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
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapEventEndpoints();

app.Run();
