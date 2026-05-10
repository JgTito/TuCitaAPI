namespace TuCita.Infrastucture.Pagos;

internal interface IFlowClient
{
    Task<FlowCreatePaymentResponse> CreatePaymentAsync(
        FlowCreatePaymentRequest request,
        CancellationToken cancellationToken);

    Task<FlowPaymentStatusResponse> GetStatusAsync(
        string token,
        CancellationToken cancellationToken);
}
