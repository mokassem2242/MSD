using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Order.Domain.Aggregates.Order.Application.Handlers;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Infrastructure.DomainEvents;
using Order.Domain.Aggregates.Order.Infrastructure.Persistence;
using Order.Domain.Aggregates.Order.Infrastructure.Repositories;
using Payment.Application.Handlers;
using Payment.Application.Ports;
using Payment.Infrastructure.DomainEvents;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;
using FluentValidation;
using Inventory.Application.Handlers;
using Inventory.Application.Ports;
using Inventory.Application.Validators;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Web Host...");

    // Add services to the container
    builder.Services.AddOpenApi();
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(Order.Domain.Aggregates.Order.Api.Controllers.OrdersController).Assembly)
        .AddApplicationPart(typeof(Payment.Api.Controllers.PaymentsController).Assembly)
        .AddApplicationPart(typeof(Inventory.Api.Controllers.InventoryController).Assembly)
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
            "Payment.Api.xml",
            "Inventory.Api.xml"
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
        var dispatcher = serviceProvider.GetRequiredService<Order.Domain.Aggregates.Order.Infrastructure.DomainEvents.DomainEventDispatcher>();
        return new OrderDbContext(options, dispatcher);
    });

    // Event Bus (Shared across all services)
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

    // Order Domain Event Dispatcher
    builder.Services.AddScoped<Order.Domain.Aggregates.Order.Infrastructure.DomainEvents.DomainEventDispatcher>();

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

    // Get connection string from configuration
    var paymentDbConnectionString = builder.Configuration.GetConnectionString("PaymentDb")
        ?? throw new InvalidOperationException("Connection string 'PaymentDb' not found.");

    // Register DbContextOptions for PaymentDbContext
    builder.Services.AddSingleton<DbContextOptions<PaymentDbContext>>(serviceProvider =>
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
        optionsBuilder.UseSqlServer(paymentDbConnectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("Payment.Infrastructure");
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

    // Payment Domain Event Dispatcher
    builder.Services.AddScoped<Payment.Infrastructure.DomainEvents.DomainEventDispatcher>();

    // Register PaymentDbContext with DomainEventDispatcher
    builder.Services.AddScoped<PaymentDbContext>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<DbContextOptions<PaymentDbContext>>();
        var dispatcher = serviceProvider.GetRequiredService<Payment.Infrastructure.DomainEvents.DomainEventDispatcher>();
        return new PaymentDbContext(options, dispatcher);
    });

    // Payment Service - Repositories
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

    // Payment Service - Command Handlers
    builder.Services.AddScoped<ProcessPaymentCommandHandler>();
    builder.Services.AddScoped<RefundPaymentCommandHandler>();

    // Payment Service - Event Handler (consumes OrderCreated)
    builder.Services.AddScoped<OrderCreatedEventHandler>();

    // Payment Service - Database Seeder
    builder.Services.AddScoped<PaymentDbSeeder>();

    // ============================================
    // INVENTORY SERVICE CONFIGURATION
    // ============================================

    var inventoryDbConnectionString = builder.Configuration.GetConnectionString("InventoryDb")
        ?? throw new InvalidOperationException("Connection string 'InventoryDb' not found.");

    builder.Services.AddSingleton<DbContextOptions<InventoryDbContext>>(serviceProvider =>
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer(inventoryDbConnectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("Inventory.Infrastructure");
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        });
        if (builder.Environment.IsDevelopment())
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
        return optionsBuilder.Options;
    });

    builder.Services.AddScoped<InventoryDbContext>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<DbContextOptions<InventoryDbContext>>();
        return new InventoryDbContext(options);
    });

    builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
    builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
    builder.Services.AddValidatorsFromAssemblyContaining<ReserveInventoryCommandValidator>();
    builder.Services.AddScoped<ReserveInventoryCommandHandler>();
    builder.Services.AddScoped<OrderInventoryRequestedEventHandler>();
    builder.Services.AddScoped<InventoryDbSeeder>();

    var app = builder.Build();

    // 2. Add Serilog Request Logging
    app.UseSerilogRequestLogging();

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

    // Ensure databases are created and seed data (only in Development)
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            // Order Service Database
            var orderContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            await orderContext.Database.EnsureCreatedAsync();
            var orderSeeder = scope.ServiceProvider.GetRequiredService<OrderDbSeeder>();
            await orderSeeder.SeedAsync();

            // Payment Service Database
            var paymentContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            await paymentContext.Database.EnsureCreatedAsync();
            var paymentSeeder = scope.ServiceProvider.GetRequiredService<PaymentDbSeeder>();
            await paymentSeeder.SeedAsync();

            // Inventory Service Database
            var inventoryContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            await inventoryContext.Database.EnsureCreatedAsync();
            var inventorySeeder = scope.ServiceProvider.GetRequiredService<InventoryDbSeeder>();
            await inventorySeeder.SeedAsync();
        }
    }

    // Subscribe to integration events
    // CRITICAL: We need to resolve handlers from a scope when events fire, not at startup
    // This ensures DbContext and other scoped dependencies are available
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    var serviceProvider = app.Services; // Store IServiceProvider to create scopes later

    // #region agent log
    try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "F", location = "Program.cs:SUBSCRIBE", message = "About to subscribe handler with scope resolution", data = new { eventType = "OrderCreated" }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] Program.cs:SUBSCRIBE - Using scope-based handler resolution"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
    // #endregion

    // Subscribe Payment Service to OrderCreated events
    // Resolve handler from a new scope when event fires to ensure DbContext is available
    eventBus.Subscribe<OrderCreated>(async (integrationEvent) =>
    {
        // Create a new scope for this event handler execution
        using (var scope = serviceProvider.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<OrderCreatedEventHandler>();
            
            // #region agent log
            try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "F", location = "Program.cs:EVENT_HANDLER_SCOPE", message = "Handler resolved from scope", data = new { orderId = integrationEvent.OrderId, handlerType = handler.GetType().Name }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine($"[DEBUG] Program.cs:EVENT_HANDLER_SCOPE - OrderId={integrationEvent.OrderId}"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
            // #endregion
            
            await handler.HandleAsync(integrationEvent);
        }
    });

    // #region agent log
    try { var logPath = @"W:\new mentality\MSD\.cursor\debug.log"; System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "F", location = "Program.cs:SUBSCRIBE_COMPLETE", message = "Subscription complete with scope resolution", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); Console.WriteLine("[DEBUG] Program.cs:SUBSCRIBE_COMPLETE"); } catch (Exception ex) { Console.WriteLine($"[DEBUG ERROR] {ex.Message}"); }
    // #endregion

    Log.Information("Subscribed Payment Service to OrderCreated events (with scope-based handler resolution)");

    // Subscribe Inventory Service to OrderInventoryRequested events
    eventBus.Subscribe<OrderInventoryRequested>(async (integrationEvent) =>
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<OrderInventoryRequestedEventHandler>();
            await handler.HandleAsync(integrationEvent);
        }
    });
    Log.Information("Subscribed Inventory Service to OrderInventoryRequested events");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
