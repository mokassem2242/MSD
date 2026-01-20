# Canonical Distributed Systems Practice Project

## Goal
Build **one deliberate system** that covers most microservices and distributed-systems concepts so you become **confident, interview-ready, and hireable**.

This is not a product idea. It is a **training system**.

---

## The System

### **Event-Driven Order Processing System**

A backend-centric distributed system designed to maximize concept coverage with minimal domain complexity.

### Core Business Flow
```
Customer places order
→ Order is created
→ Payment is processed
→ Inventory is reserved
→ Notification is sent
→ Analytics records event
```

---

## Why This System
This single system intentionally covers:

- Microservices boundaries
- Message brokers
- Outbox pattern
- Saga pattern
- Eventual consistency
- Idempotency
- Caching strategies
- Failure handling
- Read/write scaling
- Interview-level reasoning

Companies internally use **similar systems** to train senior engineers.

---

## Target Architecture

### Services
1. Order Service
2. Payment Service
3. Inventory Service
4. Notification Service
5. Analytics Worker
6. API Gateway
7. Message Broker
8. Cache (Redis)

### Core Rules
- Each service owns its **own database**
- Services communicate via **events**, not shared DBs
- Services can fail independently

---

## Where Each Distributed Concept Lives

### Message Broker
Used for:
- OrderCreated
- PaymentSucceeded / PaymentFailed
- InventoryReserved / InventoryFailed
- OrderCompleted / OrderCancelled

Practices:
- At-least-once delivery
- Duplicate event handling
- Event versioning

---

### Outbox Pattern (Mandatory)
Implemented in:
- Order Service
- Payment Service

Purpose:
- Prevents data inconsistency when DB commit succeeds but message publish fails

This is a **core senior-level concept**.

---

### Saga Pattern
Use **orchestration**:
- Order Service acts as the saga orchestrator

Example failure:
- Payment succeeds
- Inventory fails
- Compensation → refund payment

---

### Caching (Redis)
Used for:
- Read-heavy order queries
- Product availability
- Hot reference data

Practices:
- Cache-aside pattern
- TTL management
- Cache invalidation via events

---

### Idempotency
Applied to:
- Order creation
- Payment processing
- Event consumers

Reason:
- Retries are unavoidable
- Duplicate events are expected

Non-negotiable for distributed systems.

---

## Failure Scenarios to Practice
You must intentionally break the system:

- Kill a consumer mid-message
- Restart the message broker
- Introduce artificial latency
- Replay events

Then answer:
- Why data stayed consistent
- How retries behaved
- Where compensation occurred

---

## Implementation Phases

### Phase 1 — Modular Monolith (Week 1)
- Single solution
- Clear bounded contexts
- Shared DB

Goal: enforce boundaries before distribution.

---

### Phase 2 — First Extraction (Week 2)
- Extract Order and Payment services
- Separate databases
- Synchronous REST communication

Goal: feel service separation pain.

---

### Phase 3 — Introduce Message Broker (Week 3)
- Replace sync calls with events
- Add retries and dead-letter queues

Goal: async-first mindset.

---

### Phase 4 — Outbox + Idempotency (Week 4)
- Implement outbox pattern
- Add idempotency keys

Goal: correctness under failure.

---

### Phase 5 — Caching + Analytics (Week 5)
- Add Redis
- Add async analytics worker

Goal: performance and isolation.

---

## Success Criteria (Not Features)
You succeed when you can:

- Explain trade-offs clearly
- Demonstrate failure recovery
- Justify architectural decisions
- Defend why consistency is eventual

---

## Key Principle
> Companies hire engineers who deeply understand **one distributed system**, not those who shallowly build many.

This system is your reference point for interviews, discussions, and confidence.

---

## Next Steps
- Lock tech stack (.NET, RabbitMQ, Redis)
- Define event contracts
- Implement step by step
- Map code to interview answers


---

# Service Boundaries & Bounded Contexts
## Event-Driven Order Processing System

This section defines the **bounded contexts** of the system. Each bounded context has:
- A single responsibility
- Clear data ownership
- Explicit inputs and outputs
- Autonomy of change

