import { useId, useState, type ReactNode } from 'react'

export function ExpandableList<T>({
  accessibleName,
  emptyMessage,
  items,
  renderItem,
  initialCount = 3,
}: {
  accessibleName: string
  emptyMessage: string
  items: readonly T[]
  renderItem: (item: T, index: number) => ReactNode
  initialCount?: number
}) {
  const [expanded, setExpanded] = useState(false)
  const listId = useId()
  const visibleItems = expanded ? items : items.slice(0, initialCount)

  if (items.length === 0) return <p className="muted empty-state">{emptyMessage}</p>

  return <>
    <ul className="compact-list" id={listId}>
      {visibleItems.map(renderItem)}
    </ul>
    {items.length > initialCount && (
      <button
        aria-controls={listId}
        aria-expanded={expanded}
        className="list-toggle"
        onClick={() => setExpanded(current => !current)}
        type="button">
        {expanded ? `Show fewer ${accessibleName}` : `Show all ${items.length} ${accessibleName}`}
      </button>
    )}
  </>
}
