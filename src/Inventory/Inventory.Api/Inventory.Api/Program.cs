using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using FluentValidation;
using Inventory.Application.Handlers;
using Inventory.Application.Ports;
using Inventory.Application.Validators;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var inventoryDbConnectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? "Server=(localdb)\\mssqllocaldb;Database=InventoryDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddSingleton<DbContextOptions<InventoryDbContext>>(_ =>
{
    var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
    optionsBuilder.UseSqlServer(inventoryDbConnectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("Inventory.Infrastructure");
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
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
var rabbitMqOptions = BuildRabbitMqOptions(builder.Configuration, "inventory-api");
builder.Services.AddSingleton<IEventBus>(_ => new RabbitMqEventBus(rabbitMqOptions));
builder.Services.AddScoped<ReserveInventoryCommandHandler>();
builder.Services.AddScoped<OrderInventoryRequestedEventHandler>();
builder.Services.AddScoped<InventoryDbSeeder>();

var app = builder.Build();

SubscribeToIntegrationEvents(app);

app.UseHttpsRedirection();
app.MapOpenApi();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Inventory", timestamp = DateTime.UtcNow }));

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await context.Database.EnsureCreatedAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<InventoryDbSeeder>();
    await seeder.SeedAsync();
}

app.Run();

static RabbitMqOptions BuildRabbitMqOptions(IConfiguration configuration, string defaultServiceName)
{
    var rabbitMqSection = configuration.GetSection("RabbitMq");
    var options = new RabbitMqOptions
    {
        HostName = rabbitMqSection["HostName"] ?? "localhost",
        UserName = rabbitMqSection["UserName"] ?? "guest",
        Password = rabbitMqSection["Password"] ?? "guest",
        VirtualHost = rabbitMqSection["VirtualHost"] ?? "/",
        ExchangeName = rabbitMqSection["ExchangeName"] ?? "msd.events",
        ServiceName = rabbitMqSection["ServiceName"] ?? defaultServiceName
    };

    if (int.TryParse(rabbitMqSection["Port"], out var port))
    {
        options.Port = port;
    }

    return options;
}

static void SubscribeToIntegrationEvents(WebApplication app)
{
    var eventBus = app.Services.GetRequiredService<IEventBus>();

    eventBus.Subscribe<OrderInventoryRequested>(async integrationEvent =>
    {
        using var scope = app.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OrderInventoryRequestedEventHandler>();
        await handler.HandleAsync(integrationEvent);
    });
}