---

## Order Context (Order Service – Saga Orchestrator)

### Responsibility
Owns the **order lifecycle** and orchestrates the **order saga** from creation to completion or cancellation.

Acts as the **source of truth** for order state.

### Owns Data
- Orders
- Order status (Pending, Paid, Completed, Cancelled)
- Saga state

No other context is allowed to modify order state.

### Inputs (Commands / Events)
- `CreateOrder` (command)
- `PaymentSucceeded` (event)
- `PaymentFailed` (event)
- `InventoryReserved` (event)
- `InventoryFailed` (event)

### Outputs (Events)
- `OrderCreated`
- `OrderPaymentRequested`
- `OrderInventoryRequested`
- `OrderCompleted`
- `OrderCancelled`

---

## Payment Context (Payment Service)

### Responsibility
Processes payments and handles **charging and refunding** money.

This context is **financially authoritative** and must be idempotent.

### Owns Data
- Payments
- Payment transactions
- Payment status (Succeeded, Failed, Refunded)

No other context can create or modify payments.

### Inputs (Commands / Events)
- `OrderPaymentRequested` (event)
- `RefundPayment` (command)

### Outputs (Events)
- `PaymentSucceeded`
- `PaymentFailed`
- `PaymentRefunded`

---

## Inventory Context (Inventory Service)

### Responsibility
Manages **inventory availability** and stock reservation.

Decides whether requested items can be reserved.

### Owns Data
- Inventory items
- Stock levels
- Reservations

No other context can change stock quantities.

### Inputs (Commands / Events)
- `OrderInventoryRequested` (event)
- `ReleaseInventory` (command)

### Outputs (Events)
- `InventoryReserved`
- `InventoryFailed`
- `InventoryReleased`

---

## Notification Context (Notification Service)

### Responsibility
Sends **user-facing notifications** (email, SMS, push).

This context is **best-effort** and must never block business flows.

### Owns Data
- Notification logs
- Delivery attempts
- Templates (optional)

No business-critical state lives here.

### Inputs (Events)
- `OrderCompleted`
- `OrderCancelled`
- `PaymentFailed`

### Outputs
- None (side effects only)

---

## Analytics Context (Analytics Worker)

### Responsibility
Collects and processes **business events** for reporting and analytics.

This context is **eventually consistent by design**.

### Owns Data
- Analytics events
- Aggregated metrics
- Reports

Analytics data is never used for transactional decisions.

### Inputs (Events)
- `OrderCreated`
- `OrderCompleted`
- `OrderCancelled`
- `PaymentSucceeded`
- `PaymentFailed`

### Outputs
- Optional `AnalyticsProcessed`

---

## Global Bounded Context Rules

### Ownership Rule
A bounded context is the **only writer** of its data.

### Communication Rule
- Commands express **intent**
- Events express **facts**

### Consistency Rule
- No distributed transactions
- Eventual consistency
- Compensation via saga

### Failure Rule
- Events may be duplicated
- Consumers must be idempotent
- Message delivery is at-least-once

---

## Key Takeaway
These bounded contexts are defined **before distribution**. They can be implemented first as a **modular monolith** and later extracted into independent microservices without redesign.


---

# Problem Definition & System Goals

## 1. Problem Statement

The system is responsible for processing customer orders in a reliable and scalable manner. It must coordinate order creation, payment processing, inventory reservation, notifications, and analytics while allowing each capability to operate independently.

The system must remain correct and usable in the presence of partial failures, retries, and duplicate events, without relying on distributed transactions.

---

## 2. Functional Scope

The system supports the following core capabilities:

- Create and manage customer orders
- Process payments for orders
- Reserve and release inventory
- Send user-facing notifications
- Collect and store analytics events

Each capability is owned and implemented by a dedicated bounded context.

---

## 3. Out of Scope

The following concerns are intentionally excluded from this system:

- User interface or frontend applications
- Promotions, discounts, or complex pricing rules
- Real external payment gateways (payments are simulated)
- Multi-currency support
- User account management
- Reporting dashboards

