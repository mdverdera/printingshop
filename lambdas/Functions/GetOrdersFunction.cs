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
    /// Lambda handler for GET /orders
    /// Lists all orders with pagination support
    /// </summary>
    public class GetOrdersFunction
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<GetOrdersFunction> _logger;

        public GetOrdersFunction()
        {
            var serviceProvider = DependencyInjection.BuildServiceProvider();
            _orderService = serviceProvider.GetRequiredService<IOrderService>();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = factory.CreateLogger<GetOrdersFunction>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogInformation("GetOrders Lambda invoked");

                var orders = await _orderService.GetAllOrdersAsync();

                var response = new ApiResponse<List<OrderListDto>>(
                    Success: true,
                    Message: $"Retrieved {orders.Count} orders",
                    Data: orders);

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
                _logger.LogError(ex, "Error in GetOrders Lambda");

                var errorResponse = new ApiResponse<object>(
                    Success: false,
                    Message: $"Error: {ex.Message}",
                    Data: null);

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
