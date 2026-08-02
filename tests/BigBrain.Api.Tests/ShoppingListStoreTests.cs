using BigBrain.Api.ShoppingList;

namespace BigBrain.Api.Tests;

public sealed class ShoppingListStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"bigbrain-shopping-{Guid.NewGuid():N}");
    private string DatabasePath => Path.Combine(directory, "shopping.db");
    private ShoppingListStore Store() => new(new ShoppingListOptions { DatabasePath = DatabasePath });

    [Fact]
    public async Task EmptyCreateNormalizeDuplicateIncreaseEditDeleteAndPersistence()
    {
        var store=Store(); Assert.Empty((await store.GetAsync(TestContext.Current.CancellationToken)).Items);
        var milk=await store.AddAsync("  Mjölk   ",1,TestContext.Current.CancellationToken); Assert.Equal("Mjölk",milk.Name);
        var duplicate=await Assert.ThrowsAsync<ShoppingListException>(()=>store.AddAsync("mjölk",1,TestContext.Current.CancellationToken)); Assert.Equal(ShoppingListErrorCodes.Duplicate,duplicate.Code);
        await store.IncreaseAsync(milk.Id,TestContext.Current.CancellationToken); var edited=await store.UpdateAsync(milk.Id,"Mellanmjölk",3,TestContext.Current.CancellationToken); Assert.Equal(3,edited.Quantity);
        Assert.Single((await Store().GetAsync(TestContext.Current.CancellationToken)).Items);
        await store.DeleteAsync(milk.Id,TestContext.Current.CancellationToken); Assert.Empty((await store.GetAsync(TestContext.Current.CancellationToken)).Items);
    }

    [Fact]
    public async Task PurchaseRestoreFinishPreservesRemainingAndUpdatesFrequentHistory()
    {
        var store=Store(); var milk=await store.AddAsync("Mjölk",1,TestContext.Current.CancellationToken); var bread=await store.AddAsync("Bröd",1,TestContext.Current.CancellationToken);
        await store.SetPurchasedAsync(milk.Id,true,TestContext.Current.CancellationToken); Assert.True((await store.GetAsync(TestContext.Current.CancellationToken)).Items.Single(x=>x.Id==milk.Id).Purchased);
        await store.SetPurchasedAsync(milk.Id,false,TestContext.Current.CancellationToken); Assert.False((await store.GetAsync(TestContext.Current.CancellationToken)).Items.Single(x=>x.Id==milk.Id).Purchased);
        await store.SetPurchasedAsync(milk.Id,true,TestContext.Current.CancellationToken); var result=await store.FinishAsync(true,TestContext.Current.CancellationToken);
        Assert.Equal(1,result.ArchivedCount); Assert.Equal(bread.Id,Assert.Single((await store.GetAsync(TestContext.Current.CancellationToken)).Items).Id);
        Assert.Equal("Mjölk",Assert.Single(await store.FrequentAsync(TestContext.Current.CancellationToken)).Name);
        Assert.Contains(await store.SuggestionsAsync("mj",TestContext.Current.CancellationToken),x=>x.Name=="Mjölk");
    }

    [Fact]
    public async Task TwoCompletedSessionsCreateDeterministicLearnedOrderWithoutReorderingDuringChecks()
    {
        var store=Store();
        foreach(var names in new[]{new[]{"Bröd","Mjölk"},new[]{"Mjölk","Bröd"}}){var created=new List<ShoppingItem>();foreach(var name in names)created.Add(await store.AddAsync(name,1,TestContext.Current.CancellationToken));foreach(var item in created)await store.SetPurchasedAsync(item.Id,true,TestContext.Current.CancellationToken);await store.FinishAsync(true,TestContext.Current.CancellationToken);}
        var milk=await store.AddAsync("Mjölk",1,TestContext.Current.CancellationToken);var bread=await store.AddAsync("Bröd",1,TestContext.Current.CancellationToken);var before=(await store.GetAsync(TestContext.Current.CancellationToken)).Items.Select(x=>x.Id).ToArray();
        await store.SetPurchasedAsync(before[0],true,TestContext.Current.CancellationToken);var remaining=(await store.GetAsync(TestContext.Current.CancellationToken)).Items.Where(x=>!x.Purchased).Select(x=>x.Id).ToArray();Assert.Equal(before.Skip(1),remaining);Assert.Contains(milk.Id,before);Assert.Contains(bread.Id,before);
    }

    [Fact]
    public void InvalidDatabaseOnlyMakesStoreUnavailable()
    {
        Directory.CreateDirectory(directory); using var store=new ShoppingListStore(new ShoppingListOptions{DatabasePath=directory}); Assert.False(store.IsAvailable);
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
