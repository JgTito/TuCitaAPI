namespace TuCita.Infrastucture.Pagos;

public sealed class FlowOptions
{
    public const string SectionName = "Flow";

    public bool Enabled { get; init; }
    public string ApiBaseUrl { get; init; } = "https://sandbox.flow.cl/api";
    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string UrlConfirmation { get; init; } = string.Empty;
    public string UrlReturn { get; init; } = string.Empty;
    public string FrontendReturnUrl { get; init; } = "http://localhost:4200/pagos/resultado";
    public string Currency { get; init; } = "CLP";
    public int PaymentMethod { get; init; } = 9;
    public int TimeoutSeconds { get; init; } = 900;
    public int CheckoutTimeoutSeconds { get; init; } = 600;
}
