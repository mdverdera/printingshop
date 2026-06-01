namespace printingshop.application.DTOs
{
    public record OrderDetailDto(
        Guid Id,
        string CustomerEmail,
        string Subject,
        string Status,
        int PrintAttempts,
        List<AttachmentDetailDto> Attachments,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public record AttachmentDetailDto(
        Guid Id,
        string FileName,
        string GoogleDriveFileId,
        int PageCount,
        decimal SubTotalCost);

    public record OrderListDto(
        Guid Id,
        string CustomerEmail,
        string Subject,
        string Status,
        int AttachmentCount,
        DateTime CreatedAt);
}
