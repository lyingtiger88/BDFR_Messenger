using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BDFR.Gateway.Marketplace;

public sealed class BankValidationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<BankValidationService> logger) : IBankValidationService
{
    public async Task<BankValidationResult> ValidateAsync(
        string? cardNumber,
        string? iban,
        string? nationalCode,
        string? birthDate,
        CancellationToken ct)
    {
        cardNumber = NormalizeDigits(cardNumber);
        iban = NormalizeIban(iban);
        nationalCode = NormalizeDigits(nationalCode);
        birthDate = NormalizeBirthDate(birthDate);

        if (cardNumber is null && iban is null)
            return Fail("none", "A valid Iranian bank card or IBAN is required.");

        if (nationalCode is null || nationalCode.Length != 10)
            return Fail("local", "A valid 10-digit national code is required.");

        if (cardNumber is not null && !IsValidCard(cardNumber))
            return Fail("local", "The Iranian bank card number is invalid.");

        if (iban is not null && !IsValidIban(iban))
            return Fail("local", "The Iranian IBAN is invalid.");

        if (cardNumber is not null && birthDate is null)
            return Fail("local", "Birth date is required when verifying a bank card.");

        var token = configuration["BankValidation:ApiIrToken"];
        if (string.IsNullOrWhiteSpace(token))
            return Fail("configuration", "API.IR token is not configured.");

        try
        {
            using var client = httpClientFactory.CreateClient("BankValidation");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string bankName;
            string? verifiedIban = iban;
            string? last4 = cardNumber is null ? null : cardNumber[^4..];

            if (cardNumber is not null)
            {
                var info = await PostAsync<ProviderEnvelope<CardInfo>>(
                    client,
                    "https://s.api.ir/api/sw1/CardToIban",
                    new { cardNumber },
                    ct);

                if (!info.Success || info.Data is null)
                    return Fail("api.ir", info.Message ?? "Card inquiry failed.");

                bankName = info.Data.BankName ?? "Unknown";
                verifiedIban ??= NormalizeIban(info.Data.Iban);

                var match = await PostAsync<ProviderEnvelope<bool>>(
                    client,
                    "https://s.api.ir/api/sw1/CardMatch",
                    new { nationalCode, birthDate, cardNumber },
                    ct);

                if (!match.Success || match.Data != true)
                    return Fail("api.ir", "The bank card does not match the supplied national code and birth date.");
            }
            else
            {
                var info = await PostAsync<ProviderEnvelope<IbanInfo>>(
                    client,
                    "https://s.api.ir/api/sw1/IbanInfo",
                    new { iban },
                    ct);

                if (!info.Success || info.Data is null || !info.Data.Active)
                    return Fail("api.ir", "The IBAN is invalid or inactive.");

                bankName = info.Data.BankName ?? "Unknown";

                var match = await PostAsync<ProviderEnvelope<bool>>(
                    client,
                    "https://s.api.ir/api/sw1/IbanMatchPro",
                    new { nationalCode, iban },
                    ct);

                if (!match.Success || match.Data != true)
                    return Fail("api.ir", "The IBAN does not match the supplied national code.");
            }

            return new(true, bankName, verifiedIban, last4, "api.ir", null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Fail("api.ir", "Bank verification timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogError(ex, "API.IR bank verification failed.");
            return Fail("api.ir", "Bank verification service is unavailable.");
        }
    }

    private async Task<T> PostAsync<T>(
        HttpClient client,
        string url,
        object body,
        CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new JsonException("Empty API.IR response.");
    }

    private static BankValidationResult Fail(string provider, string error) =>
        new(false, null, null, null, provider, error);

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

    private static string? NormalizeBirthDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim().Replace("-", "/");
        var parts = result.Split('/');
        return parts.Length == 3 && parts.All(p => p.All(char.IsDigit)) ? result : null;
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

    private sealed record ProviderEnvelope<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] T? Data);

    private sealed record CardInfo(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("iban")] string? Iban,
        [property: JsonPropertyName("bankName")] string? BankName);

    private sealed record IbanInfo(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("bankName")] string? BankName,
        [property: JsonPropertyName("active")] bool Active);
}
