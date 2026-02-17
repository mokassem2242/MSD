using BuildingBlocks.EventBus;
using BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using Order.Domain.Aggregates.Order.Application.EventHandlers;
using Order.Domain.Aggregates.Order.Application.Ports;
using Order.Domain.Aggregates.Order.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Order.UnitTests.Application.EventHandler
{
    public class PaymentSucceededEventHandlerTests
    {
        private readonly Mock<IOrderRepository> _orderRepository;
        private readonly Mock<IEventBus> _eventBus;
        private readonly Mock<ILogger<PaymentSucceededEventHandler>>? _logger;
        private readonly PaymentSucceededEventHandler _sut;



        public PaymentSucceededEventHandlerTests()
        {

            _eventBus = new Mock<IEventBus>();
            _orderRepository = new Mock<IOrderRepository>();
            _logger = new Mock<ILogger<PaymentSucceededEventHandler>>();
            _sut = new PaymentSucceededEventHandler(_orderRepository.Object, _eventBus.Object, _logger.Object);
        }



        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void constructor_NullParameter_ShouldThrowException(
                bool nullRepo,
                bool nullEventBus,
                bool nullLogger
            )
        {
            //arange 
            var repo = nullRepo ? null : _orderRepository.Object;
            var eventBus = nullEventBus ? null : _eventBus.Object;
            var logger = nullLogger ? null : _logger!.Object;

            //act
            var ex = Assert.ThrowsAny<ArgumentNullException>(() => new PaymentSucceededEventHandler(repo, eventBus, logger));


            // Assert
            var expectedParamName =
               nullRepo ? "orderRepository" :
               nullEventBus ? "eventBus" :
               "logger";
            Assert.Equal(expectedParamName, ex.ParamName);
        }


        [Fact]
        public async Task HandleAsync_ValidPaymentSucceeded_ShouldMardOrderAsPaidANdReaseReseveInventoryEvent()
        {
            //Arrange
            var paymentId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var processedAt = DateTime.UtcNow;
            var customerId = Guid.NewGuid().ToString();
            var order = Order.Domain.Aggregates.Order.Domain.Aggregates.Order.Create(customerId, [new Order.Domain.Aggregates.Order.Domain.ValueObjects.OrderItem("pro-1", 1, 500)]);
            var paymentSucceedEvent = new PaymentSucceeded(paymentId, orderId, 500m, processedAt);

            _orderRepository.Setup(x => x.GetByIdAsync(orderId)).ReturnsAsync(order);
            _eventBus.Setup(x => x.PublishAsync(It.IsAny<IIntegrationEvent>())).Returns(Task.CompletedTask);

            //Act
            await _sut.HandleAsync(paymentSucceedEvent);


            //Assert
            Assert.Equal(OrderStatus.Paid, order.Status);
            _eventBus.Verify(
                             x => x.PublishAsync(It.Is<OrderInventoryRequested>(e =>
                                 e.OrderId == orderId &&
                                 e.Items.Count == 1 &&
                                 e.Items[0].ProductId == "pro-1" &&
                                 e.Items[0].Quantity == 1)),
                             Times.Once);
            _orderRepository.Verify(
                x => x.UpdateAsync(It.Is<Order.Domain.Aggregates.Order.Domain.Aggregates.Order>(
                  o => o.Id == orderId && o.Status == OrderStatus.Paid)),
                Times.Once);
        }
    }
}
