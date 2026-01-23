using BuildingBlocks.EventBus;
using Microsoft.EntityFrameworkCore;
using Order.Application.Handlers;
using Order.Application.Ports;
using Order.Infrastructure.DomainEvents;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Order Service API",
        Description = "API for managing customer orders in the Event-Driven Order Processing System. " +
                      "This service handles order creation, retrieval, and cancellation, and orchestrates " +
                      "the order saga through event-driven communication with Payment and Inventory services.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Order Service",
            Email = "support@orderservice.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Use fully qualified names to avoid conflicts
    options.CustomSchemaIds(type => type.FullName);
});

// Database - Register DbContextOptions
builder.Services.AddSingleton<DbContextOptions<OrderDbContext>>(serviceProvider =>
{
    var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
    optionsBuilder.UseInMemoryDatabase("OrderDb");
    return optionsBuilder.Options;
});

// Register OrderDbContext with DomainEventDispatcher
builder.Services.AddScoped<OrderDbContext>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<DbContextOptions<OrderDbContext>>();
    var dispatcher = serviceProvider.GetRequiredService<DomainEventDispatcher>();
    return new OrderDbContext(options, dispatcher);
});

// Event Bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Domain Event Dispatcher
builder.Services.AddScoped<DomainEventDispatcher>();

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Command Handlers
builder.Services.AddScoped<CreateOrderCommandHandler>();
builder.Services.AddScoped<CancelOrderCommandHandler>();

// Query Handlers
builder.Services.AddScoped<GetOrderByIdQueryHandler>();
builder.Services.AddScoped<GetOrdersQueryHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableValidator();
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseHttpsRedirection();

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Map OpenAPI endpoint
app.MapOpenApi();

// Map controllers
app.MapControllers();

app.Run();
