using Microsoft.EntityFrameworkCore;
using printingshop.domain.Entities;
using printingshop.domain.Enums;
using printingshop.domain.Interfaces;

namespace printingshop.persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly PrintDbContext _context;

        public OrderRepository(PrintDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetAsync(Guid id)
        {
            return await _context.Orders
                .Include(o => o.Attachments)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Attachments)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Include(o => o.Attachments)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(string gmailMessageId)
        {
            return await _context.Orders
                .AnyAsync(o => o.GmailMessageId == gmailMessageId);
        }
    }
}
