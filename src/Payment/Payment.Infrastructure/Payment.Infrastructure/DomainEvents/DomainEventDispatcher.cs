using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using BuildingBlocks.SharedKernel;
using Payment.Domain.Aggregates;
using Payment.Domain.Events;
using PaymentAggregate = Payment.Domain.Aggregates.Payment;

namespace Payment.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches domain events by converting them to integration events and publishing via EventBus.
/// This service handles the conversion from domain events (internal) to integration events (cross-module).
/// </summary>
public class DomainEventDispatcher
{
    private readonly IEventBus _eventBus;

    public DomainEventDispatcher(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// Dispatches domain events by converting them to integration events and publishing.
    /// </summary>
    public async Task DispatchDomainEventsAsync(
        IEnumerable<DomainEvent> domainEvents,
        AggregateRoot<Guid> aggregate)
    {
        foreach (var domainEvent in domainEvents)
        {
            IIntegrationEvent? integrationEvent = domainEvent switch
            {
                PaymentSucceededDomainEvent succeeded when aggregate is PaymentAggregate payment
                    => ConvertToPaymentSucceeded(succeeded, payment),
                PaymentFailedDomainEvent failed when aggregate is PaymentAggregate payment
                    => ConvertToPaymentFailed(failed, payment),
                PaymentRefundedDomainEvent refunded when aggregate is PaymentAggregate payment
                    => ConvertToPaymentRefunded(refunded, payment),
                _ => null
            };

            if (integrationEvent != null)
            {
                await _eventBus.PublishAsync(integrationEvent);
            }
        }
    }

    private PaymentSucceeded ConvertToPaymentSucceeded(PaymentSucceededDomainEvent domainEvent, PaymentAggregate payment)
    {
        return new PaymentSucceeded(
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.Amount,
            domainEvent.ProcessedAt);
    }

    private PaymentFailed ConvertToPaymentFailed(PaymentFailedDomainEvent domainEvent, PaymentAggregate payment)
    {
        return new PaymentFailed(
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.Amount,
            domainEvent.FailureReason,
            domainEvent.FailedAt);
    }

    private PaymentRefunded ConvertToPaymentRefunded(PaymentRefundedDomainEvent domainEvent, PaymentAggregate payment)
    {
        // PaymentRefunded integration event requires a RefundId, so we generate one
        return new PaymentRefunded(
            Guid.NewGuid(), // RefundId
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.Amount,
            domainEvent.RefundedAt);
    }
}
