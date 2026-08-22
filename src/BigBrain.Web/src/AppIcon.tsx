export type AppIconName = 'home' | 'family' | 'media' | 'finance' | 'more' | 'ai' | 'admin' | 'settings' | 'chevron'

const paths: Record<AppIconName, string> = {
  home: 'M3 11.5 12 4l9 7.5M5.5 10v10h13V10M9.5 20v-6h5v6',
  family: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75',
  media: 'M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Zm5 6 6 3-6 3V9Z',
  finance: 'M4 19V9M10 19V5M16 19v-7M22 19V3M2 21h22',
  more: 'M5 12h.01M12 12h.01M19 12h.01',
  ai: 'M12 3a4 4 0 0 0-4 4v1a4 4 0 0 0-2 7.46V17a4 4 0 0 0 6 3.46A4 4 0 0 0 18 17v-1.54A4 4 0 0 0 16 8V7a4 4 0 0 0-4-4Zm0 0v18M8 8h4M12 16h4',
  admin: 'M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Zm0-12v2M12 18.5v2M3.5 12h2M18.5 12h2M5.9 5.9l1.4 1.4M16.7 16.7l1.4 1.4M18.1 5.9l-1.4 1.4M7.3 16.7l-1.4 1.4',
  settings: 'M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Zm7.4-3.5a7 7 0 0 0-.1-1l2-1.5-2-3.4-2.4 1a8 8 0 0 0-1.7-1L15 3.5h-4l-.3 2.6a8 8 0 0 0-1.7 1l-2.4-1-2 3.4 2 1.5a7 7 0 0 0 0 2l-2 1.5 2 3.4 2.4-1a8 8 0 0 0 1.7 1l.3 2.6h4l.3-2.6a8 8 0 0 0 1.7-1l2.4 1 2-3.4-2-1.5a7 7 0 0 0 .1-1Z',
  chevron: 'm9 18 6-6-6-6',
}

export function AppIcon({ name, size = 22 }: { name: AppIconName; size?: number }) {
  return <svg aria-hidden="true" className="app-icon" fill="none" height={size} viewBox="0 0 24 24" width={size}><path d={paths[name]} stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.7" /></svg>
}
