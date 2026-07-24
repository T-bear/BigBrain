import type { ComponentType } from 'react'

export type DashboardSectionId = 'hero' | 'insights' | 'widgets' | 'activity' | 'details' | (string & {})

export interface WidgetProps<TData> {
  data: TData
}

export interface WidgetRegistration<TData> {
  id: string
  title: string
  section: DashboardSectionId
  order: number
  component: ComponentType<WidgetProps<TData>>
  supportedStates: readonly string[]
}

export interface DashboardSectionRegistration {
  id: DashboardSectionId
  order: number
  className: string
  label?: string
  title?: string
}

export class WidgetRegistry<TData> {
  private readonly widgets: readonly WidgetRegistration<TData>[]
  private readonly sections: readonly DashboardSectionRegistration[]

  public constructor(
    widgets: readonly WidgetRegistration<TData>[],
    sections: readonly DashboardSectionRegistration[])
  {
    this.widgets = [...widgets]
    this.sections = [...sections]
  }

  public getSections(): readonly DashboardSectionRegistration[] {
    return [...this.sections].sort((left, right) => left.order - right.order)
  }

  public getWidgets(section: DashboardSectionId, state: string): readonly WidgetRegistration<TData>[] {
    return this.widgets
      .filter(widget => widget.section === section && widget.supportedStates.includes(state))
      .sort((left, right) => left.order - right.order)
  }
}
