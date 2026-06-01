using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using printingshop.application.Interfaces;
using printingshop.application.Services;
using printingshop.domain.Interfaces;
using printingshop.infrastructure.Drive;
using printingshop.infrastructure.Gmail;
using printingshop.persistence;
using printingshop.persistence.Repositories;

namespace printingshop.infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // configuration from environment
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // DbContext - Postgres
            var connectionString = configuration["CONNECTION_STRING"] ?? configuration["Database__ConnectionString"];
            services.AddDbContext<PrintDbContext>(options =>
            {
                if (!string.IsNullOrEmpty(connectionString))
                    options.UseNpgsql(connectionString);
            });

            // Repositories and application services
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderService, OrderService>();

            // Google API clients
            var googleCredentialPath = configuration["GOOGLE_CREDENTIALS_JSON_PATH"];
            GoogleCredential? credential = null;
            if (!string.IsNullOrEmpty(googleCredentialPath) && File.Exists(googleCredentialPath))
            {
                // Use FromStreamAsync instead of obsolete FromFile
                using var credentialStream = new FileStream(googleCredentialPath, FileMode.Open, FileAccess.Read);
                var task = GoogleCredential.FromStreamAsync(credentialStream, CancellationToken.None);
                task.Wait();
                credential = task.Result;

                if (credential != null)
                {
                    var scopes = new[]
                    {
                        GmailService.ScopeConstants.GmailReadonly,
                        GmailService.ScopeConstants.GmailModify,
                        DriveService.ScopeConstants.DriveFile
                    };

                    credential = credential.CreateScoped(scopes);
                }
            }

            if (credential != null)
            {
                var ga = new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "printingshop"
                };

                var gmailApi = new Google.Apis.Gmail.v1.GmailService(ga);
                var driveApi = new Google.Apis.Drive.v3.DriveService(ga);

                services.AddSingleton(gmailApi);
                services.AddSingleton(driveApi);

                services.AddScoped<IGmailService, GmailProvider>();
                services.AddScoped<IGoogleDriveService, DriveProvider>();
            }

            return services.BuildServiceProvider();
        }
    }
}
