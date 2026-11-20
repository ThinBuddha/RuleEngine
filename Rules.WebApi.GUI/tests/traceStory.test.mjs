import assert from 'node:assert/strict'
import { test } from 'node:test'
import { createServer } from 'vite'
import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { traceStory } from '../src/traceStory.js'

const fired = [
  { container: 'Build', rule: 'choose', conditions: ['Profile = "office"'], conclusions: ['Parts := "basic"'] },
  { container: 'Build', rule: 'assemble', conditions: ['Parts = "basic"'], conclusions: ['Status := "ready"', 'Done'] }
]

test('fire once story follows fired rules and names their outcomes', () => {
  const story = traceStory(fired, 'once')
  assert.match(story[0], /once in priority order/)
  assert.match(story[1], /First.*choose.*Profile.*Parts/)
  assert.match(story[1], /set “Parts” to "basic"/)
  assert.match(story[2], /Then.*assemble.*set “Status” to "ready" and established “Done”/)
  assert.match(story.at(-1), /completed this run/)
})

test('iteration story explains why rules can fire later and handles no fired rules', () => {
  const story = traceStory(fired, 'iteration')
  assert.match(story[0], /checked the rules repeatedly/)
  assert.match(story.at(-1), /no further rule fired/)
  assert.deepEqual(traceStory([], 'iteration').slice(1), ['No rule fired during this run.'])
})

test('trace story renders rule text as escaped text', async () => {
  const server = await createServer({ server: { middlewareMode: true, hmr: false }, appType: 'custom' })
  try {
    const { default: component } = await server.ssrLoadModule('/src/TraceStory.vue')
    const html = await renderToString(createSSRApp(component, {
      mode: 'once', trace: [{ container: 'Demo', rule: '<script>unsafe</script>', conditions: [], conclusions: [] }]
    }))
    assert.match(html, /&lt;script&gt;unsafe&lt;\/script&gt;/)
    assert.doesNotMatch(html, /<script>/)
  } finally { await server.close() }
})
