using printingshop.application.Interfaces;
using printingshop.domain.Interfaces;

namespace printingshop.application.Handlers
{
    public class RefreshOrdersHandler
    {
        private readonly IGmailService _gmail;
        private readonly IGoogleDriveService _drive;
        private readonly IOrderRepository _orders;

        public RefreshOrdersHandler(
            IGmailService gmail,
            IGoogleDriveService drive,
            IOrderRepository orders)
        {
            _gmail = gmail;
            _drive = drive;
            _orders = orders;
        }

        public async Task ExecuteAsync()
        {
            var emails =
                await _gmail
                    .GetPrintQueueEmailsAsync();

            foreach (var email in emails)
            {
                // upload files

                // create order

                // save order
            }
        }
    }
}
