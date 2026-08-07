using System.Net;
using System.Net.Http.Json;
using BigBrain.Api;
using BigBrain.Api.ShoppingList;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BigBrain.Api.Tests;

public sealed class ShoppingListApiTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),$"bigbrain-shopping-api-{Guid.NewGuid():N}");

    [Fact]
    public async Task FullItemFlowUsesVersionedRoutes()
    {
        await using var factory=Factory();using var client=factory.CreateClient();
        var created=(await (await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest(" Bananer ",3),TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<ShoppingItem>(TestContext.Current.CancellationToken))!;
        Assert.Equal(3,created.Quantity);
        Assert.True((await (await client.PostAsync($"/api/v1/modules/shopping-list/items/{created.Id}/purchase",JsonContent.Create(new{}),TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<ShoppingItem>(TestContext.Current.CancellationToken))!.Purchased);
        Assert.False((await (await client.PostAsync($"/api/v1/modules/shopping-list/items/{created.Id}/restore",JsonContent.Create(new{}),TestContext.Current.CancellationToken)).Content.ReadFromJsonAsync<ShoppingItem>(TestContext.Current.CancellationToken))!.Purchased);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/v1/modules/shopping-list/items/{created.Id}",TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task ValidationDuplicateAndNotFoundUseStableProblemDetails()
    {
        await using var factory=Factory();using var client=factory.CreateClient();
        var invalid=await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest(" ",1),TestContext.Current.CancellationToken);
        (await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest("Mjölk",1),TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var duplicate=await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest("  mjölk  ",1),TestContext.Current.CancellationToken);
        var missing=await client.PostAsync("/api/v1/modules/shopping-list/items/missing/purchase",JsonContent.Create(new{}),TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);Assert.Contains(ShoppingListErrorCodes.InvalidRequest,await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Conflict,duplicate.StatusCode);Assert.Contains(ShoppingListErrorCodes.Duplicate,await duplicate.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound,missing.StatusCode);var body=await missing.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);Assert.Contains(ShoppingListErrorCodes.NotFound,body,StringComparison.Ordinal);Assert.DoesNotContain(directory,body,StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> Factory()=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureTestServices(services=>{services.RemoveAll<ShoppingListOptions>();services.RemoveAll<ShoppingListStore>();services.AddSingleton(new ShoppingListOptions{DatabasePath=Path.Combine(directory,"shopping.db")});services.AddSingleton<ShoppingListStore>();}));

    [Fact]
    public async Task SimilarDuplicateRequiresExplicitAddAnyway()
    {
        await using var factory=Factory();using var client=factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest("Lördagsgodis",1),TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        var similar=await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest("lordags godis",1),TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict,similar.StatusCode);Assert.Contains(ShoppingListErrorCodes.SimilarDuplicate,await similar.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),StringComparison.Ordinal);
        (await client.PostAsJsonAsync("/api/v1/modules/shopping-list/items",new AddShoppingItemRequest("lordags godis",1,true),TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
