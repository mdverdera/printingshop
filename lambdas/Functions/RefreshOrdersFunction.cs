using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using printingshop.application.DTOs;
using printingshop.application.Interfaces;
using printingshop.infrastructure;
using System.Text.Json;

namespace PrintingShopLambdas
{
    /// <summary>
    /// Lambda handler for POST /orders/refresh
    /// Scans Gmail Print Queue, creates orders, uploads to Drive, moves emails
    /// </summary>
    public class RefreshOrdersFunction
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<RefreshOrdersFunction> _logger;

        public RefreshOrdersFunction()
        {
            var serviceProvider = DependencyInjection.BuildServiceProvider();
            _orderService = serviceProvider.GetRequiredService<IOrderService>();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = factory.CreateLogger<RefreshOrdersFunction>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogInformation("RefreshOrders Lambda invoked");

                var result = await _orderService.RefreshOrdersAsync();

                var response = new ApiResponse(Success: true, Message: result);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = JsonSerializer.Serialize(response),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RefreshOrders Lambda");

                var errorResponse = new ApiResponse(
                    Success: false,
                    Message: $"Error: {ex.Message}");

                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = JsonSerializer.Serialize(errorResponse),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }
        }
    }
}
