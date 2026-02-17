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
builder.Services.AddScoped<ReserveInventoryCommandHandler>();
builder.Services.AddScoped<InventoryDbSeeder>();

var app = builder.Build();

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
