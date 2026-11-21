import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { test } from 'node:test'
import { createServer } from 'vite'
import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { splitModelSource } from '../src/modelSource.js'

for (const stem of ['Parcel-Dispatcher', 'Computer-Assembly', 'Moonbase-Clinic']) {
  test(`${stem} splits into types and classes without losing text`, () => {
    const folder = { 'Parcel-Dispatcher': 'Parcel dispatcher', 'Computer-Assembly': 'Computer assembly', 'Moonbase-Clinic': 'Moonbase clinic' }[stem]
    const source = readFileSync(new URL(`../../RuleParser/doc/Examples/${folder}/${stem}-model.txt`, import.meta.url), 'utf8')
    const { types, classes } = splitModelSource(source)

    assert.match(types, /^unit /)
    assert.match(classes, /^class /)
    assert.equal(types + classes, source)
  })
}

test('ignores class inside comments, strings, and longer identifiers', () => {
  const source = 'unit Classifier of real with short = "class"\r\n' +
    '// class Fake\r\n/*\r\nclass Hidden\r\n*/\r\n' +
    'type Label of string with values = ["class Example"]\r\n' +
    'class Real has\r\n  Label of Label\r\n'
  const { types, classes } = splitModelSource(source)

  assert.match(types, /type Label/)
  assert.equal(classes, 'class Real has\r\n  Label of Label\r\n')
  assert.equal(types + classes, source)
})

test('supports files containing only types or only classes', () => {
  assert.deepEqual(splitModelSource('type Name of string'), { types: 'type Name of string', classes: '' })
  assert.deepEqual(splitModelSource('class Person has\n  Name of string'), {
    types: '', classes: 'class Person has\n  Name of string'
  })
})

test('model workspace renders imported content in the selected tab', async () => {
  const server = await createServer({ server: { middlewareMode: true, hmr: false }, appType: 'custom' })
  try {
    const { default: workspace } = await server.ssrLoadModule('/src/ModelWorkspace.vue')
    const props = {
      typesSource: 'unit Credits of integer with short = "cr"',
      classesSource: 'class Order has\n  Total of Credits',
      fileName: 'demo-model.txt'
    }
    const types = await renderToString(createSSRApp(workspace, { ...props, tab: 'types' }))
    const classes = await renderToString(createSSRApp(workspace, { ...props, tab: 'classes' }))

    assert.match(types, /unit Credits of integer/)
    assert.doesNotMatch(types, /class Order has/)
    assert.match(classes, /class Order has/)
    assert.doesNotMatch(classes, /unit Credits of integer/)
    assert.match(classes, /demo-model\.txt/)
  } finally { await server.close() }
})
