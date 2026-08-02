namespace BigBrain.Api.ShoppingList;

public static class ShoppingListEndpoints
{
    public static IEndpointRouteBuilder MapShoppingListEndpoints(this IEndpointRouteBuilder app)
    {
        var group=app.MapGroup("/api/v1/modules/shopping-list");
        group.MapGet("/items",(ShoppingListStore s,CancellationToken t)=>Run(()=>s.GetAsync(t)));
        group.MapPost("/items",(AddShoppingItemRequest r,ShoppingListStore s,CancellationToken t)=>Run(async()=>await s.AddAsync(r.Name??"",r.Quantity,t),201));
        group.MapPut("/items/{id}",(string id,UpdateShoppingItemRequest r,ShoppingListStore s,CancellationToken t)=>Run(()=>s.UpdateAsync(id,r.Name??"",r.Quantity,t)));
        group.MapPost("/items/{id}/purchase",(string id,ShoppingListStore s,CancellationToken t)=>Run(()=>s.SetPurchasedAsync(id,true,t)));
        group.MapPost("/items/{id}/restore",(string id,ShoppingListStore s,CancellationToken t)=>Run(()=>s.SetPurchasedAsync(id,false,t)));
        group.MapPost("/items/{id}/increase",async(string id,ShoppingListStore s,CancellationToken t)=>await Run(async()=>{await s.IncreaseAsync(id,t);return await s.GetAsync(t);}));
        group.MapPost("/items/{id}/reactivate",async(string id,ShoppingListStore s,CancellationToken t)=>await Run(async()=>{await s.ReactivateAsync(id,t);return await s.GetAsync(t);}));
        group.MapDelete("/items/{id}",(string id,ShoppingListStore s,CancellationToken t)=>Run(async()=>{await s.DeleteAsync(id,t);return new { };},204));
        group.MapGet("/suggestions",(string? query,ShoppingListStore s,CancellationToken t)=>Run(()=>s.SuggestionsAsync(query??"",t)));
        group.MapGet("/frequent",(ShoppingListStore s,CancellationToken t)=>Run(()=>s.FrequentAsync(t)));
        group.MapPost("/finish",(FinishShoppingRequest r,ShoppingListStore s,CancellationToken t)=>Run(()=>s.FinishAsync(r.KeepUnpurchased,t)));
        return app;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> action,int status=200){try{var value=await action();return status==204?Results.NoContent():Results.Json(value,statusCode:status);}catch(ShoppingListException e){return Problem(e.Code,e.Message,e.StatusCode);}catch(ShoppingListUnavailableException){return Problem(ShoppingListErrorCodes.Unavailable,"Inköpslistan kunde inte laddas.",503);}}
    private static IResult Problem(string code,string detail,int status)=>Results.Problem(statusCode:status,title:"Inköpslistan kunde inte uppdateras",detail:detail,extensions:new Dictionary<string,object?>{{"code",code}});
}
