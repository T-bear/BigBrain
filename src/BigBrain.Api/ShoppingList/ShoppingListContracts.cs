namespace BigBrain.Api.ShoppingList;

public sealed record ShoppingListOptions
{
    public const string SectionName = "ShoppingList";
    public string DatabasePath { get; init; } = "data/shopping-list.db";
}

public sealed record ShoppingItem(string Id, string Name, string NormalizedName, int Quantity, bool Purchased,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int SortOrdinal);
public sealed record ShoppingListSnapshot(IReadOnlyList<ShoppingItem> Items, string? SessionId);
public sealed record ShoppingSuggestion(string Name, string Source);
public sealed record FrequentItem(string Name, int PurchaseCount, DateTimeOffset? LastPurchasedAtUtc);
public sealed record FinishShoppingResult(int ArchivedCount, int RemainingCount);
public sealed record AddShoppingItemRequest(string? Name, int Quantity = 1, bool AddAnyway = false);
public sealed record UpdateShoppingItemRequest(string? Name, int Quantity = 1);
public sealed record FinishShoppingRequest(bool KeepUnpurchased);

public static class ShoppingListErrorCodes
{
    public const string InvalidRequest = "shoppingListInvalidRequest";
    public const string Duplicate = "shoppingListDuplicate";
    public const string PurchasedDuplicate = "shoppingListPurchasedDuplicate";
    public const string SimilarDuplicate = "shoppingListSimilarDuplicate";
    public const string NotFound = "shoppingListItemNotFound";
    public const string Unavailable = "shoppingListUnavailable";
}

public sealed class ShoppingListException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
public sealed class ShoppingListUnavailableException() : Exception("Shopping list storage is unavailable.");
