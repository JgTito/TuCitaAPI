using System.Text.Json;

namespace TuCita.Infrastucture.Pagos;

internal sealed record FlowCreatePaymentRequest(
    string CommerceOrder,
    string Subject,
    decimal Amount,
    string Email,
    string OptionalJson);

internal sealed record FlowCreatePaymentResponse(
    string Url,
    string Token,
    long FlowOrder);

internal sealed record FlowPaymentStatusResponse(
    long FlowOrder,
    string CommerceOrder,
    string? RequestDate,
    int Status,
    string? Subject,
    string? Currency,
    decimal Amount,
    string? Payer,
    JsonElement? Optional,
    JsonElement? PaymentData,
    JsonElement? PendingInfo,
    string? MerchantId);
