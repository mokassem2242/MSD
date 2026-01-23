# API Gateway / Web Host

This is the **unified entry point** for all services in the Event-Driven Order Processing System.

## Purpose

In a **Modular Monolith** architecture, this API Gateway:
- Aggregates all service APIs (Order, Payment, Inventory, etc.)
- Provides a single Swagger UI for all endpoints
- Configures dependency injection for all services
- Routes requests to the appropriate controllers

## Current Services

- ✅ **Order Service** - `/api/orders`
- ⏳ **Payment Service** - `/api/payments` (to be implemented)
- ⏳ **Inventory Service** - `/api/inventory` (to be implemented)

## Running the Gateway

```bash
cd host/Bootstrapper/WebHost/WebHost
dotnet run
```

Then access:
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Order API**: http://localhost:5000/api/orders

## Architecture

### Current: Modular Monolith
```
┌─────────────────────────────────────┐
│  API Gateway (This Project)        │
│  ┌─────────┐ ┌─────────┐ ┌────────┐│
│  │ Order   │ │Payment  │ │Inventory││
│  │ API     │ │ API     │ │ API    ││
│  └─────────┘ └─────────┘ └────────┘│
│  Single Process                     │
└─────────────────────────────────────┘
```

### Future: Microservices
```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│Order Service │  │Payment Service│  │Inventory     │
│(Port 5001)   │  │(Port 5002)   │  │Service       │
│              │  │              │  │(Port 5003)   │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       └─────────────────┼─────────────────┘
                         │
              ┌──────────▼──────────┐
              │   API Gateway       │
              │  (This Project)     │
              │  Routes to services│
              └─────────────────────┘
```

## Adding a New Service

1. **Add project reference** in `WebHost.csproj`:
   ```xml
   <ProjectReference Include="..\..\..\..\src\YourService\YourService.Api\YourService.Api.csproj" />
   ```

2. **Register controllers** in `Program.cs`:
   ```csharp
   .AddApplicationPart(typeof(YourService.Api.Controllers.YourController).Assembly)
   ```

3. **Configure services** in `Program.cs`:
   ```csharp
   // YourService configuration
   builder.Services.AddDbContext<YourServiceDbContext>(...);
   builder.Services.AddScoped<IYourServiceRepository, YourServiceRepository>();
   // etc.
   ```

4. **Add XML comments** to Swagger:
   ```csharp
   var xmlFiles = new[] { "Order.Api.xml", "YourService.Api.xml" };
   ```

## Benefits

✅ **Single Entry Point** - One URL for all services  
✅ **Unified Documentation** - All APIs in one Swagger UI  
✅ **Easy Development** - Run everything in one process  
✅ **Microservices-Ready** - Can extract services later without changing API contracts

