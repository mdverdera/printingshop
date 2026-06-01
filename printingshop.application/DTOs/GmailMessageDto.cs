namespace printingshop.application.DTOs
{
    public record GmailMessageDto(
        string MessageId,
        string ThreadId,
        string From,
        string Subject,
        string BodyText,
        List<GmailAttachmentDto> Attachments);
}
