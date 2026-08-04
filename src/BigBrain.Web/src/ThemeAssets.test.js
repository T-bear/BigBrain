import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

const obsidianGold = readFileSync(
  resolve(process.cwd(), 'src/styles/themes/bigbrain-obsidian-gold.css'),
  'utf8',
)
const jellyfinObsidianGold = readFileSync(
  resolve(process.cwd(), '../../themes/jellyfin/bigbrain-obsidian-gold.css'),
  'utf8',
)

const requiredTokens = [
  '--bb-color-bg',
  '--bb-color-bg-subtle',
  '--bb-color-surface',
  '--bb-color-surface-raised',
  '--bb-color-surface-overlay',
  '--bb-color-border',
  '--bb-color-border-strong',
  '--bb-color-text',
  '--bb-color-text-muted',
  '--bb-color-text-inverse',
  '--bb-color-primary',
  '--bb-color-primary-hover',
  '--bb-color-primary-active',
  '--bb-color-primary-contrast',
  '--bb-color-accent',
  '--bb-color-success',
  '--bb-color-success-contrast',
  '--bb-color-warning',
  '--bb-color-warning-contrast',
  '--bb-color-danger',
  '--bb-color-danger-contrast',
  '--bb-color-info',
  '--bb-color-info-contrast',
  '--bb-color-backdrop',
  '--bb-shadow-sm',
  '--bb-shadow-md',
  '--bb-shadow-lg',
  '--bb-focus-ring',
]

describe('Obsidian Gold theme assets', () => {
  it('implements the complete token contract exactly once', () => {
    expect(obsidianGold).toContain('[data-theme="bigbrain-obsidian-gold"]')
    for (const token of requiredTokens) {
      expect(obsidianGold.match(new RegExp(`${token}:`, 'g'))).toHaveLength(1)
    }
    expect(obsidianGold).not.toMatch(/\{\s*\}/)
    expect(obsidianGold.split('{')).toHaveLength(obsidianGold.split('}').length)
    expect(obsidianGold).not.toContain('!important')
  })

  it('keeps the Jellyfin variant standalone and explicitly manual', () => {
    expect(jellyfinObsidianGold).toContain('--bb-jf-theme-id: bigbrain-obsidian-gold')
    expect(jellyfinObsidianGold).toContain('Manual installation only')
    expect(jellyfinObsidianGold).not.toMatch(/@import|url\s*\(/)
    expect(jellyfinObsidianGold).toContain('.skinHeader')
    expect(jellyfinObsidianGold).toContain('.mainDrawer')
    expect(jellyfinObsidianGold).toContain('.cardBox')
    expect(jellyfinObsidianGold).toContain('.cardImageContainer')
    expect(jellyfinObsidianGold).toContain('.dialog')
    expect(jellyfinObsidianGold).toContain('.emby-button')
    expect(jellyfinObsidianGold).toContain('.emby-input')
    expect(jellyfinObsidianGold).toContain('.emby-select')
    expect(jellyfinObsidianGold).toContain('.detailPagePrimaryContainer')
    expect(jellyfinObsidianGold.split('{')).toHaveLength(jellyfinObsidianGold.split('}').length)
  })
})
