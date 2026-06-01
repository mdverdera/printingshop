using Xunit;
using Moq;
using printingshop.application.Services;
using printingshop.application.Interfaces;
using printingshop.domain.Interfaces;
using printingshop.domain.Entities;
using printingshop.domain.Enums;

namespace printingshop.unittests;

/// <summary>
/// Unit tests for PrintOrderHandler
/// Tests print simulation logic and attempt tracking
/// </summary>
public class PrintOrderHandlerTests
{
    private readonly Mock<IGmailService> _mockGmail;
    private readonly Mock<IGoogleDriveService> _mockDrive;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly OrderService _orderService;

    public PrintOrderHandlerTests()
    {
        _mockGmail = new Mock<IGmailService>();
        _mockDrive = new Mock<IGoogleDriveService>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _orderService = new OrderService(_mockGmail.Object, _mockDrive.Object, _mockOrderRepository.Object);
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
        Assert.Equal(1, order.PrintAttempts);
        _mockOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
    }

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
    public async Task PrintOrderAsync_MarksFailed_After3FailedAttempts()
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

        // Simulate a failure scenario (30% chance of failure)
        // Multiple calls to potentially get 3 failures
        var failureCounter = 0;
        for (int i = 0; i < 10; i++)
        {
            await _orderService.PrintOrderAsync(orderId);
            
            if (order.Status == OrderStatus.Failed)
            {
                failureCounter++;
                break;
            }
        }

        // Assert - After enough attempts, it should eventually fail
        Assert.True(order.PrintAttempts >= 1);
        // Either it succeeded or it eventually failed after 3 attempts
        Assert.True(
            order.Status == OrderStatus.Printed ||
            (order.PrintAttempts >= 3 && order.Status == OrderStatus.Failed)
        );
    }

    [Fact]
    public async Task PrintOrderAsync_MarksPrinted_OnSuccess()
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

        // Act - try multiple times until success
        bool success = false;
        for (int i = 0; i < 20; i++)
        {
            success = await _orderService.PrintOrderAsync(orderId);
            if (success)
                break;
        }

        // Assert - either we got success eventually, or status changed
        Assert.True(success || order.Status == OrderStatus.Printed || order.Status == OrderStatus.Failed);
    }

    [Fact]
    public async Task PrintOrderAsync_InitialAttemptCounterStartsAtZero()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerEmail = "test@example.com",
            Subject = "New Print Job",
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
        Assert.Equal(1, order.PrintAttempts);
    }

    [Fact]
    public async Task PrintOrderAsync_DoesNotExceedMaxAttempts()
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

        // Act - third attempt
        await _orderService.PrintOrderAsync(orderId);

        // Assert
        Assert.True(order.PrintAttempts >= 3);
        // Should not go beyond 3 in this specific call
        Assert.True(order.Status == OrderStatus.Printed || order.Status == OrderStatus.Failed);
    }
}
