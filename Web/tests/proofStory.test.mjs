import assert from 'node:assert/strict'
import { test } from 'node:test'
import { createServer } from 'vite'
import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { proofStory } from '../src/proofStory.js'

const node = (id, kind, text, state, children = [], fields = {}) =>
  ({ id, kind, text, state, value: null, source: null, evidenceId: null, children, ...fields })

test('story follows supporting rules and distinguishes a failed alternative', () => {
  const attempt = node(2, 'Rule', 'Demo/first', 'Failed', [
    node(3, 'Condition', 'Status = "ready"', 'Failed'),
    node(4, 'Condition', 'Extra', 'Skipped'),
  ])
  const inner = node(7, 'Rule', 'Demo/inner', 'Succeeded', [
    node(8, 'Condition', 'Input > 10', 'Succeeded', [
      node(9, 'Evidence', 'Input', 'Succeeded', [], { value: 12, source: 'UserAnswer' }),
    ]),
    node(10, 'Conclusion', 'Intermediate', 'Succeeded', [], { value: true, source: 'RuleConclusion' }),
  ])
  const outer = node(5, 'Rule', 'Demo/outer', 'Succeeded', [
    node(6, 'Condition', 'Intermediate', 'Succeeded', [inner]),
    node(11, 'Conclusion', 'Goal', 'Succeeded', [], { value: true, source: 'RuleConclusion' }),
  ])
  const story = proofStory({ root: node(1, 'Query', 'Goal', 'Succeeded', [attempt, outer], { value: true }) })
  assert.match(story.join(' '), /You told us “Input” was 12/)
  assert.match(story.join(' '), /“Status = "ready"” did not hold/)
  assert.match(story.join(' '), /remaining conditions were not checked/)
  assert.ok(story.findIndex(line => line.includes('Demo/inner')) < story.findIndex(line => line.includes('Demo/outer')))
  assert.match(story.at(-1), /answer to “Goal” is yes/)
})

test('story keeps false evidence separate from an unresolved question', () => {
  const falseProof = { root: node(1, 'Query', 'Goal', 'Failed', [
    node(2, 'Evidence', 'Goal', 'Succeeded', [], { value: false, source: 'SuppliedFact' }),
  ], { value: false }) }
  const falseStory = proofStory(falseProof).join(' ')
  assert.match(falseStory, /already knew “Goal” was false/)
  assert.match(falseStory, /answer to “Goal” is no/)
  assert.doesNotMatch(falseStory, /cannot settle/)

  const unknownProof = { root: node(1, 'Query', 'Goal', 'Unknown', [
    node(2, 'Evidence', 'Input', 'Unknown', [], { source: 'UserAnswer' }),
  ]) }
  const unknownStory = proofStory(unknownProof).join(' ')
  assert.match(unknownStory, /still need a value for “Input”/)
  assert.match(unknownStory, /cannot settle “Goal”/)
  assert.doesNotMatch(unknownStory, /answer to “Goal” is no/)
})

test('story explains a deduction question while its answer is pending', () => {
  const waitingProof = { root: node(1, 'Query', 'Goal', 'WaitingForInput', [
    node(2, 'Rule', 'Demo/decide', 'WaitingForInput', [
      node(3, 'Condition', 'Input > 10', 'WaitingForInput', [
        node(4, 'Evidence', 'Input', 'WaitingForInput', [], { source: 'UserAnswer' }),
      ]),
    ]),
  ]) }
  const story = proofStory(waitingProof).join(' ')
  assert.match(story, /waiting for a value for/)
  assert.match(story, /Demo\/decide.*remains open/)
  assert.match(story, /cannot settle.*Goal/)
})

test('story component escapes user supplied proof text', async () => {
  const server = await createServer({ server: { middlewareMode: true }, appType: 'custom' })
  try {
    const { default: component } = await server.ssrLoadModule('/src/ProofStory.vue')
    const proof = { container: 'Demo', root: node(1, 'Query', '<script>unsafe</script>', 'Unknown') }
    const html = await renderToString(createSSRApp(component, { proof }))
    assert.match(html, /&lt;script&gt;unsafe&lt;\/script&gt;/)
    assert.doesNotMatch(html, /<script>/)
  } finally { await server.close() }
})
