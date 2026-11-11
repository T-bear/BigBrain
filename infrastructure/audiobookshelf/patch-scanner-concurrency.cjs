'use strict'
const fs = require('node:fs')
const crypto = require('node:crypto')
const path = '/app/server/scanner/AudioFileScanner.js'
const expected = '2d16ef07c3f1ee2b026980f36f9efd31fd297b6f21d74073591bea8b74c90901'
const current = fs.readFileSync(path)
const sha = crypto.createHash('sha256').update(current).digest('hex')
if (sha !== expected) throw Error('pinned ABS scanner source does not match')
const source = current.toString('utf8')
const pattern = 'const batchSize = 32'
if (source.split(pattern).length !== 2) throw Error('expected one scanner batch size site')
const newSource = source.replace(pattern,'const batchSize = 4')
fs.writeFileSync(path, newSource, {mode: 0o644})
fs.chmodSync(path,0o644)
console.log('EXACT_PIN_PATCH_PASS')
