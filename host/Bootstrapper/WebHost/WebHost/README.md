# API Gateway / Web Host (Ocelot)

This project is now a dedicated **Ocelot API Gateway**.

## Purpose

- Exposes a single entry point for all service APIs.
- Proxies requests to downstream services.
- Keeps public contracts stable (`/api/orders`, `/api/payments`, `/api/inventory`).

## Downstream Services

- `Order.Api` on `http://localhost:5071`
- `Payment.Api` on `http://localhost:5219`
- `Inventory.Api` on `http://localhost:5112`

Routes are defined in `ocelot.json`.

## Run Locally

1. Start RabbitMQ:

```bash
docker compose -f docker-compose.rabbitmq.yml up -d
```

RabbitMQ endpoints:
- AMQP: `localhost:5672`
- Management UI: `http://localhost:15672` (`guest` / `guest`)

2. Start each downstream service:
   - `src/Order/Order.Api/Order.Api`
   - `src/Payment/Payment.Api/Payment.Api`
   - `src/Inventory/Inventory.Api/Inventory.Api`
3. Start gateway:

```bash
cd host/Bootstrapper/WebHost/WebHost
dotnet run
```

Gateway endpoints:
- Health: `http://localhost:5000/health`
- Orders: `http://localhost:5000/api/orders`
- Payments: `http://localhost:5000/api/payments`
- Inventory: `http://localhost:5000/api/inventory`

Service passthrough routes:
- Order service root: `http://localhost:5000/services/order/{everything}`
- Payment service root: `http://localhost:5000/services/payment/{everything}`
- Inventory service root: `http://localhost:5000/services/inventory/{everything}`

## Rate Limiting

The gateway now throttles requests using Ocelot rate limiting.

- Order routes: `5` requests per `1` minute
- Payment routes: `100` requests per `1` minute
- Inventory routes: `100` requests per `1` minute
- Status when exceeded: `429 Too Many Requests`
- Client identity header: `X-ClientId`

Use a stable `X-ClientId` value per client/app so requests are counted correctly per caller.

## Authentication And Authorization

- JWT is validated at the gateway using the `GatewayJwt` authentication scheme.
- Configure JWT settings in `appsettings*.json` under `Jwt`:
  - `Authority` (for external IdP) or `SigningKey` (for local symmetric tokens)
  - `Issuer`
  - `Audience`
- Every proxied route requires authentication plus a route claim:
  - Order routes require `role=orders.api`
  - Payment routes require `role=payments.api`
  - Inventory routes require `role=inventory.api`

## User Context Propagation

After token validation, the gateway forwards trusted identity headers to downstream services:

- `X-User-Id` from `sub`/name identifier
- `X-User-Email` from `email`
- `X-User-Roles` as comma-separated roles

Inbound `X-User-*` headers from clients are removed first to prevent spoofing.

## Add a New Service Route

1. Add a route in `ocelot.json`:
   - `UpstreamPathTemplate` for public gateway path.
   - `DownstreamPathTemplate` for service path.
   - `DownstreamHostAndPorts` for target service host/port.
2. Restart the gateway (or rely on config reload if enabled by environment).
