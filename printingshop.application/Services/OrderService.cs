using printingshop.application.DTOs;
using printingshop.application.Interfaces;
using printingshop.domain.Entities;
using printingshop.domain.Enums;
using printingshop.domain.Interfaces;

namespace printingshop.application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IGmailService _gmail;
        private readonly IGoogleDriveService _drive;
        private readonly IOrderRepository _orders;

        public OrderService(
            IGmailService gmail,
            IGoogleDriveService drive,
            IOrderRepository orders)
        {
            _gmail = gmail ?? throw new ArgumentNullException(nameof(gmail));
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        }

        public async Task<string> RefreshOrdersAsync()
        {
            var emails = await _gmail.GetPrintQueueEmailsAsync();
            var processed = 0;

            foreach (var email in emails)
            {
                // skip if already exists
                if (await _orders.ExistsAsync(email.MessageId))
                    continue;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerEmail = email.From,
                    Subject = email.Subject,
                    Status = OrderStatus.ReadyToPrint,
                    GmailMessageId = email.MessageId,
                    GmailThreadId = email.ThreadId,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var att in email.Attachments)
                {
                    using var ms = new MemoryStream(att.FileData);
                    var driveId = await _drive.UploadAsync(ms, att.FileName);

                    var attachment = new Attachment
                    {
                        Id = Guid.NewGuid(),
                        FileName = att.FileName,
                        GoogleDriveFileId = driveId,
                        PageCount = 0,
                        SubTotalCost = 0m,
                        CreatedAt = DateTime.UtcNow
                    };

                    order.Attachments.Add(attachment);
                }

                await _orders.AddAsync(order);

                // Move email to Ready To Print
                await _gmail.MoveEmailToLabelAsync(email.MessageId, "Ready To Print");

                processed++;
            }

            return $"Processed {processed} emails.";
        }

        public async Task<List<OrderListDto>> GetAllOrdersAsync()
        {
            var list = await _orders.GetAllAsync();
            return list.Select(o => new OrderListDto(
                o.Id,
                o.CustomerEmail,
                o.Subject,
                o.Status.ToString(),
                o.Attachments.Count,
                o.CreatedAt)).ToList();
        }

        public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid id)
        {
            var o = await _orders.GetAsync(id);
            if (o == null) return null;

            var attachments = o.Attachments.Select(a => new AttachmentDetailDto(
                a.Id,
                a.FileName,
                a.GoogleDriveFileId,
                a.PageCount,
                a.SubTotalCost)).ToList();

            return new OrderDetailDto(
                o.Id,
                o.CustomerEmail,
                o.Subject,
                o.Status.ToString(),
                o.PrintAttempts,
                attachments,
                o.CreatedAt,
                o.UpdatedAt);
        }

        public async Task<bool> ApproveOrderAsync(Guid id)
        {
            var o = await _orders.GetAsync(id);
            if (o == null) return false;

            o.Status = OrderStatus.ReadyToPrint;
            await _orders.UpdateAsync(o);
            return true;
        }

        public async Task<bool> PrintOrderAsync(Guid id)
        {
            var o = await _orders.GetAsync(id);
            if (o == null) return false;

            // simulate print - random success for MVP (70% success rate)
            var success = Random.Shared.NextDouble() > 0.3;

            o.PrintAttempts += 1;

            if (success)
            {
                o.Status = OrderStatus.Printed;
            }
            else if (o.PrintAttempts >= 3)
            {
                o.Status = OrderStatus.Failed;
            }

            await _orders.UpdateAsync(o);
            return success;
        }

        public async Task<bool> FailOrderAsync(Guid id)
        {
            var o = await _orders.GetAsync(id);
            if (o == null) return false;

            o.Status = OrderStatus.Failed;
            await _orders.UpdateAsync(o);
            return true;
        }
    }
}
