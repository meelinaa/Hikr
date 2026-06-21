using Hikr.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();

// Add services to the container.
builder.Services.AddPresentation();
builder.Services.AddApplicationServices();
builder.Services.AddDataAccess(builder.Configuration);

// Add API services (CORS, Health Checks, Swagger, Global Exception Handler)
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseApiPipeline();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
