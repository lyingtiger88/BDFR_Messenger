using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BDFR.Gateway.Marketplace;

public sealed class BankValidationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<BankValidationService> logger) : IBankValidationService
{
    public async Task<BankValidationResult> ValidateAsync(string? cardNumber, string? iban, CancellationToken ct)
    {
        cardNumber = NormalizeDigits(cardNumber);
        iban = NormalizeIban(iban);

        if (cardNumber is null && iban is null)
            return new(false, null, null, null, "none", "A valid Iranian bank card or IBAN is required.");

        if (cardNumber is not null && !IsValidCard(cardNumber))
            return new(false, null, null, null, "local", "The Iranian bank card number is invalid.");

        if (iban is not null && !IsValidIban(iban))
            return new(false, null, null, null, "local", "The Iranian IBAN is invalid.");

        var endpoint = configuration["BankValidation:Endpoint"];
        var apiKey = configuration["BankValidation:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            return new(false, null, null, null, "configuration",
                "Bank validation API is not configured.");

        try
        {
            using var client = httpClientFactory.CreateClient("BankValidation");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            request.Content = JsonContent.Create(new { cardNumber, iban });

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return new(false, null, null, null, "provider",
                    "Bank validation provider rejected the request.");

            var payload = await response.Content.ReadFromJsonAsync<ProviderResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

            if (payload?.Success != true || payload.Data is null)
                return new(false, null, null, null, "provider",
                    payload?.Message ?? "Bank validation failed.");

            var returnedIban = NormalizeIban(payload.Data.Iban) ?? iban;
            var returnedCard = NormalizeDigits(payload.Data.CardNumber);
            var last4 = returnedCard is { Length: 16 } ? returnedCard[^4..]
                : cardNumber is { Length: 16 } ? cardNumber[^4..] : null;

            return new(true, payload.Data.BankName, returnedIban, last4,
                "configured-provider", null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogError(ex, "Bank validation provider call failed.");
            return new(false, null, null, null, "provider",
                "Bank validation service is unavailable.");
        }
    }

    private static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = value.Where(char.IsDigit).ToArray();
        return digits.Length == 0 ? null : new string(digits);
    }

    private static string? NormalizeIban(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Replace(" ", "").Trim().ToUpperInvariant();
        return result.StartsWith("IR", StringComparison.Ordinal) ? result : null;
    }

    private static bool IsValidCard(string card)
    {
        if (card.Length != 16 || card.All(c => c == '0')) return false;
        var sum = 0;
        for (var i = 0; i < 16; i++)
        {
            var digit = card[i] - '0';
            if ((i & 1) == 0)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
        }
        return sum % 10 == 0;
    }

    private static bool IsValidIban(string iban)
    {
        if (iban.Length != 26 || !iban.StartsWith("IR", StringComparison.Ordinal)) return false;
        var rearranged = iban[4..] + "1827" + iban[2..4];
        if (!rearranged.All(char.IsDigit)) return false;

        var remainder = 0;
        foreach (var c in rearranged)
            remainder = (remainder * 10 + c - '0') % 97;
        return remainder == 1;
    }

    private sealed record ProviderResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] ProviderData? Data);

    private sealed record ProviderData(
        [property: JsonPropertyName("bankName")] string? BankName,
        [property: JsonPropertyName("iban")] string? Iban,
        [property: JsonPropertyName("cardNumber")] string? CardNumber);
}
