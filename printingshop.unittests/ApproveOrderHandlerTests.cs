using Xunit;
using Moq;
using printingshop.application.Services;
using printingshop.application.Interfaces;
using printingshop.domain.Interfaces;
using printingshop.domain.Entities;
using printingshop.domain.Enums;

namespace printingshop.unittests;

/// <summary>
/// Unit tests for ApproveOrderHandler
/// Tests order approval logic
/// </summary>
public class ApproveOrderHandlerTests
{
    private readonly Mock<IGmailService> _mockGmail;
    private readonly Mock<IGoogleDriveService> _mockDrive;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly OrderService _orderService;

    public ApproveOrderHandlerTests()
    {
        _mockGmail = new Mock<IGmailService>();
        _mockDrive = new Mock<IGoogleDriveService>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _orderService = new OrderService(_mockGmail.Object, _mockDrive.Object, _mockOrderRepository.Object);
    }

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
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task ApproveOrderAsync_ReturnsTrue_WhenOrderExists()
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
        _mockOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ApproveOrderAsync_MaintainsReadyToPrintStatus()
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
        await _orderService.ApproveOrderAsync(orderId);

        // Assert
        Assert.Equal(OrderStatus.ReadyToPrint, order.Status);
    }

    [Fact]
    public async Task ApproveOrderAsync_CallsUpdateAsync()
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
        await _orderService.ApproveOrderAsync(orderId);

        // Assert
        _mockOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ApproveOrderAsync_HandlesPrintedOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerEmail = "test@example.com",
            Subject = "Print Job",
            Status = OrderStatus.Printed,
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
        // Status should be set to ReadyToPrint (for re-printing if needed)
        Assert.Equal(OrderStatus.ReadyToPrint, order.Status);
    }

    [Fact]
    public async Task ApproveOrderAsync_HandlesFailedOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerEmail = "test@example.com",
            Subject = "Print Job",
            Status = OrderStatus.Failed,
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
        // Status should be reset to ReadyToPrint for retry
        Assert.Equal(OrderStatus.ReadyToPrint, order.Status);
    }

    [Fact]
    public async Task ApproveOrderAsync_PresetsOrdersCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order
            {
                Id = Guid.NewGuid(),
                CustomerEmail = "customer1@example.com",
                Subject = "Print 1",
                Status = OrderStatus.Printed,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                Id = Guid.NewGuid(),
                CustomerEmail = "customer2@example.com",
                Subject = "Print 2",
                Status = OrderStatus.Failed,
                Attachments = new(),
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var order in orders)
        {
            _mockOrderRepository.Setup(r => r.GetAsync(order.Id))
                .ReturnsAsync(order);

            _mockOrderRepository.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.ApproveOrderAsync(order.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderStatus.ReadyToPrint, order.Status);
        }
    }
}