These exclusions keep the system focused on distributed-systems concerns rather than product features.

---

## 4. Non-Functional Requirements

### Availability
- Order creation must not depend on notification or analytics services.
- Partial system outages must not block core business flows.

### Consistency
- The system is eventually consistent by design.
- No distributed transactions are used.
- Business consistency is achieved through sagas and compensation.

### Reliability
- Events must not be lost, even in the presence of service or broker failures.
- Message delivery is assumed to be at-least-once.

### Scalability
- Services must scale independently based on their workload characteristics.
- Read-heavy and write-heavy paths should be isolated where possible.

### Fault Tolerance
- Services may crash, restart, or process duplicate messages without corrupting data.
- All consumers must be idempotent.

### Performance
- Read operations should be optimized for low latency.
- Non-critical operations must be asynchronous.

### Observability
- Requests and events should be traceable across services.
- Failures must be detectable and diagnosable.

---

## 5. Constraints

- Technology stack: .NET ecosystem
- Communication model: asynchronous, event-driven
- Message delivery: at-least-once
- Team size: single developer
- Deployment environment: local development with containerization

These constraints intentionally reflect a realistic solo-engineer learning environment.

---

## Key Goal

The primary goal of this system is not feature completeness, but **deep understanding of microservices and distributed-systems principles**, including failure handling, consistency trade-offs, and service autonomy.


---

# End-to-End Roadmap: From Design to Code

This section documents **what happens next** and the **complete sequence of steps** from architecture design to writing production-grade code. It exists to prevent confusion, premature coding, and scope drift.

---

## Current Status

Completed:
- C4 Level 1 – System Context Diagram
- C4 Level 2 – Container Diagram

The system structure is now **clear and validated**.

---

## Phase 1 — Behavioral Design (No Code)

### Step 3 — Saga & Event Flow (NEXT)
**Goal:** Understand system behavior over time, including failures.

**What to produce:**
- Text-based description of the order saga
- Happy path
- Payment failure path
- Inventory failure path with compensation

**Outcome:**
- Clear understanding of eventual consistency
- Clear ownership of failure handling
- Interview-ready explanations

---

### Step 4 — Event Contracts
**Goal:** Define how services communicate asynchronously.

**What to produce:**
- Event names (e.g., OrderCreated, PaymentSucceeded)
- Minimal event payloads
- Event ownership (publisher)

**Outcome:**
- No ambiguous events during implementation
- Stable async boundaries

---

### Step 5 — Saga Control Decision
**Goal:** Remove ambiguity in orchestration.

**Decision:**
- Saga type: **Orchestration**
- Orchestrator: **Order Service**

**Outcome:**
- Single source of control for business flow
- Easier debugging and reasoning

---

## Phase 2 — Code Shape (Still No Infrastructure)

### Step 6 — Modular Monolith Design
**Goal:** Prepare a codebase that enforces boundaries without distribution.

**What to design:**
- One .NET solution
- Separate projects per bounded context
- Clear dependency rules between projects

**Outcome:**
- Clean boundaries
- Easy future extraction to microservices

---

### Step 7 — Persistence Model
**Goal:** Align data models with bounded contexts.

**What to define:**
- Tables per service
- No cross-service joins
- Identifiers and status fields only

**Outcome:**
- No shared data ownership
- Persistence aligned with saga flow

---

## Phase 3 — First Code (Local, Synchronous)

### Step 8 — Implement Order Service Only
**Goal:** Start coding with the core business flow.

**What to implement:**
- Create order
- Persist order
- Emit in-memory domain events

**Outcome:**
- Business logic validated early
- Confidence before complexity

---

### Step 9 — Add Payment & Inventory (Synchronous)
**Goal:** Validate correctness before async complexity.

**What to implement:**
- Direct calls from Order Service to Payment and Inventory
- Basic failure handling

**Outcome:**
- Correct business behavior
- Clear understanding of flow

---

## Phase 4 — Distributed Reality

### Step 10 — Introduce Message Broker
**Goal:** Transition to event-driven communication.

**What changes:**
- Replace synchronous calls with events
- Add consumers per service

