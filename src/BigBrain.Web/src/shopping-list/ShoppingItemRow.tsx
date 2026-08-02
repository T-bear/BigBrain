import { useState } from 'react'
import type { ShoppingItem } from './types'
export function ShoppingItemRow({item,onToggle,onEdit,onDelete}:{item:ShoppingItem;onToggle:()=>void;onEdit:(name:string,quantity:number)=>void;onDelete:()=>void}){
 const [editing,setEditing]=useState(false);const [name,setName]=useState(item.name);const [quantity,setQuantity]=useState(item.quantity)
 return <li className={`shopping-item${item.purchased?' shopping-item--purchased':''}`}>
  <label className="shopping-item__check"><input aria-label={`${item.purchased?'Återställ':'Markera som köpt'} ${item.name}`} checked={item.purchased} onChange={onToggle} type="checkbox"/><span aria-hidden="true"/></label>
  {editing?<form className="shopping-item__edit" onSubmit={e=>{e.preventDefault();onEdit(name,quantity);setEditing(false)}}><input aria-label="Varans namn" maxLength={120} value={name} onChange={e=>setName(e.target.value)}/><input aria-label="Antal" min={1} max={999} type="number" value={quantity} onChange={e=>setQuantity(Number(e.target.value))}/><button type="submit">Spara</button><button type="button" onClick={()=>setEditing(false)}>Avbryt</button></form>:<><span className="shopping-item__name">{item.name}{item.quantity>1&&<strong> × {item.quantity}</strong>}<span className="sr-only">, {item.purchased?'köpt':'inte köpt'}</span></span><details className="shopping-item__menu"><summary aria-label={`Åtgärder för ${item.name}`}>•••</summary><button type="button" onClick={()=>setEditing(true)}>Redigera</button><button type="button" onClick={onDelete}>Ta bort</button></details></>}
 </li>
}
