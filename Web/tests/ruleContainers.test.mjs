import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { test } from 'node:test'
import { splitRuleContainers } from '../src/ruleContainers.js'

test('the parcel demo has ten tabs and round-trips without changing its source', () => {
  const source = readFileSync(new URL('../../RuleParser/doc/Examples/Demo-Parcel-Dispatcher.txt', import.meta.url), 'utf8')
  const sections = splitRuleContainers(source)
  assert.equal(sections.length, 10)
  assert.equal(sections[0].name, 'Parcel.Handling')
  assert.equal(sections[9].name, 'Quote.Calculation')
  assert.equal(sections.map(section => section.text).join(''), source)
})

test('comments and strings do not create phantom containers', () => {
  const source = '// RuleContainer Fake\r\n/* RuleContainer AlsoFake */\r\nRuleContainer First\r\nRule A If X = "RuleContainer InString" Then Y\r\n  rulecontainer Second\r\nRule B If Y Then Z'
  const sections = splitRuleContainers(source)
  assert.deepEqual(sections.map(section => section.name), ['First', 'Second'])
  assert.equal(sections.map(section => section.text).join(''), source)
  assert.ok(sections[0].text.startsWith('// RuleContainer Fake'))
  assert.ok(sections[1].text.startsWith('  rulecontainer Second'))
})

test('editing one tab preserves other containers, including duplicate names', () => {
  const source = 'RuleContainer Same\nRule A If X Then Y\n\nRuleContainer Same\nRule B If Y Then Z'
  const sections = splitRuleContainers(source)
  const untouched = sections[1].text
  sections[0].text = sections[0].text.replace('Then Y', 'Then Changed')
  const updated = splitRuleContainers(sections.map(section => section.text).join(''))
  assert.equal(updated[1].text, untouched)
  assert.match(updated[0].text, /Then Changed/)
})

test('empty and incomplete files remain editable', () => {
  assert.deepEqual(splitRuleContainers(''), [])
  assert.deepEqual(splitRuleContainers('// Start here'), [])
  assert.deepEqual(splitRuleContainers('RuleContainer '), [{ name: 'Unnamed container', text: 'RuleContainer ' }])
})