**Outcome:**
- Eventual consistency becomes real

---

### Step 11 — Implement Outbox Pattern
**Goal:** Prevent message loss.

**What to add:**
- Outbox table
- Reliable event publishing after DB commit

**Outcome:**
- Production-grade reliability

---

### Step 12 — Idempotency & Retries
**Goal:** Make the system safe under duplication and restarts.

**What to add:**
- Idempotency keys
- Safe retry handling

**Outcome:**
- Correctness under at-least-once delivery

---

### Step 13 — Cache & Analytics (Optional)
**Goal:** Improve performance and observability.

**What to add:**
- Redis cache for read models
- Analytics worker consuming events

**Outcome:**
- Realistic production characteristics

---

## Guiding Principles

- Design before infrastructure
- Behavior before optimization
- Correctness before performance
- One system, many concepts

---

## Key Reminder

> Do not skip steps. Each step exists to remove ambiguity before the next one introduces complexity.

---

## Next Immediate Action

Proceed to:

**Step 3 — Saga & Event Flow Modeling**

This is the next mandatory step before writing any code.

---

# Order Saga – Event Flow (DDD Perspective)

## 1️⃣ Happy Path (Order Succeeds)

1. Customer places an order
2. Order Service creates an Order aggregate in Pending state
3. Order Service publishes OrderCreated
4. Payment Service processes payment
5. Payment Service publishes PaymentSucceeded
6. Order Service marks order as Paid
7. Order Service requests inventory reservation
8. Inventory Service reserves stock
9. Inventory Service publishes InventoryReserved
10. Order Service marks order as Completed

## 2️⃣ Failure Path: Payment Fails

**What fails?**
Payment processing fails (insufficient funds, card declined, payment gateway error).

**Who knows?**
Payment Service detects the failure and publishes PaymentFailed event.

**What state does the order end up in?**
Order ends up in Cancelled state.

1. Order created
2. Payment processing fails
3. Payment Service publishes PaymentFailed
4. Order Service marks order as Cancelled
5. No inventory is touched

## 3️⃣ Failure Path: Inventory Fails (Compensation)

1. Order created
2. Payment succeeds
3. Inventory reservation fails
4. Inventory Service publishes InventoryFailed
5. Order Service triggers compensation
6. Order Service requests payment refund
7. Payment Service refunds money
8. Order Service marks order as Cancelled

---

# Saga Orchestration Pattern

This system uses the **Saga Orchestration Pattern** to coordinate the order processing workflow across multiple services.

## Decision: Orchestration vs Choreography

**Pattern:** Orchestration  
**Orchestrator:** Order Service  
**Reason:** Centralized control, easier debugging, clear failure handling, explicit compensation logic

---

## What is Saga Orchestration?

In orchestration, a **central coordinator** (the orchestrator) manages the entire saga flow. The orchestrator:
- Decides **what to do next** at each step
- **Sends commands** to participant services
- **Waits for responses** (events) from participants
- **Triggers compensation** when failures occur
- **Maintains saga state** (current step, status)

---

## Order Service as Orchestrator

The **Order Service** acts as the saga orchestrator because:

1. **Ownership**: Order Service owns the order lifecycle
2. **Business Logic**: Order processing is its core responsibility
3. **State Management**: Order Service maintains the saga state (order status transitions)
4. **Compensation Control**: Order Service decides when and how to compensate

---

## How Orchestration Works in This System

### Step-by-Step Flow

**Happy Path:**
1. Order Service receives `CreateOrder` command
2. Order Service creates order in `Pending` state
3. Order Service publishes `OrderCreated` event
4. Order Service **listens** for `PaymentSucceeded` event
5. When received, Order Service marks order as `Paid`
6. Order Service publishes `OrderPaymentRequested` event (implicit: Payment Service processes)
7. Order Service **listens** for `InventoryReserved` event
8. When received, Order Service marks order as `Completed`
9. Order Service publishes `OrderCompleted` event

**Payment Failure Path:**
1. Order Service receives `PaymentFailed` event
2. Order Service **decides** to cancel order
3. Order Service marks order as `Cancelled`
4. Order Service publishes `OrderCancelled` event
5. No compensation needed (payment never succeeded)

