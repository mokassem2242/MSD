using BuildingBlocks.EventBus;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Handlers;
using Payment.Application.Ports;
using Payment.Infrastructure.DomainEvents;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Payment Service API",
        Description = "API for processing and refunding payments.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Payment Service",
            Email = "support@paymentservice.local"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.CustomSchemaIds(type => type.FullName);
});

var paymentDbConnectionString = builder.Configuration.GetConnectionString("PaymentDb")
    ?? "Server=(localdb)\\mssqllocaldb;Database=PaymentDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddSingleton<DbContextOptions<PaymentDbContext>>(_ =>
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

    if (builder.Environment.IsDevelopment())
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
    }

    return optionsBuilder.Options;
});

builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddScoped<DomainEventDispatcher>();

builder.Services.AddScoped<PaymentDbContext>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<DbContextOptions<PaymentDbContext>>();
    var dispatcher = serviceProvider.GetRequiredService<DomainEventDispatcher>();
    return new PaymentDbContext(options, dispatcher);
});

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ProcessPaymentCommandHandler>();
builder.Services.AddScoped<RefundPaymentCommandHandler>();
builder.Services.AddScoped<PaymentDbSeeder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapOpenApi();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Payment", timestamp = DateTime.UtcNow }));

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await context.Database.EnsureCreatedAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<PaymentDbSeeder>();
    await seeder.SeedAsync();
}

app.Run();
