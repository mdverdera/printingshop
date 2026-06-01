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
    /// Lambda handler for GET /orders/{id}
    /// Retrieves detailed information about a specific order
    /// </summary>
    public class GetOrderByIdFunction
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<GetOrderByIdFunction> _logger;

        public GetOrderByIdFunction()
        {
            var serviceProvider = DependencyInjection.BuildServiceProvider();
            _orderService = serviceProvider.GetRequiredService<IOrderService>();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = factory.CreateLogger<GetOrderByIdFunction>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogInformation("GetOrderById Lambda invoked");

                if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var idString))
                {
                    return BadRequest("Order ID is required");
                }

                if (!Guid.TryParse(idString, out var orderId))
                {
                    return BadRequest("Invalid Order ID format");
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);

                if (order == null)
                {
                    return NotFound($"Order with ID {orderId} not found");
                }

                var response = new ApiResponse<OrderDetailDto>(
                    Success: true,
                    Message: "Order retrieved successfully",
                    Data: order);

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
                _logger.LogError(ex, "Error in GetOrderById Lambda");

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

        private APIGatewayProxyResponse BadRequest(string message)
        {
            var response = new ApiResponse(Success: false, Message: message);
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }

        private APIGatewayProxyResponse NotFound(string message)
        {
            var response = new ApiResponse(Success: false, Message: message);
            return new APIGatewayProxyResponse
            {
                StatusCode = 404,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
    }
}
