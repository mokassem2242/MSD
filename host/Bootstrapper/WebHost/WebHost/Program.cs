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
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Order.Api.Controllers.OrdersController).Assembly)
    // Add other API assemblies as they're implemented
    // .AddApplicationPart(typeof(Payment.Api.Controllers.PaymentsController).Assembly)
    // .AddApplicationPart(typeof(Inventory.Api.Controllers.InventoryController).Assembly)
    ;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Event-Driven Order Processing System API",
        Description = "Unified API Gateway for all services in the Event-Driven Order Processing System. " +
                      "This gateway aggregates APIs from Order, Payment, Inventory, and other services.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Gateway",
            Email = "support@ordersystem.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    // Include XML comments from all API projects
    var xmlFiles = new[]
    {
        "Order.Api.xml",
        // Add other API XML files as they're implemented
        // "Payment.Api.xml",
        // "Inventory.Api.xml"
    };

    foreach (var xmlFile in xmlFiles)
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }

    // Use fully qualified names to avoid conflicts
    options.CustomSchemaIds(type => type.FullName);
    
    // Tag controllers by service name for better organization
    options.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        var serviceName = api.GroupName ?? "Default";
        
        // Extract service name from namespace (e.g., "Order.Api" -> "Order")
        if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controller)
        {
            var namespaceParts = controller.ControllerTypeInfo.Namespace?.Split('.');
            if (namespaceParts != null && namespaceParts.Length > 0)
            {
                serviceName = namespaceParts[0]; // First part is usually the service name
            }
        }
        
        return new[] { $"{serviceName} Service" };
    });
});

// ============================================
// ORDER SERVICE CONFIGURATION
// ============================================

// Get connection string from configuration
var orderDbConnectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Connection string 'OrderDb' not found.");

// Register DbContextOptions for OrderDbContext
builder.Services.AddSingleton<DbContextOptions<OrderDbContext>>(serviceProvider =>
{
    var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
    optionsBuilder.UseSqlServer(orderDbConnectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("Order.Infrastructure");
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }
    
    return optionsBuilder.Options;
});

// Register OrderDbContext with DomainEventDispatcher
builder.Services.AddScoped<OrderDbContext>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<DbContextOptions<OrderDbContext>>();
    var dispatcher = serviceProvider.GetRequiredService<DomainEventDispatcher>();
    return new OrderDbContext(options, dispatcher);
});

// Event Bus (Shared across all services)
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Domain Event Dispatcher
builder.Services.AddScoped<DomainEventDispatcher>();

// Order Service - Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Order Service - Command Handlers
builder.Services.AddScoped<CreateOrderCommandHandler>();
builder.Services.AddScoped<CancelOrderCommandHandler>();

// Order Service - Query Handlers
builder.Services.AddScoped<GetOrderByIdQueryHandler>();
builder.Services.AddScoped<GetOrdersQueryHandler>();

// Order Service - Database Seeder
builder.Services.AddScoped<OrderDbSeeder>();

// ============================================
// PAYMENT SERVICE CONFIGURATION
// ============================================
// TODO: Add Payment service configuration when implemented
// builder.Services.AddDbContext<PaymentDbContext>(...);
// builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
// etc.

// ============================================
// INVENTORY SERVICE CONFIGURATION
// ============================================
// TODO: Add Inventory service configuration when implemented
// builder.Services.AddDbContext<InventoryDbContext>(...);
// builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
// etc.

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Event-Driven Order Processing System API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableValidator();
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    
    // Group by service tags
    options.DefaultModelsExpandDepth(-1);
});

app.UseHttpsRedirection();

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Map OpenAPI endpoint
app.MapOpenApi();

// Map controllers from all services
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health")
    .WithName("HealthCheck");

// Ensure database is created and seed data (only in Development)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed initial data
        var seeder = scope.ServiceProvider.GetRequiredService<OrderDbSeeder>();
        await seeder.SeedAsync();
    }
}

app.Run();
