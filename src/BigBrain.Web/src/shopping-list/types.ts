export interface ShoppingItem { id:string; name:string; normalizedName:string; quantity:number; purchased:boolean; createdAtUtc:string; updatedAtUtc:string; sortOrdinal:number }
export interface ShoppingSnapshot { items:ShoppingItem[]; sessionId:string|null }
export interface ShoppingSuggestion { name:string; source:string }
export interface FrequentItem { name:string; purchaseCount:number; lastPurchasedAtUtc:string|null }
