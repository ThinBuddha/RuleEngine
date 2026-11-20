import assert from 'node:assert/strict'
import { test } from 'node:test'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const assembly = fileURLToPath(new URL('../../CommandShell/bin/Debug/net10.0/CommandShell.dll', import.meta.url))
const fixture = fileURLToPath(new URL('./fixtures/proof.rules', import.meta.url))
const multiFixture = fileURLToPath(new URL('./fixtures/proof-multi.rules', import.meta.url))
const parcelDemo = fileURLToPath(new URL('../../RuleParser/doc/Examples/Parcel-Dispatcher.txt', import.meta.url))

function shell(answer, ruleFile = fixture) {
  const result = spawnSync('dotnet', [assembly], {
    input: `lr ${ruleFile}\n2\ner\nGoal\n/why\n${answer}\nhow\ncc\nhow\nquit\n`,
    encoding: 'utf8', timeout: 10000, windowsHide: true,
  })
  assert.equal(result.status, 0, result.stderr)
  return result.stdout
}

test('shell /why explains without consuming the answer and how prints the completed proof', () => {
  const output = shell('true')
  assert.match(output, /WaitingForInput.*current question/)
  assert.match(output, /Rule: ExplanationDemo\/outer/)
  assert.match(output, /Rule: ExplanationDemo\/inner/)
  assert.match(output, /Explanation: ExplanationDemo \(Succeeded\)/)
  assert.match(output, /evidence: #\d+/)
  assert.equal((output.match(/Input \(blank to skip/g) || []).length, 2)
  assert.match(output, /context was cleared[\s\S]*No deduction proof is available/)
})

test('shell blank answers remain unknown in the proof', () => {
  const output = shell('')
  assert.match(output, /Explanation: ExplanationDemo \(Unknown\)/)
  assert.doesNotMatch(output, /Explanation: ExplanationDemo \(Succeeded\)/)
})

test('shell deduction solves a query across two containers', () => {
  const output = shell('true', multiFixture)
  assert.doesNotMatch(output, /Cannot apply a query to more than? one rule container/)
  assert.match(output, /Explanation: All containers \(Succeeded\)/)
  assert.match(output, /Rule: First\/outer/)
  assert.match(output, /Rule: Second\/inner/)
  assert.match(output, /answer to your query is: True/)
})

test('shell loads and validates a companion model beside the rule file', () => {
  const result = spawnSync('dotnet', [assembly], {
    input: `lr ${parcelDemo}\n0\nquit\n`, encoding: 'utf8', timeout: 10000, windowsHide: true,
  })
  assert.equal(result.status, 0, result.stderr)
  assert.match(result.stdout, /Successfully loaded file/)
  assert.doesNotMatch(result.stdout, /model validation errors|Problem while reading file/)
})