**Inventory Failure Path (Compensation):**
1. Order Service receives `InventoryFailed` event
2. Order Service **triggers compensation**:
   - Publishes `RefundPayment` command (or `OrderPaymentRefundRequested` event)
   - **Waits** for `PaymentRefunded` event
3. When received, Order Service marks order as `Cancelled`
4. Order Service publishes `OrderCancelled` event

---

## Orchestrator Responsibilities

### Order Service (Orchestrator) Must:

1. **Maintain Saga State**
   - Track current order status (Pending → Paid → Completed)
   - Track which steps have completed
   - Store saga context (orderId, paymentId, reservationId)

2. **Coordinate Participants**
   - Decide when to trigger next step
   - Send commands/events to participant services
   - Wait for participant responses

3. **Handle Failures**
   - Detect failures from participant events
   - Decide compensation actions
   - Trigger compensation workflow
   - Ensure saga reaches terminal state

4. **Ensure Idempotency**
   - Handle duplicate events safely
   - Prevent duplicate command processing
   - Maintain idempotency keys

---

## Participant Services (Not Orchestrators)

### Payment Service
- **Role**: Participant
- **Responsibilities**: Process payment, publish success/failure, process refunds
- **Does NOT decide**: When to refund (orchestrator decides)

### Inventory Service
- **Role**: Participant
- **Responsibilities**: Reserve inventory, publish success/failure
- **Does NOT decide**: When to release inventory (orchestrator decides)

### Notification Service
- **Role**: Observer (listens to final events)
- **Responsibilities**: Send notifications
- **Does NOT participate in saga coordination**

### Analytics Worker
- **Role**: Observer (listens to all events)
- **Responsibilities**: Record events for analytics
- **Does NOT participate in saga coordination**

---

## Orchestration vs Choreography

### Why Orchestration (Chosen)

**Advantages:**
- ✅ Centralized control and visibility
- ✅ Easier to understand flow (all logic in one place)
- ✅ Simpler debugging (single service to inspect)
- ✅ Explicit compensation logic
- ✅ Better for complex workflows

**Trade-offs:**
- ❌ Orchestrator can become bottleneck (not an issue here)
- ❌ Tighter coupling (orchestrator knows all participants)

### Why NOT Choreography

**Disadvantages for this system:**
- ❌ Distributed control (harder to debug)
- ❌ Implicit flow (harder to understand)
- ❌ Complex compensation logic
- ❌ Requires all participants to understand full flow

---

## Saga State Management

The Order Service maintains saga state through:

1. **Order Aggregate**
   - Order status (Pending, Paid, Completed, Cancelled)
   - Related IDs (paymentId, reservationId)
   - Saga step tracking

2. **Event Sourcing (Optional Enhancement)**
   - Store all saga events
   - Replay to rebuild state
   - Audit trail

3. **Idempotency Keys**
   - Track processed events
   - Prevent duplicate processing
   - Ensure saga consistency

---

## Key Principle

> The **Order Service orchestrates the saga** because it owns the order lifecycle and is the best place to make coordination decisions.

The orchestrator does **NOT**:
- Own participant service data (Payment, Inventory)
- Make business decisions for participants
- Force synchronous communication

The orchestrator **DOES**:
- Coordinate the workflow
- Decide next steps based on events
- Trigger compensation when needed
- Maintain saga state

---

# Event Contracts

This section defines the **event contracts** used in the order saga. Each event is a fact that has happened and represents communication between services.

## Core Principle
- Events are **immutable facts**
- Events have **minimal payloads** (only what consumers need)
- Events are **owned by publishers**
- Events may be consumed by **multiple services**

---

## Event: OrderCreated

**Publisher:** Order Service

**Purpose:** Announces that a new order has been created and is ready for processing.

**Payload:**
```json
{
  "orderId": "guid",
  "customerId": "string",
  "totalAmount": "decimal",
  "items": [
    {
      "productId": "string",
      "quantity": "int",
      "price": "decimal"
    }
  ],
  "createdAt": "datetime"
}
```

