namespace printingshop.domain.Entities
{
    public class Attachment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string FileName { get; set; } = null!;
        public string GoogleDriveFileId { get; set; } = null!;
        public int PageCount { get; set; }
        public decimal SubTotalCost { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Order? Order { get; set; }
    }
}
