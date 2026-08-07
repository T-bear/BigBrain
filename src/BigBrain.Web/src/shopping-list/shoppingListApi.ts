import { ApiError } from '../api'
import type { FrequentItem, ShoppingItem, ShoppingSnapshot, ShoppingSuggestion } from './types'
const base='/api/v1/modules/shopping-list'
async function request<T>(path:string,method='GET',body?:unknown):Promise<T>{const response=await fetch(base+path,{method,headers:body?{'Content-Type':'application/json'}:undefined,body:body?JSON.stringify(body):undefined});if(!response.ok){const p=await response.json().catch(()=>null) as {code?:string;detail?:string}|null;throw new ApiError(p?.code??'requestFailed',p?.detail??'Inköpslistan kunde inte uppdateras.')}return response.status===204?undefined as T:response.json()}
export const getShoppingList=()=>request<ShoppingSnapshot>('/items')
export const addShoppingItem=(name:string,quantity=1,addAnyway=false)=>request<ShoppingItem>('/items','POST',{name,quantity,addAnyway})
export const updateShoppingItem=(id:string,name:string,quantity:number)=>request<ShoppingItem>(`/items/${id}`,'PUT',{name,quantity})
export const purchaseShoppingItem=(id:string)=>request<ShoppingItem>(`/items/${id}/purchase`,'POST',{})
export const restoreShoppingItem=(id:string)=>request<ShoppingItem>(`/items/${id}/restore`,'POST',{})
export const increaseShoppingItem=(id:string)=>request<ShoppingSnapshot>(`/items/${id}/increase`,'POST',{})
export const reactivateShoppingItem=(id:string)=>request<ShoppingSnapshot>(`/items/${id}/reactivate`,'POST',{})
export const deleteShoppingItem=(id:string)=>request<void>(`/items/${id}`,'DELETE')
export const getShoppingSuggestions=(query:string)=>request<ShoppingSuggestion[]>(`/suggestions?query=${encodeURIComponent(query)}`)
export const getFrequentItems=()=>request<FrequentItem[]>('/frequent')
export const finishShopping=(keepUnpurchased:boolean)=>request<{archivedCount:number;remainingCount:number}>('/finish','POST',{keepUnpurchased})
