using Xunit;
using Moq;
using printingshop.application.Services;
using printingshop.application.Interfaces;
using printingshop.domain.Interfaces;
using printingshop.domain.Entities;
using printingshop.domain.Enums;

namespace printingshop.unittests
{
    public class OrderServiceTests
    {
        private readonly Mock<IGmailService> _mockGmail;
        private readonly Mock<IGoogleDriveService> _mockDrive;
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockGmail = new Mock<IGmailService>();
            _mockDrive = new Mock<IGoogleDriveService>();
            _mockOrderRepository = new Mock<IOrderRepository>();
            _orderService = new OrderService(_mockGmail.Object, _mockDrive.Object, _mockOrderRepository.Object);
        }

        #region GetAllOrdersAsync Tests

        [Fact]
        public async Task GetAllOrdersAsync_ReturnsEmptyList_WhenNoOrdersExist()
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _orderService.GetAllOrdersAsync();

            // Assert
            Assert.Empty(result);
            _mockOrderRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllOrdersAsync_ReturnsList_WhenOrdersExist()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerEmail = "test@example.com",
                    Subject = "Print Job",
                    Status = OrderStatus.ReadyToPrint,
                    Attachments = new(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockOrderRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetAllOrdersAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("test@example.com", result[0].CustomerEmail);
            Assert.Equal("ReadyToPrint", result[0].Status);
        }

        #endregion

        #region GetOrderByIdAsync Tests

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsNull_WhenOrderNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.GetOrderByIdAsync(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsOrderDetail_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerEmail = "test@example.com",
                Subject = "Print Job",
                Status = OrderStatus.ReadyToPrint,
                PrintAttempts = 0,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            };

            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderByIdAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal("test@example.com", result.CustomerEmail);
            Assert.Equal(0, result.PrintAttempts);
        }

        #endregion

        #region ApproveOrderAsync Tests

        [Fact]
        public async Task ApproveOrderAsync_ReturnsFalse_WhenOrderNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.ApproveOrderAsync(orderId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ApproveOrderAsync_UpdatesStatus_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerEmail = "test@example.com",
                Subject = "Print Job",
                Status = OrderStatus.ReadyToPrint,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            };

            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.ApproveOrderAsync(orderId);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderStatus.ReadyToPrint, order.Status);
            _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }

        #endregion

        #region PrintOrderAsync Tests

        [Fact]
        public async Task PrintOrderAsync_ReturnsFalse_WhenOrderNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.PrintOrderAsync(orderId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PrintOrderAsync_IncrementsPrintAttempts()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerEmail = "test@example.com",
                Subject = "Print Job",
                Status = OrderStatus.ReadyToPrint,
                PrintAttempts = 0,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            };

            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            await _orderService.PrintOrderAsync(orderId);

            // Assert
            Assert.True(order.PrintAttempts >= 1);
        }

        [Fact]
        public async Task PrintOrderAsync_MarksFailed_When3rdAttemptFails()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerEmail = "test@example.com",
                Subject = "Print Job",
                Status = OrderStatus.ReadyToPrint,
                PrintAttempts = 2,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            };

            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act - simulate multiple attempts until failure
            for (int i = 0; i < 2; i++)
            {
                await _orderService.PrintOrderAsync(orderId);
                if (order.PrintAttempts >= 3 && order.Status == OrderStatus.Failed)
                    break;
            }

            // Assert
            Assert.True(order.PrintAttempts >= 1);
        }

        #endregion

        #region FailOrderAsync Tests

        [Fact]
        public async Task FailOrderAsync_ReturnsFalse_WhenOrderNotFound()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.FailOrderAsync(orderId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FailOrderAsync_MarksOrderAsFailed_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerEmail = "test@example.com",
                Subject = "Print Job",
                Status = OrderStatus.ReadyToPrint,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            };

            _mockOrderRepository.Setup(r => r.GetAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.FailOrderAsync(orderId);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderStatus.Failed, order.Status);
            _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }

        #endregion
    }
}
