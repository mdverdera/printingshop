using printingshop.domain.Enums;

namespace printingshop.domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string CustomerEmail { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public OrderStatus Status { get; set; } = OrderStatus.ReadyToPrint;
        public int PrintAttempts { get; set; } = 0;
        public string? GmailMessageId { get; set; }
        public string? GmailThreadId { get; set; }
        public List<Attachment> Attachments { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
