import assert from 'node:assert/strict'
import { test } from 'node:test'
import { derivedModelSource } from '../src/modelSource.js'
import { ref } from 'vue'
import { useDerivedModel } from '../src/derivedModel.js'

const property = (fullName, kind, stringValues = []) => ({
  name: fullName.split('.').at(-1), fullName, kind, stringValues,
})

test('derive action requires a successful current parse and disappears for enforced or cleared rules', () => {
  const source = ref('RuleContainer Demo Rule go If Input Then Goal')
  const state = useDerivedModel(source)
  assert.equal(state.canDeriveModel.value, false)
  assert.equal(state.createDerivedModelDraft(), null)
  const response = { containers: [{ name: 'Demo' }], model: {
    properties: { Goal: property('Goal', 'Boolean') }, classes: {},
  } }
  state.acceptDerivedModel(source.value, response)
  assert.equal(state.canDeriveModel.value, true)
  assert.match(state.createDerivedModelDraft().types, /type Goal of boolean/)
  source.value += '\nRule second If Input Then Other'
  assert.equal(state.canDeriveModel.value, false)
  assert.equal(state.createDerivedModelDraft(), null)
  state.acceptDerivedModel(source.value, response)
  assert.equal(state.canDeriveModel.value, true)
  state.clearDerivedModel()
  assert.equal(state.canDeriveModel.value, false)
  state.acceptDerivedModel(source.value, { containers: [{ name: 'Demo' }], model: null })
  assert.equal(state.canDeriveModel.value, false)
  state.acceptDerivedModel(source.value, { containers: [], model: response.model })
  assert.equal(state.canDeriveModel.value, false)
})

test('derived source preserves nested paths, scalar kinds, and string choices', () => {
  const model = { properties: {}, classes: {
    Parcel: { name: 'Parcel', fullName: 'Parcel', properties: {
      ready: property('Parcel.ready', 'Boolean'),
      weight: property('Parcel.weight', 'numeric'),
    }, classes: {
      Drone: { name: 'Drone', fullName: 'Parcel.Drone', classes: {}, properties: {
        status: property('Parcel.Drone.status', 'string', ['allowed', 'pending']),
      } },
    } },
  } }
  const { types, classes } = derivedModelSource(model)
  assert.match(types, /type Parcel.ready of boolean/)
  assert.match(types, /Parcel.weight: numeric; property uses real/)
  assert.match(types, /type Parcel.Drone.status of string with values = \["allowed", "pending"\]/)
  assert.match(classes, /class Parcel has\r\n  ready of Parcel.ready\r\n  weight of real\r\n  Drone as Parcel_Drone/)
  assert.match(classes, /class Parcel_Drone has\r\n  status of Parcel.Drone.status/)
})

test('unknown kinds and unqualified facts are retained as review notes', () => {
  const { types, classes } = derivedModelSource({
    properties: { Goal: property('Goal', 'Boolean') },
    classes: { Input: { name: 'Input', fullName: 'Input', classes: {}, properties: {
      unknown: property('Input.unknown', null),
    } } },
  })
  assert.match(types, /type Goal of boolean/)
  assert.match(types, /TODO: choose a type for Input.unknown/)
  assert.match(classes, /Unqualified fact Goal/)
  assert.match(classes, /Input.unknown needs a type/)
  assert.doesNotMatch(types, /of null|of undefined/)
  assert.doesNotMatch(classes, /^class Input has/m)
})

test('nested class names cannot collide with top-level classes', () => {
  const { classes } = derivedModelSource({ properties: {}, classes: {
    A: { name: 'A', fullName: 'A', properties: {}, classes: {
      B: { name: 'B', fullName: 'A.B', classes: {}, properties: { x: property('A.B.x', 'Boolean') } },
    } },
    A_B: { name: 'A_B', fullName: 'A_B', classes: {}, properties: { x: property('A_B.x', 'string') } },
  } })
  assert.match(classes, /B as A_B_2/)
  assert.match(classes, /class A_B_2 has/)
  assert.match(classes, /class A_B has/)
})

test('reserved path segments are preserved as review notes without invalid property declarations', () => {
  const { classes } = derivedModelSource({ properties: {}, classes: {
    Price: { name: 'Price', fullName: 'Price', properties: {}, classes: {
      of: { name: 'of', fullName: 'Price.of', classes: {}, properties: { gas: property('Price.of.gas', 'string') } },
    } },
  } })
  assert.match(classes, /Price.of uses a reserved model keyword/)
  assert.match(classes, /gas of Price.of.gas/)
  assert.doesNotMatch(classes, /\n  of as/)
})
