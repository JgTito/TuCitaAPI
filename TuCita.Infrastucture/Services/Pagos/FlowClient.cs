using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TuCita.Infrastucture.Pagos;

internal sealed class FlowClient(IOptions<FlowOptions> options) : IFlowClient
{
    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<FlowCreatePaymentResponse> CreatePaymentAsync(
        FlowCreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var parameters = BuildCreateParameters(config, request);
        parameters["s"] = FlowSignature.Sign(parameters, config.SecretKey);

        using var content = new FormUrlEncodedContent(parameters);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var response = await HttpClient.PostAsync(BuildUrl(config, "payment/create"), content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new FlowClientException($"Flow rechazó la creación del pago: {body}");
        }

        var result = JsonSerializer.Deserialize<FlowCreatePaymentResponse>(body, JsonOptions);
        return result ?? throw new FlowClientException("Flow no retornó una respuesta válida al crear el pago.");
    }

    public async Task<FlowPaymentStatusResponse> GetStatusAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["apiKey"] = config.ApiKey,
            ["token"] = token
        };
        parameters["s"] = FlowSignature.Sign(parameters, config.SecretKey);

        var query = string.Join(
            '&',
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        using var response = await HttpClient.GetAsync($"{BuildUrl(config, "payment/getStatus")}?{query}", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new FlowClientException($"Flow rechazó la consulta del pago: {body}");
        }

        var result = JsonSerializer.Deserialize<FlowPaymentStatusResponse>(body, JsonOptions);
        return result ?? throw new FlowClientException("Flow no retornó una respuesta válida al consultar el pago.");
    }

    private static Dictionary<string, string> BuildCreateParameters(
        FlowOptions config,
        FlowCreatePaymentRequest request)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["apiKey"] = config.ApiKey,
            ["commerceOrder"] = request.CommerceOrder,
            ["subject"] = request.Subject,
            ["currency"] = config.Currency,
            ["amount"] = request.Amount.ToString("0.##", CultureInfo.InvariantCulture),
            ["email"] = request.Email,
            ["paymentMethod"] = config.PaymentMethod.ToString(CultureInfo.InvariantCulture),
            ["urlConfirmation"] = config.UrlConfirmation,
            ["urlReturn"] = config.UrlReturn,
            ["optional"] = request.OptionalJson
        };

        if (config.TimeoutSeconds > 0)
        {
            parameters["timeout"] = config.TimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        }

        if (config.CheckoutTimeoutSeconds > 0)
        {
            parameters["checkout_timeout"] = config.CheckoutTimeoutSeconds.ToString(CultureInfo.InvariantCulture);
        }

        return parameters;
    }

    private static string BuildUrl(FlowOptions config, string path)
    {
        return $"{config.ApiBaseUrl.TrimEnd('/')}/{path}";
    }
}
