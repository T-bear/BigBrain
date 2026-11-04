import assert from 'node:assert/strict'
import { createHash } from 'node:crypto'
import { execFileSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

const root = resolve('src/BigBrain.Web/public/newspaper')
const image = readFileSync(resolve(root, 'Morgonbladet_designmaster_A4.png'))
const pdfPath = resolve(root, 'Morgonbladet_designmaster_A4.pdf')
const pdf = readFileSync(pdfPath)

assert.equal(
  createHash('sha256').update(image).digest('hex'),
  '9c3bb9676c337dc8bf6d97c93befd1fc18e03ef1200c5c089a119f45d9ad4733',
)

assert.equal(image.readUInt32BE(16), 2480)
assert.equal(image.readUInt32BE(20), 3508)
assert.ok(pdf.subarray(0, 8).toString().startsWith('%PDF-1.4'))
assert.ok(pdf.includes(Buffer.from('/Subtype /Image')))
assert.ok(pdf.includes(Buffer.from('/Width 2480 /Height 3508')))

let offset = 8
const parts = []

while (offset < image.length) {
  const size = image.readUInt32BE(offset)
  const type = image.toString('ascii', offset + 4, offset + 8)
  if (type === 'IDAT') {
    parts.push(image.subarray(offset + 8, offset + 8 + size))
  }
  offset += size + 12
  if (type === 'IEND') break
}

assert.ok(parts.length > 0)
assert.ok(pdf.includes(Buffer.concat(parts)), 'PDF måste bädda in originalbildens bilddata')

const info = execFileSync('pdfinfo', [pdfPath], { encoding: 'utf8' })
assert.match(info, /^Pages:\s+1\s*$/m)

const size = info.match(/Page size:\s+([\d.]+) x ([\d.]+)/)
assert.ok(size)
assert.ok(Math.abs(Number(size[1]) - 595.28) < 1)
assert.ok(Math.abs(Number(size[2]) - 841.89) < 1)

console.log('PASS: en A4-PDF med oförändrad originalbild')
