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
    /// Lambda handler for POST /orders/{id}/fail
    /// Marks order as Failed (manual failure marking)
    /// </summary>
    public class FailOrderFunction
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<FailOrderFunction> _logger;

        public FailOrderFunction()
        {
            var serviceProvider = DependencyInjection.BuildServiceProvider();
            _orderService = serviceProvider.GetRequiredService<IOrderService>();
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = factory.CreateLogger<FailOrderFunction>();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                _logger.LogInformation("FailOrder Lambda invoked");

                if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var idString))
                {
                    return BadRequest("Order ID is required");
                }

                if (!Guid.TryParse(idString, out var orderId))
                {
                    return BadRequest("Invalid Order ID format");
                }

                var success = await _orderService.FailOrderAsync(orderId);

                if (!success)
                {
                    return NotFound($"Order with ID {orderId} not found");
                }

                var response = new ApiResponse(
                    Success: true,
                    Message: "Order marked as Failed");

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
                _logger.LogError(ex, "Error in FailOrder Lambda");

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
