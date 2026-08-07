import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const css = readFileSync(resolve(process.cwd(), 'src/styles/modules.css'), 'utf8')

describe('Sprint 1 UX layout contracts', () => {
  it('constrains download information to the widget at mobile and wider sizes', () => {
    expect(css).toMatch(/\.download-control \{[^}]*min-width:0;[^}]*max-width:100%;[^}]*overflow-x:clip;/)
    expect(css).toMatch(/\.download-list li \{[^}]*min-width:0;[^}]*max-width:100%;[^}]*grid-template-columns:minmax\(0,1fr\) minmax\(0,auto\);/)
    expect(css).toMatch(/\.download-message \{[^}]*max-width:100%;[^}]*overflow-wrap:anywhere;/)
    expect(css).toContain('@media(max-width:560px){.download-control{padding:12px}')
  })

  it('keeps frequent item buttons readable in every interaction state', () => {
    expect(css).toMatch(/\.shopping-frequent button \{[^}]*background:var\(--bb-color-surface\);[^}]*color:var\(--bb-color-text\);/)
    expect(css).toMatch(/\.shopping-frequent button:hover \{[^}]*color:var\(--bb-color-text\);/)
    expect(css).toMatch(/\.shopping-frequent button:focus-visible \{[^}]*outline:var\(--bb-focus-ring\);/)
    expect(css).toMatch(/\.shopping-frequent button:active,\.shopping-frequent button\[aria-pressed=true\] \{[^}]*color:var\(--bb-color-primary-contrast\);/)
    expect(css).toMatch(/\.shopping-frequent button:disabled \{[^}]*color:var\(--bb-color-text-muted\);[^}]*opacity:1;/)
  })
})
