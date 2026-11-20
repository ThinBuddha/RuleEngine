import assert from 'node:assert/strict'
import { test } from 'node:test'
import { inputHint, parsePromptValue } from '../src/promptValue.js'

test('derived prompts retain inferred choices and free text conversion', () => {
  assert.deepEqual(parsePromptValue('123', ['123']), { value: '123' })
  assert.deepEqual(parsePromptValue('123'), { value: 123 })
  assert.deepEqual(parsePromptValue('false'), { value: false })
  assert.deepEqual(parsePromptValue(' '), { value: null })
})

test('explicit strings stay strings, including numeric-looking and empty choices', () => {
  const type = { kind: 'string', values: ['', '123', 'false'] }
  assert.deepEqual(parsePromptValue('123', type.values, type), { value: '123' })
  assert.deepEqual(parsePromptValue('false', type.values, type), { value: 'false' })
  assert.deepEqual(parsePromptValue('', type.values, type), { value: '' })
  assert.match(parsePromptValue('other', type.values, type).error, /permitted/)
  assert.deepEqual(parsePromptValue('123', [], { kind: 'string', values: [] }), { value: '123' })
})

test('explicit boolean and bounded numeric prompts convert and reject invalid input', () => {
  assert.deepEqual(parsePromptValue('false', [], { kind: 'boolean' }), { value: false })
  assert.match(parsePromptValue('1', [], { kind: 'boolean' }).error, /true or false/)
  const type = { kind: 'integer', minimum: { value: 0, inclusive: false }, maximum: { value: 5, inclusive: false }, unit: 'points' }
  assert.deepEqual(parsePromptValue('3', [], type), { value: 3 })
  assert.match(parsePromptValue('0', [], type).error, /greater than/)
  assert.match(parsePromptValue('5', [], type).error, /less than/)
  assert.match(parsePromptValue('2.5', [], type).error, /whole number/)
  assert.equal(inputHint(type), 'Whole number · > 0 and < 5 · points')
})
