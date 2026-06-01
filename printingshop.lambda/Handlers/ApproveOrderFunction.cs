using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using printingshop.application.DTOs;
using printingshop.application.Interfaces;
using System.Text.Json;

namespace printingshop.lambda.Handlers;

/// <summary>
/// Lambda handler for POST /orders/{id}/approve
/// Marks order as ReadyToPrint
/// </summary>
public class ApproveOrderFunction
{
    private readonly IOrderService _orderService;

    public ApproveOrderFunction()
    {
        var serviceProvider = LambdaStartup.GetServiceProvider();
        _orderService = serviceProvider.GetRequiredService<IOrderService>();
    }

    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.Log("Approve order Lambda triggered");

        try
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("id", out var idString))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new ApiResponse(false, "Order ID is required")),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            if (!Guid.TryParse(idString, out var orderId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new ApiResponse(false, "Invalid order ID format")),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var success = await _orderService.ApproveOrderAsync(orderId);

            if (!success)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 404,
                    Body = JsonSerializer.Serialize(new ApiResponse(false, "Order not found")),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var response = new ApiResponse(true, "Order approved successfully");

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(response),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            context.Logger.Log($"Error approving order: {ex.Message}");
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
