import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { test } from 'node:test'
import { formatRuleSource } from '../src/ruleFormat.js'
import { splitRuleContainers } from '../src/ruleContainers.js'

test('auto format aligns conditions and conclusions with tabs', () => {
  const source = 'Enforce model "demo.txt" RuleContainer Demo Rule First Priority 1 If A = "sand and Then Rule" and B > 2 Then X := 1 and Y := 2 Rule Next If B Then Z'
  const formatted = formatRuleSource(source)
  assert.equal(formatted, [
    'Enforce model "demo.txt"',
    '',
    'RuleContainer Demo',
    '',
    '\tRule First',
    '\tPriority 1',
    '\tIf   A = "sand and Then Rule"',
    '\tand  B > 2',
    '\tThen X := 1',
    '\tand  Y := 2',
    '',
    '\tRule Next',
    '\tIf   B',
    '\tThen Z',
  ].join('\n'))
  assert.equal(formatRuleSource(formatted), formatted)
})

test('comments and quoted keywords remain intact', () => {
  const source = '// RuleContainer phantom\r\nRuleContainer Real Rule first If A /* and Then */ and B = "Rule and Then" Then X := 1 and Y := "and" // Rule fake\r\nRule second If B Then Z'
  const formatted = formatRuleSource(source)
  assert.match(formatted, /^\/\/ RuleContainer phantom\r\n\r\nRuleContainer Real/)
  assert.match(formatted, /\tIf   A \/\* and Then \*\/\r\n\tand  B = "Rule and Then"/)
  assert.match(formatted, /\tand  Y := "and" \/\/ Rule fake\r\n/)
  assert.deepEqual(splitRuleContainers(formatted).map(section => section.name), ['Real'])
  assert.equal(formatRuleSource(formatted), formatted)
})

test('formats a complete demo without losing containers or quoted values', () => {
  const source = readFileSync(new URL('../../RuleParser/doc/Examples/Demo-Parcel-Dispatcher.txt', import.meta.url), 'utf8')
  const formatted = formatRuleSource(source)
  assert.equal(splitRuleContainers(formatted).length, 10)
  assert.match(formatted, /\tIf   Delivery\.Distance\.km > 3\r?\n\tand  Delivery\.Distance\.km <= 15/)
  assert.match(formatted, /"ring the doorbell"/)
  assert.equal(formatRuleSource(formatted), formatted)
})

test('leaves incomplete files without a container unchanged', () => {
  assert.equal(formatRuleSource('Rule A If X and'), 'Rule A If X and')
  assert.equal(formatRuleSource('// empty for now'), '// empty for now')
})

test('keeps a comment before the next rule on its own line', () => {
  const source = 'RuleContainer Demo\nRule one If A Then B\n// Explain the next rule\nRule two If C Then D'
  const formatted = formatRuleSource(source)
  assert.match(formatted, /\tThen B\n\/\/ Explain the next rule\n\n\tRule two/)
  assert.equal(formatRuleSource(formatted), formatted)
})

test('keeps descriptions and expressions intact while separating clauses', () => {
  const source = 'RuleContainer Demo Rule decide Priority 2 Description "if and then" If not Flag and (Count + 2) > -4 Then Total := -(4 * 50) and Done'
  const formatted = formatRuleSource(source)
  assert.match(formatted, /\tDescription "if and then"\n\tIf   not Flag\n\tand  \(Count \+ 2\) > -4/)
  assert.match(formatted, /\tThen Total := -\(4 \* 50\)\n\tand  Done/)
  assert.equal(formatRuleSource(formatted), formatted)
})
