using printingshop.domain.Entities;

namespace printingshop.domain.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetAsync(Guid id);
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetByStatusAsync(Enums.OrderStatus status);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task<bool> ExistsAsync(string gmailMessageId);
    }
}