**Consumers:**
- Payment Service (triggers payment processing)
- Inventory Service (triggers inventory reservation)
- Analytics Worker (records order creation)

---

## Event: PaymentSucceeded

**Publisher:** Payment Service

**Purpose:** Announces that payment for an order has been successfully processed.

**Payload:**
```json
{
  "paymentId": "guid",
  "orderId": "guid",
  "amount": "decimal",
  "processedAt": "datetime"
}
```

**Consumers:**
- Order Service (marks order as Paid, triggers inventory reservation)
- Analytics Worker (records successful payment)

---

## Event: PaymentFailed

**Publisher:** Payment Service

**Purpose:** Announces that payment processing has failed.

**Payload:**
```json
{
  "paymentId": "guid",
  "orderId": "guid",
  "amount": "decimal",
  "failureReason": "string",
  "failedAt": "datetime"
}
```

**Consumers:**
- Order Service (marks order as Cancelled)
- Notification Service (sends failure notification)
- Analytics Worker (records payment failure)

---

## Event: InventoryReserved

**Publisher:** Inventory Service

**Purpose:** Announces that inventory has been successfully reserved for an order.

**Payload:**
```json
{
  "reservationId": "guid",
  "orderId": "guid",
  "items": [
    {
      "productId": "string",
      "quantity": "int"
    }
  ],
  "reservedAt": "datetime"
}
```

**Consumers:**
- Order Service (marks order as Completed)
- Analytics Worker (records inventory reservation)

---

## Event: InventoryFailed

**Publisher:** Inventory Service

**Purpose:** Announces that inventory reservation has failed (e.g., out of stock).

**Payload:**
```json
{
  "orderId": "guid",
  "failureReason": "string",
  "failedItems": [
    {
      "productId": "string",
      "requestedQuantity": "int",
      "availableQuantity": "int"
    }
  ],
  "failedAt": "datetime"
}
```

**Consumers:**
- Order Service (triggers compensation: requests payment refund)

---

## Event: PaymentRefunded

**Publisher:** Payment Service

**Purpose:** Announces that a payment has been refunded (compensation for failed inventory).

**Payload:**
```json
{
  "refundId": "guid",
  "paymentId": "guid",
  "orderId": "guid",
  "amount": "decimal",
  "refundedAt": "datetime"
}
```

**Consumers:**
- Order Service (marks order as Cancelled after compensation)
- Analytics Worker (records refund)

---

## Event: OrderCompleted

**Publisher:** Order Service

**Purpose:** Announces that an order has been successfully completed (paid and inventory reserved).

**Payload:**
```json
{
  "orderId": "guid",
  "completedAt": "datetime"
}
```

**Consumers:**
- Notification Service (sends order confirmation)
- Analytics Worker (records order completion)

---

## Event: OrderCancelled

**Publisher:** Order Service

**Purpose:** Announces that an order has been cancelled (payment failed or compensation completed).

**Payload:**
```json
{
  "orderId": "guid",
  "cancellationReason": "string",
  "cancelledAt": "datetime"
}
```

**Consumers:**
- Notification Service (sends cancellation notification)
- Analytics Worker (records order cancellation)

---

## Event Contract Rules

### Ownership
- Each service owns and publishes its own events
- No service publishes events for another service's domain

### Payload Design
- Include only **essential fields** consumers need
- Use identifiers (orderId, paymentId) for correlation
- Avoid including full aggregate states

### Idempotency
- Events include identifiers that enable idempotent processing
- Consumers must handle duplicate events safely

### Versioning
- Events are versioned (e.g., OrderCreated v1)
- New versions must be backward compatible or introduce new event names

---

## Event Flow Summary

**Happy Path:**
```
OrderCreated → PaymentSucceeded → InventoryReserved → OrderCompleted
```

**Payment Fails:**
```
OrderCreated → PaymentFailed → OrderCancelled
```

**Inventory Fails (Compensation):**
```
OrderCreated → PaymentSucceeded → InventoryFailed → PaymentRefunded → OrderCancelled
```

