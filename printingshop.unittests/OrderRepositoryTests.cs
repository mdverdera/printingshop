using Xunit;
using Microsoft.EntityFrameworkCore;
using printingshop.persistence;
using printingshop.persistence.Repositories;
using printingshop.domain.Entities;
using printingshop.domain.Enums;

namespace printingshop.unittests
{
    public class OrderRepositoryTests : IAsyncLifetime
    {
        private readonly DbContextOptions<PrintDbContext> _dbContextOptions;
        private PrintDbContext _context = null!;

        public OrderRepositoryTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<PrintDbContext>()
                .UseInMemoryDatabase(databaseName: $"PrintDb_Test_{Guid.NewGuid()}")
                .Options;
        }

        public async Task InitializeAsync()
        {
            _context = new PrintDbContext(_dbContextOptions);
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_ReturnsNull_WhenOrderDoesNotExist()
        {
            // Arrange
            var repository = new OrderRepository(_context);
            var orderId = Guid.NewGuid();

            // Act
            var result = await repository.GetAsync(orderId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_ReturnsOrder_WhenOrderExists()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerEmail = "test@example.com",
                Subject = "Test Print",
                Status = OrderStatus.ReadyToPrint,
                GmailMessageId = "msg123",
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var repository = new OrderRepository(_context);

            // Act
            var result = await repository.GetAsync(order.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.Id, result.Id);
            Assert.Equal("test@example.com", result.CustomerEmail);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoOrdersExist()
        {
            // Arrange
            var repository = new OrderRepository(_context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllOrders_WhenOrdersExist()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), CustomerEmail = "test1@example.com", Subject = "Print 1", Status = OrderStatus.ReadyToPrint, CreatedAt = DateTime.UtcNow },
                new Order { Id = Guid.NewGuid(), CustomerEmail = "test2@example.com", Subject = "Print 2", Status = OrderStatus.Printed, CreatedAt = DateTime.UtcNow }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var repository = new OrderRepository(_context);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_InsertsOrder_Successfully()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerEmail = "new@example.com",
                Subject = "New Print",
                Status = OrderStatus.ReadyToPrint,
                CreatedAt = DateTime.UtcNow
            };

            var repository = new OrderRepository(_context);

            // Act
            await repository.AddAsync(order);

            // Assert
            var savedOrder = await _context.Orders.FindAsync(order.Id);
            Assert.NotNull(savedOrder);
            Assert.Equal("new@example.com", savedOrder.CustomerEmail);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_UpdatesOrder_Successfully()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerEmail = "test@example.com",
                Subject = "Original Subject",
                Status = OrderStatus.ReadyToPrint,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            order.Subject = "Updated Subject";
            order.Status = OrderStatus.Printed;

            var repository = new OrderRepository(_context);

            // Act
            await repository.UpdateAsync(order);

            // Assert
            var updatedOrder = await _context.Orders.FindAsync(order.Id);
            Assert.NotNull(updatedOrder);
            Assert.Equal("Updated Subject", updatedOrder.Subject);
            Assert.Equal(OrderStatus.Printed, updatedOrder.Status);
            Assert.NotNull(updatedOrder.UpdatedAt);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_ReturnsFalse_WhenMessageIdDoesNotExist()
        {
            // Arrange
            var repository = new OrderRepository(_context);

            // Act
            var result = await repository.ExistsAsync("nonexistent-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenMessageIdExists()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerEmail = "test@example.com",
                Subject = "Test",
                Status = OrderStatus.ReadyToPrint,
                GmailMessageId = "msg-12345",
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var repository = new OrderRepository(_context);

            // Act
            var result = await repository.ExistsAsync("msg-12345");

            // Assert
            Assert.True(result);
        }

        #endregion

        #region GetByStatusAsync Tests

        [Fact]
        public async Task GetByStatusAsync_ReturnsList_FilteredByStatus()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), CustomerEmail = "test1@example.com", Subject = "Ready", Status = OrderStatus.ReadyToPrint, CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
                new Order { Id = Guid.NewGuid(), CustomerEmail = "test2@example.com", Subject = "Printed", Status = OrderStatus.Printed, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
                new Order { Id = Guid.NewGuid(), CustomerEmail = "test3@example.com", Subject = "Ready 2", Status = OrderStatus.ReadyToPrint, CreatedAt = DateTime.UtcNow }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var repository = new OrderRepository(_context);

            // Act
            var result = await repository.GetByStatusAsync(OrderStatus.ReadyToPrint);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal(OrderStatus.ReadyToPrint, o.Status));
        }

        #endregion
    }
}
