namespace printingshop.application.DTOs
{
    public record ApiResponse<T>(
        bool Success,
        string? Message,
        T? Data);

    public record ApiResponse(
        bool Success,
        string? Message);
}
