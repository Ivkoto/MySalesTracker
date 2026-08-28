using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Application.DTOs;

public sealed record ProductPricing(
    int ProductId,
    string ProductName,
    Brand Brand,
    bool IsFreePrice,
    IReadOnlyList<PriceRuleDetails> CurrentRules);

public sealed record PriceRuleDetails(
    decimal Price,
    Currency Currency,
    int UnitsPerSale,
    int SortOrder,
    bool IsDefault);

public sealed record UpdateProductPriceRulesRequest(
    int ProductId,
    IReadOnlyList<PriceRuleInput> Rules);

public sealed record PriceRuleInput(decimal Price, int UnitsPerSale, bool IsDefault);
