import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const css = readFileSync(resolve(process.cwd(), 'src/styles/modules.css'), 'utf8')
const audiobookRoutes = readFileSync(resolve(process.cwd(), 'src/styles/audiobook-routes.css'), 'utf8')
const audiobooks = readFileSync(resolve(process.cwd(), 'src/styles/audiobooks.css'), 'utf8')
const appShell = readFileSync(resolve(process.cwd(), 'src/AppShell.tsx'), 'utf8')

describe('Sprint 1 UX layout contracts', () => {
  it('constrains download information to the widget at mobile and wider sizes', () => {
    expect(css).toMatch(/\.download-control \{[^}]*min-width:0;[^}]*max-width:100%;[^}]*overflow-x:clip;/)
    expect(css).toMatch(/\.download-list>li \{[^}]*min-width:0;[^}]*max-width:100%;[^}]*grid-template-columns:auto minmax\(0,1fr\);/)
    expect(css).toMatch(/\.download-message \{[^}]*max-width:100%;[^}]*overflow-wrap:anywhere;/)
    expect(css).toContain('@media(max-width:560px){.download-control{padding:12px}')
  })

  it('keeps grouped download navigation responsive with adequate mobile touch targets', () => {
    expect(css).toMatch(/\.download-groups,\.download-group \{[^}]*min-width:0;[^}]*max-width:100%;/)
    expect(css).toMatch(/\.download-group__toggle \{[^}]*min-height:44px;/)
    expect(css).toContain('@media(max-width:560px){.download-control{padding:12px}')
    expect(css).toContain('.download-group>header{align-items:stretch;flex-direction:column}')
    expect(css).toMatch(/\.download-row-actions button,\.download-batch button \{[^}]*width:100%;[^}]*max-width:100%;[^}]*min-height:44px;/)
    expect(css).not.toMatch(/\.download-group[^}]*position:fixed/)
  })

  it('keeps frequent item buttons readable in every interaction state', () => {
    expect(css).toMatch(/\.shopping-frequent button \{[^}]*background:var\(--bb-color-surface\);[^}]*color:var\(--bb-color-text\);/)
    expect(css).toMatch(/\.shopping-frequent button:hover \{[^}]*color:var\(--bb-color-text\);/)
    expect(css).toMatch(/\.shopping-frequent button:focus-visible \{[^}]*outline:var\(--bb-focus-ring\);/)
    expect(css).toMatch(/\.shopping-frequent button:active,\.shopping-frequent button\[aria-pressed=true\] \{[^}]*color:var\(--bb-color-primary-contrast\);/)
    expect(css).toMatch(/\.shopping-frequent button:disabled \{[^}]*color:var\(--bb-color-text-muted\);[^}]*opacity:1;/)
  })

  it('removes decorative audiobook route motion when reduced motion is requested', () => {
    expect(audiobookRoutes).toContain('@media (prefers-reduced-motion: reduce)')
    expect(audiobookRoutes).toMatch(/\.audiobook-route-view--forward,[\s\S]*\.audiobook-route-view--back[\s\S]*animation: none;/)
  })

  it('suppresses only pointer-origin route focus without disabling keyboard focus globally', () => {
    expect(audiobookRoutes).toMatch(/\[data-bb-route-focus="pointer"\]:focus\s*\{[^}]*box-shadow: none;/)
    expect(audiobookRoutes).not.toMatch(/:focus-visible\s*\{[^}]*box-shadow:\s*none/)
  })

  it('keeps the audiobook detail hero ratio-safe without an intrinsic title-width collapse', () => {
    expect(audiobooks).toMatch(/\.audiobook-detail-page>\.audiobook-detail-page__hero\{[^}]*display:grid;[^}]*grid-template-columns:minmax\(120px,180px\) minmax\(0,1fr\);[^}]*min-width:0/)
    expect(audiobooks).toMatch(/\.audiobook-detail-page__summary h1\{[^}]*word-break:normal;[^}]*overflow-wrap:break-word/)
    expect(audiobooks).toMatch(/\.audiobook-detail-page>\.audiobook-detail-page__hero>\.audiobook-detail-page__artwork,[^}]*grid-column:1;[^}]*max-inline-size:180px;[^}]*min-inline-size:0;[^}]*aspect-ratio:2\/3;[^}]*object-fit:cover/)
    expect(audiobooks).toContain('grid-template-columns:clamp(120px,40vw,180px) minmax(0,1fr)')
    expect(audiobooks).toContain('inline-size:clamp(120px,40vw,180px)')
    expect(audiobooks).toContain('width:min(140px,42vw)')
    expect(audiobooks).not.toContain('.audiobook-detail-page>div{display:contents}')
    expect(audiobooks).not.toContain('.audiobook-detail-page>div>*:not(.eyebrow):not(h1)')
    expect(audiobooks.match(/\.audiobook-detail-page\{/g) ?? []).toHaveLength(1)
  })

  it('keeps audiobook controls in-flow instead of as a global floating overlay', () => {
    expect(audiobookRoutes).toMatch(/\.audiobook-player\{display:grid;/)
    expect(audiobookRoutes).not.toMatch(/\.audiobook-player\{[^}]*position:fixed/)
    expect(appShell).not.toContain('<AudiobookPlayer')
  })

  it('gives the compact timer status its own non-overlapping grid row', () => {
    expect(audiobooks).toMatch(/\.audiobook-compact-book__timer-status\{[^}]*grid-column:1\/-1;[^}]*min-width:0;[^}]*overflow-wrap:anywhere/)
    expect(audiobooks).not.toMatch(/\.audiobook-compact-book__timer-status\{[^}]*position:absolute/)
    expect(audiobooks).not.toMatch(/\.audiobook-sleep-timer--compact \.audiobook-sleep-timer__status\{[^}]*position:absolute/)
    expect(audiobooks).toMatch(/\.audiobook-sleep-timer--compact \.audiobook-sleep-timer__panel\{[^}]*width:min\(280px,calc\(100vw - 48px\)\);[^}]*max-width:calc\(100vw - 48px\)/)
  })
})
