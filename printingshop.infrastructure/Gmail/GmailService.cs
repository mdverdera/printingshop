using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using printingshop.application.DTOs;
using printingshop.application.Interfaces;
using System.Text;

namespace printingshop.infrastructure.Gmail
{
    public class GmailProvider : IGmailService
    {
        private readonly Google.Apis.Gmail.v1.GmailService _gmailApiService;
        private const string PrintQueueLabel = "Print Queue";
        private const string ReadyToPrintLabel = "Ready To Print";
        private const string PrintedLabel = "Printed";
        private const string FailedLabel = "Failed";

        public GmailProvider(Google.Apis.Gmail.v1.GmailService gmailApiService)
        {
            _gmailApiService = gmailApiService ?? throw new ArgumentNullException(nameof(gmailApiService));
        }

        public async Task<List<GmailMessageDto>> GetPrintQueueEmailsAsync()
        {
            try
            {
                var messages = new List<GmailMessageDto>();

                var listRequest = _gmailApiService.Users.Messages.List("me");
                listRequest.Q = $"label:{PrintQueueLabel.ToLower()}";
                listRequest.MaxResults = 100;

                var result = await listRequest.ExecuteAsync();

                if (result?.Messages == null)
                    return messages;

                foreach (var messageMetadata in result.Messages)
                {
                    var message = await GetMessageWithAttachmentsAsync(messageMetadata.Id);
                    if (message != null)
                        messages.Add(message);
                }

                return messages;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching Gmail messages: {ex.Message}", ex);
            }
        }

        public async Task MoveEmailToLabelAsync(string messageId, string labelName)
        {
            try
            {
                var labelId = await GetLabelIdAsync(labelName);
                if (string.IsNullOrEmpty(labelId))
                    throw new Exception($"Label '{labelName}' not found");

                var printQueueLabelId = await GetLabelIdAsync(PrintQueueLabel);
                var modifyRequest = _gmailApiService.Users.Messages.Modify(
                    new ModifyMessageRequest
                    {
                        AddLabelIds = new List<string> { labelId },
                        RemoveLabelIds = string.IsNullOrEmpty(printQueueLabelId) ? [] : new List<string> { printQueueLabelId }
                    },
                    "me",
                    messageId);

                await modifyRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error moving email to label: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> DownloadAttachmentAsync(string messageId, string attachmentId)
        {
            try
            {
                var getRequest = _gmailApiService.Users.Messages.Attachments.Get("me", messageId, attachmentId);
                var attachment = await getRequest.ExecuteAsync();

                if (attachment?.Data == null)
                    throw new Exception("Attachment data is empty");

                // Gmail returns base64url encoded data
                var bytes = Convert.FromBase64String(attachment.Data
                    .Replace('-', '+')
                    .Replace('_', '/'));

                return bytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error downloading attachment: {ex.Message}", ex);
            }
        }

        private async Task<GmailMessageDto?> GetMessageWithAttachmentsAsync(string messageId)
        {
            try
            {
                var getRequest = _gmailApiService.Users.Messages.Get("me", messageId);
                getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                var message = await getRequest.ExecuteAsync();

                if (message == null)
                    return null;

                var headers = message.Payload?.Headers ?? [];
                var from = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "unknown";
                var subject = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(No Subject)";

                var bodyText = ExtractBody(message.Payload);
                var attachments = await ExtractAttachmentsAsync(messageId, message.Payload);

                return new GmailMessageDto(
                    messageId,
                    message.ThreadId,
                    from,
                    subject,
                    bodyText,
                    attachments);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting message details: {ex.Message}", ex);
            }
        }

        private async Task<List<GmailAttachmentDto>> ExtractAttachmentsAsync(string messageId, MessagePart? payload)
        {
            var attachments = new List<GmailAttachmentDto>();

            if (payload?.Parts == null || payload.Parts.Count == 0)
                return attachments;

            foreach (var part in payload.Parts)
            {
                if (!string.IsNullOrEmpty(part.Filename))
                {
                    try
                    {
                        var fileData = await DownloadAttachmentAsync(messageId, part.Body?.AttachmentId ?? string.Empty);
                        attachments.Add(new GmailAttachmentDto(
                            part.Filename,
                            fileData,
                            part.MimeType ?? "application/octet-stream"));
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other attachments
                        System.Diagnostics.Debug.WriteLine($"Error downloading attachment {part.Filename}: {ex.Message}");
                    }
                }

                // Recursively check nested parts
                if (part.Parts is { Count: > 0 })
                {
                    attachments.AddRange(await ExtractAttachmentsAsync(messageId, part));
                }
            }

            return attachments;
        }

        private static string ExtractBody(MessagePart? payload)
        {
            try
            {
                if (payload?.Parts == null)
                {
                    return !string.IsNullOrEmpty(payload?.Body?.Data)
                        ? DecodeBase64Url(payload.Body.Data)
                        : string.Empty;
                }

                foreach (var part in payload.Parts)
                {
                    if (part.MimeType == "text/plain" && !string.IsNullOrEmpty(part.Body?.Data))
                    {
                        return DecodeBase64Url(part.Body.Data);
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string DecodeBase64Url(string base64Url)
        {
            try
            {
                var base64 = base64Url
                    .Replace('-', '+')
                    .Replace('_', '/');

                var paddingNeeded = (4 - (base64.Length % 4)) % 4;
                base64 += new string('=', paddingNeeded);

                var bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64Url;
            }
        }

        private async Task<string?> GetLabelIdAsync(string labelName)
        {
            try
            {
                var listRequest = _gmailApiService.Users.Labels.List("me");
                var result = await listRequest.ExecuteAsync();

                return result?.Labels
                    ?.FirstOrDefault(l => l.Name == labelName)
                    ?.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting label ID: {ex.Message}", ex);
            }
        }
    }
}
