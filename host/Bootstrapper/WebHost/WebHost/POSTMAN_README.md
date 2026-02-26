# Postman Collection for Gateway API

This directory contains Postman collections and environments for testing the API Gateway routes.

## Files

- **OrderService.postman_collection.json** - Postman collection with all API endpoints
- **OrderService.postman_environment.json** - Postman environment with variables

## Import Instructions

### Option 1: Import via Postman UI

1. Open Postman
2. Click **Import** button (top left)
3. Select **Files** tab
4. Choose `OrderService.postman_collection.json`
5. Choose `OrderService.postman_environment.json`
6. Click **Import**

### Option 2: Import via File Menu

1. Open Postman
2. Go to **File** → **Import**
3. Drag and drop both JSON files
4. Click **Import**

## Setup

1. **Select Environment**: 
   - In Postman, select "Gateway - Development" from the environment dropdown (top right)

2. **Update Base URL** (if needed):
   - The default base URL is `http://localhost:5000`
   - If your API runs on a different port, update the `baseUrl` variable in the environment

## Collection Structure

### Orders Folder
- **Create Order** - POST `/api/orders`
- **Get Order by ID** - GET `/api/orders/{id}`
- **Get All Orders** - GET `/api/orders`
- **Get Orders by Customer ID** - GET `/api/orders?customerId={id}`
- **Get Orders by Status** - GET `/api/orders?status={status}`
- **Get Orders with Pagination** - GET `/api/orders?skip={skip}&take={take}`
- **Get Orders with Combined Filters** - GET `/api/orders?customerId={id}&status={status}&skip={skip}&take={take}`
- **Cancel Order** - PUT `/api/orders/{id}/cancel`
- **Cancel Order (No Reason)** - PUT `/api/orders/{id}/cancel`

### Payments Folder
- **Process Payment** - POST `/api/payments`
- **Get Payment by ID** - GET `/api/payments/{paymentId}`
- **Get Payment by Order ID** - GET `/api/payments?orderId={orderId}`
- **Refund Payment** - PUT `/api/payments/{paymentId}/refund`

### Inventory Folder
- **Get All Inventory Items** - GET `/api/inventory/items`
- **Get Inventory Item by Product ID** - GET `/api/inventory/items/{productId}`

### Health Folder
- **Health Check** - GET `/health`

## Using the Collection

### 1. Create an Order

1. Open **Orders** → **Create Order**
2. The request body is pre-filled with sample data
3. Click **Send**
4. Copy the `orderId` from the response
5. Update the `orderId` variable in the environment for use in other requests

### 2. Get an Order

1. Open **Orders** → **Get Order by ID**
2. Replace `:orderId` with an actual order ID (or use the variable)
3. Click **Send**

### 3. Filter Orders

- Use **Get Orders by Customer ID** to filter by customer
- Use **Get Orders by Status** to filter by status (Pending, Paid, Completed, Cancelled)
- Use **Get Orders with Pagination** for large result sets
- Use **Get Orders with Combined Filters** for complex queries

### 4. Cancel an Order

1. Open **Orders** → **Cancel Order**
2. Replace `:orderId` with the order ID to cancel
3. Optionally modify the cancellation reason in the request body
4. Click **Send**

## Environment Variables

- **baseUrl**: Base URL for the API (default: `http://localhost:5000`)
- **orderId**: Can be set manually after creating an order for easier testing
- **paymentId**: Can be set manually after processing or fetching a payment
- **productId**: Product lookup key for inventory requests (default: `product-1`)

## Sample Request Bodies

### Create Order
```json
{
  "customerId": "CUST001",
  "items": [
    {
      "productId": "PROD001",
      "quantity": 2,
      "price": 29.99
    },
    {
      "productId": "PROD002",
      "quantity": 1,
      "price": 49.99
    }
  ]
}
```

### Cancel Order
```json
{
  "reason": "Customer requested cancellation"
}
```

## Status Values

When filtering by status, use one of these values:
- `Pending` (0)
- `Paid` (1)
- `Completed` (2)
- `Cancelled` (3)

## Testing Workflow

1. **Start the API**: Ensure the WebHost is running on `http://localhost:5000`
2. **Health Check**: Verify API is running with the Health Check endpoint
3. **Create Order**: Create a new order and note the order ID
4. **Payments**: Process payment, then get/refund payment as needed
5. **Inventory**: Query inventory items and product stock
6. **Orders**: Retrieve/list/cancel orders as needed

## Troubleshooting

- **404 Not Found**: Check that the API is running and the base URL is correct
- **500 Internal Server Error**: Check the API logs for detailed error messages
- **400 Bad Request**: Verify the request body matches the expected schema
- **Connection Refused**: Ensure the API is running on the specified port

## Notes

- All endpoints return JSON responses
- The API uses RESTful conventions
- Order IDs are GUIDs (UUID format)
- Status values are case-sensitive

