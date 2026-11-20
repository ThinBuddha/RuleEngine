import assert from 'node:assert/strict'
import { test } from 'node:test'
import { createServer } from 'vite'
import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { indexProof, proofValue } from '../src/proof.js'
import { evaluate } from '../src/runtime.js'

const node = (id, children = [], fields = {}) => ({ id, kind: 'Evidence', text: `Fact${id}`, state: 'Succeeded', value: null, source: null, evidenceId: null, children, ...fields })
const proof = { container: 'Demo', activeNodeId: 3, root: node(1, [node(2, [node(3, [], { state: 'WaitingForInput', text: '<script>unsafe</script>' })]), node(4, [], { evidenceId: 2, value: false })], { kind: 'Query' }) }

test('proof indexing provides the active path and supporting evidence without losing failed branches', () => {
  const index = indexProof(proof.root)
  assert.deepEqual(index.paths.get(3), [1, 2, 3])
  assert.equal(index.nodes.get(4).evidenceId, 2)
  assert.equal(index.nodes.size, 4)
  assert.equal(proofValue(false), 'false')
  assert.equal(proofValue(0), '0')
  assert.equal(proofValue('false'), '"false"')
  assert.equal(proofValue(null), '')
})

test('proof component renders active branches, evidence controls, typed values and escaped text', async () => {
  const server = await createServer({ server: { middlewareMode: true }, appType: 'custom' })
  try {
    const { default: component } = await server.ssrLoadModule('/src/ProofTree.vue')
    const html = await renderToString(createSSRApp(component, { proof }))
    assert.match(html, /current question/)
    assert.match(html, /Show supporting evidence #2/)
    assert.match(html, /&lt;script&gt;unsafe&lt;\/script&gt;/)
    assert.doesNotMatch(html, /<script>/)
    assert.match(html, /<code[^>]*>false<\/code>/)
    assert.equal((html.match(/ open/g) || []).length, 3)
  } finally { await server.close() }
})

test('runtime forwards Why without submitting it and retains result/error proofs', async (t) => {
  t.mock.method(globalThis, 'fetch', async () => ({ ok: false, json: async () => ({ error: 'Missing', missingFacts: ['Input'], proofs: [proof] }) }))
  await assert.rejects(evaluate({ source: '', mode: 'deduction', context: {}, query: 'Goal' }), error => error.proofs[0] === proof)
  const originalWindow = globalThis.window
  const originalSocket = globalThis.WebSocket
  globalThis.window = { location: { href: 'http://localhost/' } }
  let fail = false
  const sent = []
  class FakeSocket {
    constructor() { queueMicrotask(() => this.onopen()) }
    close() {}
    send(data) {
      const message = JSON.parse(data)
      sent.push(message)
      queueMicrotask(() => this.onmessage({ data: JSON.stringify(message.type === 'answer'
        ? fail ? { type: 'error', error: 'Missing', proofs: [proof], missingFacts: ['Input'] }
          : { type: 'result', result: { trace: [], proofs: [proof], context: {}, queryResult: true } }
        : { type: 'unknownValue', name: 'Input', choices: [], why: proof, inputType: { kind: 'boolean', values: [] } }) }))
    }
  }
  globalThis.WebSocket = FakeSocket
  try {
    let lifetime
    const answer = (name, choices, signal, why, inputType) => {
      assert.deepEqual(why, proof)
      assert.equal(inputType.kind, 'boolean')
      assert.equal(name, 'Input')
      assert.equal(signal.aborted, false)
      lifetime = signal
      return true
    }
    const request = { source: '', mode: 'deduction', context: {}, query: 'Goal' }
    const result = await evaluate(request, answer)
    assert.deepEqual(result.proofs, [proof])
    assert.deepEqual(sent[1], { type: 'answer', value: true })
    assert.equal(lifetime.aborted, true)
    fail = true
    await assert.rejects(evaluate(request, answer), error => {
      assert.deepEqual(error.proofs, [proof])
      assert.deepEqual(error.missingFacts, ['Input'])
      return true
    })
  } finally {
    globalThis.window = originalWindow
    globalThis.WebSocket = originalSocket
  }
})
