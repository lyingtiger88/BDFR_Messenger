namespace BDFR.Gateway.Marketplace;

public sealed record BankValidationResult(
    bool IsValid,
    string? BankName,
    string? Iban,
    string? CardLast4,
    string Provider,
    string? Error);

public interface IBankValidationService
{
    Task<BankValidationResult> ValidateAsync(
        string? cardNumber,
        string? iban,
        string? nationalCode,
        string? birthDate,
        CancellationToken cancellationToken);
}
