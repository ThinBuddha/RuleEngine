import assert from 'node:assert/strict'
import { test } from 'node:test'
import { createServer } from 'vite'
import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { parseRules } from '../src/runtime.js'

const model = {
  name: '', fullName: '', properties: {}, classes: {
    Parcel: { name: 'Parcel', fullName: 'Parcel', properties: {}, classes: {
      Drone: { name: 'Drone', fullName: 'Parcel.Drone', classes: {}, properties: {
        status: { name: 'status', fullName: 'Parcel.Drone.status', kind: 'string', stringValues: ['allowed', '<script>'] },
      } },
    } },
  },
}

test('parse request returns the derived model with the container summary', async (t) => {
  const response = { containers: [{ name: 'Demo', rules: [] }], model }
  t.mock.method(globalThis, 'fetch', async (url, options) => {
    assert.equal(url, '/api/rules/parse')
    assert.deepEqual(JSON.parse(options.body), { source: 'RuleContainer Demo' })
    return { ok: true, json: async () => response }
  })
  assert.deepEqual(await parseRules('RuleContainer Demo'), response)
})

test('derived model tree renders nested classes, kinds and escaped string choices', async () => {
  const server = await createServer({ server: { middlewareMode: true, hmr: false }, appType: 'custom' })
  try {
    const { default: component } = await server.ssrLoadModule('/src/ModelTree.vue')
    const html = await renderToString(createSSRApp(component, { model }))
    assert.match(html, /2 classes · 1 property/)
    assert.match(html, /Parcel/)
    assert.match(html, /Drone/)
    assert.match(html, /status/)
    assert.match(html, /string/)
    assert.match(html, /allowed/)
    assert.match(html, /&lt;script&gt;/)
    assert.doesNotMatch(html, /<script>/)
  } finally { await server.close() }
})
