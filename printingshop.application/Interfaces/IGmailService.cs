using printingshop.application.DTOs;

namespace printingshop.application.Interfaces
{
    public interface IGmailService
    {
        /// <summary>
        /// Retrieve all emails from Print Queue label containing PRINT keyword
        /// </summary>
        Task<List<GmailMessageDto>> GetPrintQueueEmailsAsync();

        /// <summary>
        /// Move email to a specific label
        /// </summary>
        Task MoveEmailToLabelAsync(string messageId, string labelName);

        /// <summary>
        /// Download attachment from Gmail
        /// </summary>
        Task<byte[]> DownloadAttachmentAsync(string messageId, string attachmentId);
    }
}
