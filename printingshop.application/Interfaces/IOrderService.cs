using printingshop.application.DTOs;

namespace printingshop.application.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderListDto>> GetAllOrdersAsync();
        Task<OrderDetailDto?> GetOrderByIdAsync(Guid id);
        Task<string> RefreshOrdersAsync();
        Task<bool> ApproveOrderAsync(Guid id);
        Task<bool> PrintOrderAsync(Guid id);
        Task<bool> FailOrderAsync(Guid id);
    }
}
