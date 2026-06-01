namespace printingshop.application.Commands;

/// <summary>
/// Approve order command - mark order as ReadyToPrint
/// </summary>
public record ApproveOrderCommand(Guid OrderId);
