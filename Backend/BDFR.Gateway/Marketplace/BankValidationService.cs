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
        string? accountNumber,
        string? bankCode,
        string? nationalCode,
        string? birthDate,
        CancellationToken ct)
    {
        cardNumber = NormalizeDigits(cardNumber);
        iban = NormalizeIban(iban);
        accountNumber = NormalizeAccount(accountNumber);
        bankCode = NormalizeDigits(bankCode);
        nationalCode = NormalizeDigits(nationalCode);
        birthDate = NormalizeBirthDate(birthDate);

        var identifiers = new[] { cardNumber, iban, accountNumber }
            .Count(x => !string.IsNullOrWhiteSpace(x));

        if (identifiers != 1)
            return Fail("local", "Provide exactly one of bank card, IBAN, or bank account number.");

        if (nationalCode is null || nationalCode.Length != 10)
            return Fail("local", "A valid 10-digit national code is required.");

        if (cardNumber is not null && !IsValidCard(cardNumber))
            return Fail("local", "The Iranian bank card number is invalid.");

        if (iban is not null && !IsValidIban(iban))
            return Fail("local", "The Iranian IBAN is invalid.");

        if (accountNumber is not null && string.IsNullOrWhiteSpace(bankCode))
            return Fail("local", "Bank code is required when verifying a bank account number.");

        if (cardNumber is not null && birthDate is null)
            return Fail("local", "Birth date is required when verifying a bank card.");

        var token = configuration["BankValidation:ApiIrToken"];
        if (string.IsNullOrWhiteSpace(token))
            return Fail("configuration", "API.IR token is not configured.");

        try
        {
            using var client = httpClientFactory.CreateClient("BankValidation");

            string bankName;
            string? verifiedIban = iban;
            string? last4 = cardNumber is null ? null : cardNumber[^4..];

            if (cardNumber is not null)
            {
                var info = await PostAsync<ProviderEnvelope<CardInfo>>(
                    client,
                    "/api/sw1/CardToIban",
                    new { cardNumber },
                    ct);

                if (!info.Success || info.Data is null)
                    return Fail("api.ir", info.Message ?? "Card inquiry failed.");

                bankName = info.Data.BankName ?? "Unknown";
                verifiedIban ??= NormalizeIban(info.Data.Iban);

                var match = await PostAsync<ProviderEnvelope<bool>>(
                    client,
                    "/api/sw1/CardMatch",
                    new { nationalCode, birthDate, cardNumber },
                    ct);

                if (!match.Success || match.Data != true)
                    return Fail("api.ir", match.Message ?? "The bank card does not match the supplied national code and birth date.");
            }
            else
            {
                if (accountNumber is not null)
                {
                    var account = await PostAsync<ProviderEnvelope<BankAccountInfo>>(
                        client,
                        "/api/sw1/BankAccountInfo",
                        new { accountNumber, bankCode },
                        ct);

                    if (!account.Success || account.Data is null || !account.Data.Active)
                        return Fail("api.ir", account.Message ?? "The bank account is invalid or inactive.");

                    verifiedIban = NormalizeIban(account.Data.Iban);
                    if (verifiedIban is null)
                        return Fail("api.ir", "The bank account did not return a valid IBAN.");

                    bankName = account.Data.BankName ?? "Unknown";
                }
                else
                {
                    var info = await PostAsync<ProviderEnvelope<IbanInfo>>(
                        client,
                        "/api/sw1/IbanInfo",
                        new { iban },
                        ct);

                    if (!info.Success || info.Data is null || !info.Data.Active)
                        return Fail("api.ir", info.Message ?? "The IBAN is invalid or inactive.");

                    bankName = info.Data.BankName ?? "Unknown";
                }

                var match = await PostAsync<ProviderEnvelope<bool>>(
                    client,
                    "/api/sw1/IbanMatchPro",
                    new { nationalCode, iban = verifiedIban },
                    ct);

                if (!match.Success || match.Data != true)
                    return Fail("api.ir", match.Message ?? "The bank account does not match the supplied national code.");
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
        string path,
        object body,
        CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(path, body, ct);
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

    private static string? NormalizeAccount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim().Replace(" ", "").Replace("-", "");
        return result.Length is >= 5 and <= 30 ? result : null;
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

    private sealed record BankAccountInfo(
        [property: JsonPropertyName("iban")] string? Iban,
        [property: JsonPropertyName("active")] bool Active,
        [property: JsonPropertyName("bankName")] string? BankName);
}
