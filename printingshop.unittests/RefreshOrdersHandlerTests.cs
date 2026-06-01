using Xunit;
using Moq;
using printingshop.application.Services;
using printingshop.application.Interfaces;
using printingshop.domain.Interfaces;
using printingshop.domain.Entities;
using printingshop.domain.Enums;
using printingshop.application.DTOs;

namespace printingshop.unittests;

/// <summary>
/// Unit tests for RefreshOrdersHandler
/// Tests email scanning, file upload, and order creation
/// </summary>
public class RefreshOrdersHandlerTests
{
    private readonly Mock<IGmailService> _mockGmail;
    private readonly Mock<IGoogleDriveService> _mockDrive;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly OrderService _orderService;

    public RefreshOrdersHandlerTests()
    {
        _mockGmail = new Mock<IGmailService>();
        _mockDrive = new Mock<IGoogleDriveService>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _orderService = new OrderService(_mockGmail.Object, _mockDrive.Object, _mockOrderRepository.Object);
    }

    [Fact]
    public async Task RefreshOrdersAsync_ProcessesEmailsSuccessfully()
    {
        // Arrange
        var emailData = new List<GmailMessageDto>
        {
            new GmailMessageDto(
                "msg-001",
                "thread-001",
                "customer@example.com",
                "PRINT: Important Document",
                "Please print this document",
                new List<GmailAttachmentDto>
                {
                    new GmailAttachmentDto("document.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf")
                })
        };

        _mockGmail.Setup(g => g.GetPrintQueueEmailsAsync())
            .ReturnsAsync(emailData);

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockDrive.Setup(d => d.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("drive-file-123");

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockGmail.Setup(g => g.MoveEmailToLabelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.RefreshOrdersAsync();

        // Assert
        Assert.Contains("Processed 1", result);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _mockGmail.Verify(g => g.MoveEmailToLabelAsync("msg-001", "Ready To Print"), Times.Once);
        _mockDrive.Verify(d => d.UploadAsync(It.IsAny<Stream>(), "document.pdf"), Times.Once);
    }

    [Fact]
    public async Task RefreshOrdersAsync_SkipsDuplicateEmails()
    {
        // Arrange
        var emailData = new List<GmailMessageDto>
        {
            new GmailMessageDto("msg-001", "thread-001", "customer@example.com", "PRINT: Document", "Body", new())
        };

        _mockGmail.Setup(g => g.GetPrintQueueEmailsAsync())
            .ReturnsAsync(emailData);

        _mockOrderRepository.Setup(r => r.ExistsAsync("msg-001"))
            .ReturnsAsync(true);

        // Act
        var result = await _orderService.RefreshOrdersAsync();

        // Assert
        Assert.Contains("Processed 0", result);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task RefreshOrdersAsync_HandlesMultipleAttachments()
    {
        // Arrange
        var emailData = new List<GmailMessageDto>
        {
            new GmailMessageDto(
                "msg-001",
                "thread-001",
                "customer@example.com",
                "PRINT: Multiple Files",
                "Multiple documents",
                new List<GmailAttachmentDto>
                {
                    new GmailAttachmentDto("file1.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf"),
                    new GmailAttachmentDto("file2.docx", new byte[] { 0x50, 0x4B }, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                })
        };

        _mockGmail.Setup(g => g.GetPrintQueueEmailsAsync())
            .ReturnsAsync(emailData);

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockDrive.Setup(d => d.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("drive-file-id");

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockGmail.Setup(g => g.MoveEmailToLabelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.RefreshOrdersAsync();

        // Assert
        Assert.Contains("Processed 1", result);
        _mockDrive.Verify(d => d.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RefreshOrdersAsync_HandlesEmptyQueue()
    {
        // Arrange
        _mockGmail.Setup(g => g.GetPrintQueueEmailsAsync())
            .ReturnsAsync(new List<GmailMessageDto>());

        // Act
        var result = await _orderService.RefreshOrdersAsync();

        // Assert
        Assert.Contains("Processed 0", result);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task RefreshOrdersAsync_CreatesOrderWithCorrectDetails()
    {
        // Arrange
        var emailData = new List<GmailMessageDto>
        {
            new GmailMessageDto(
                "msg-123",
                "thread-456",
                "customer@example.com",
                "PRINT: Test Document",
                "Test body",
                new List<GmailAttachmentDto>
                {
                    new GmailAttachmentDto("test.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf")
                })
        };

        Order? capturedOrder = null;

        _mockGmail.Setup(g => g.GetPrintQueueEmailsAsync())
            .ReturnsAsync(emailData);

        _mockOrderRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _mockDrive.Setup(d => d.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("drive-file-123");

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .Returns(Task.CompletedTask);

        _mockGmail.Setup(g => g.MoveEmailToLabelAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.RefreshOrdersAsync();

        // Assert
        Assert.NotNull(capturedOrder);
        Assert.Equal("customer@example.com", capturedOrder!.CustomerEmail);
        Assert.Equal("PRINT: Test Document", capturedOrder.Subject);
        Assert.Equal("msg-123", capturedOrder.GmailMessageId);
        Assert.Equal("thread-456", capturedOrder.GmailThreadId);
        Assert.Equal(OrderStatus.ReadyToPrint, capturedOrder.Status);
        Assert.Single(capturedOrder.Attachments);
        Assert.Equal("test.pdf", capturedOrder.Attachments[0].FileName);
        Assert.Equal("drive-file-123", capturedOrder.Attachments[0].GoogleDriveFileId);
    }
}
