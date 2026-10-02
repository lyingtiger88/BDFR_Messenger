using System.Security.Claims;
using BDFR.Database;
using BDFR.Database.Models;
using BDFR.Gateway.Contracts;
using BDFR.Gateway.Marketplace;
using BDFR.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Controllers;

[ApiController]
[Authorize]
[Route("api/marketplace")]
public sealed class MarketplaceController(
    MessengerDbContext db,
    IBankValidationService bankValidation,
    AesGcmDataProtector protector,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("stores")]
    public async Task<ActionResult<StoreResponse>> CreateStore(CreateStoreRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
            return BadRequest(new { error = "Store name must be 1-120 characters." });

        if (request.Kind is < StoreKind.Products or > StoreKind.ProductsAndServices)
            return BadRequest(new { error = "Invalid store kind." });

        if (string.IsNullOrWhiteSpace(request.CardNumber) && string.IsNullOrWhiteSpace(request.Iban))
            return BadRequest(new { error = "Seller verification requires a valid bank card or IBAN." });

        if (await db.MarketplaceStores.AnyAsync(x => x.OwnerUserId == userId.Value, ct))
            return Conflict(new { error = "This profile already has a store." });

        var validation = await bankValidation.ValidateAsync(request.CardNumber, request.Iban, request.NationalCode, request.BirthDate, ct);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Error ?? "Bank verification failed." });

        var owner = await db.Users.FirstAsync(x => x.Id == userId.Value, ct);
        var identityKey = GetDataKey();
        if (!string.IsNullOrWhiteSpace(request.NationalCode))
            owner.NationalCodeEncrypted = protector.Encrypt(request.NationalCode.Trim(), identityKey);
        if (!string.IsNullOrWhiteSpace(request.BirthDate))
            owner.BirthDateEncrypted = protector.Encrypt(request.BirthDate.Trim(), identityKey);

        var store = new MarketplaceStore
        {
            OwnerUserId = userId.Value,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Kind = request.Kind,
            BankVerification = CreateVerification(request, validation)
        };

        db.MarketplaceStores.Add(store);
        owner.IsSellerVerified = true;
        owner.SellerVerifiedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Created($"/api/marketplace/stores/{store.Id}", ToResponse(store));
    }

    [HttpGet("stores/me")]
    public async Task<ActionResult<StoreResponse>> GetMyStore(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var store = await db.MarketplaceStores
            .Include(x => x.BankVerification)
            .FirstOrDefaultAsync(x => x.OwnerUserId == userId.Value, ct);

        return store is null ? NotFound() : Ok(ToResponse(store));
    }

    [HttpPut("stores/me")]
    public async Task<ActionResult<StoreResponse>> UpdateMyStore(CreateStoreRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var store = await db.MarketplaceStores
            .Include(x => x.BankVerification)
            .FirstOrDefaultAsync(x => x.OwnerUserId == userId.Value, ct);

        if (store is null) return NotFound();

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
            return BadRequest(new { error = "Store name must be 1-120 characters." });

        store.Name = name;
        store.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        store.Kind = request.Kind;
        store.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.CardNumber) || !string.IsNullOrWhiteSpace(request.Iban))
        {
            var validation = await bankValidation.ValidateAsync(request.CardNumber, request.Iban, request.NationalCode, request.BirthDate, ct);
            if (!validation.IsValid)
                return BadRequest(new { error = validation.Error ?? "Bank verification failed." });

            store.BankVerification = CreateVerification(request, validation, store.BankVerification);
            var owner = await db.Users.FirstAsync(x => x.Id == userId.Value, ct);
            var identityKey = GetDataKey();
            if (!string.IsNullOrWhiteSpace(request.NationalCode))
                owner.NationalCodeEncrypted = protector.Encrypt(request.NationalCode.Trim(), identityKey);
            if (!string.IsNullOrWhiteSpace(request.BirthDate))
                owner.BirthDateEncrypted = protector.Encrypt(request.BirthDate.Trim(), identityKey);
            owner.IsSellerVerified = true;
            owner.SellerVerifiedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(store));
    }

    private SellerBankVerification CreateVerification(
        CreateStoreRequest request,
        BankValidationResult validation,
        SellerBankVerification? existing = null)
    {
        var verification = existing ?? new SellerBankVerification();
        var key = GetDataKey();

        if (!string.IsNullOrWhiteSpace(request.CardNumber))
        {
            verification.CardNumberEncrypted = protector.Encrypt(request.CardNumber.Trim(), key);
            verification.CardLast4 = validation.CardLast4;
        }

        if (!string.IsNullOrWhiteSpace(request.Iban))
            verification.IbanEncrypted = protector.Encrypt(request.Iban.Trim().ToUpperInvariant(), key);

        verification.BankName = validation.BankName;
        verification.Status = SellerVerificationStatus.Verified;
        verification.Provider = validation.Provider;
        verification.VerifiedAt = DateTimeOffset.UtcNow;
        return verification;
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private byte[] GetDataKey()
    {
        var value = configuration["Security:DataEncryptionKeyBase64"]
            ?? throw new InvalidOperationException("Data encryption key is missing.");
        var key = Convert.FromBase64String(value);
        if (key.Length != 32) throw new InvalidOperationException("Data encryption key must be 32 bytes.");
        return key;
    }

    private static StoreResponse ToResponse(MarketplaceStore store) => new(
        store.Id,
        store.OwnerUserId,
        store.Name,
        store.Description,
        store.Kind,
        store.IsActive,
        store.BankVerification?.Status ?? SellerVerificationStatus.Pending,
        store.BankVerification?.BankName,
        store.BankVerification?.CardLast4,
        store.CreatedAt,
        store.UpdatedAt);
}
