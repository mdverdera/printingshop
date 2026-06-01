using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using printingshop.application.DTOs;
using printingshop.application.Interfaces;
using System.Text.Json;

namespace printingshop.lambda.Handlers;

/// <summary>
/// Lambda handler for GET /orders
/// Returns list of all orders
/// </summary>
public class GetOrdersFunction
{
    private readonly IOrderService _orderService;
    private readonly ILambdaLogger _logger;

    public GetOrdersFunction()
    {
        var serviceProvider = LambdaStartup.GetServiceProvider();
        _orderService = serviceProvider.GetRequiredService<IOrderService>();
        _logger = LambdaContext?.Logger ?? throw new InvalidOperationException("Lambda context not available");
    }

    private static ILambdaContext? LambdaContext { get; set; }

    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        LambdaContext = context;
        _logger.LogInformation("Get orders Lambda triggered");

        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var response = new ApiResponse<List<OrderListDto>>(true, "Orders retrieved successfully", orders);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting orders: {ex.Message}");
            var response = new ApiResponse(false, $"Error: {ex.Message}");

            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
