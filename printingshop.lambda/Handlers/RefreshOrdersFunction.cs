using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using printingshop.application.DTOs;
using printingshop.application.Interfaces;
using System.Text.Json;

namespace printingshop.lambda.Handlers;

/// <summary>
/// Lambda handler for POST /orders/refresh
/// Scans Gmail Print Queue, creates orders, uploads files to Google Drive
/// </summary>
public class RefreshOrdersFunction
{
    private readonly IOrderService _orderService;
    private readonly ILambdaLogger _logger;

    public RefreshOrdersFunction()
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
        _logger.LogInformation("Refresh orders Lambda triggered");

        try
        {
            var result = await _orderService.RefreshOrdersAsync();
            var response = new ApiResponse<string>(true, "Orders refreshed successfully", result);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error refreshing orders: {ex.Message}");
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
