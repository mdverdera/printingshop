namespace printingshop.application.DTOs
{
    public record GmailAttachmentDto(
        string FileName,
        byte[] FileData,
        string MimeType);
}
