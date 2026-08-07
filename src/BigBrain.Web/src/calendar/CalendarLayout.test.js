import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const css=readFileSync(resolve(process.cwd(),'src/styles/modules.css'),'utf8')
const rule=(selector)=>css.match(new RegExp(`${selector.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')}\\s*\\{([^}]+)\\}`))?.[1]??''

describe('calendar mobile layout contract',()=>{
  it('keeps month content and import history in normal flex/block flow',()=>{
    expect(rule('.calendar-dialog')).toContain('display:flex')
    expect(rule('.calendar-dialog')).toContain('flex-direction:column')
    expect(rule('.calendar-dialog__content')).toContain('flex-direction:column')
    expect(rule('.calendar-import-history')).not.toMatch(/position\s*:\s*(absolute|fixed|sticky)/)
    expect(css).not.toMatch(/\.calendar-import-history\s*\{[^}]*(?:margin-(?:top|bottom)|transform)\s*:\s*-/s)
  })

  it('wraps long history names and reserves mobile bottom-navigation space',()=>{
    expect(rule('.calendar-import-history li,.calendar-previews li')).toContain('overflow-wrap:anywhere')
    expect(css).toMatch(/@media\(max-width:700px\)[\s\S]*?\.calendar-dialog\{[^}]*padding:[^;}]*86px[^;}]*env\(safe-area-inset-bottom\)/)
    expect(css).toMatch(/\.calendar-month-list__day\{[^}]*min-width:0/)
  })
})
