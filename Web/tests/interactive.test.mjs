import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { once } from 'node:events'
import { before, after, test } from 'node:test'
import { fileURLToPath } from 'node:url'
import { readFileSync } from 'node:fs'
import { formatRuleSource } from '../src/ruleFormat.js'

let server
let baseUrl
before(async () => {
  const assembly = process.env.RULES_TEST_ASSEMBLY || fileURLToPath(new URL('../../Rules.WebApi/bin/Debug/net10.0/Rules.WebApi.dll', import.meta.url))
  server = spawn('dotnet', [assembly, '--urls', 'http://127.0.0.1:0'], { windowsHide: true })
  baseUrl = await new Promise((resolve, reject) => {
    let output = ''
    const timeout = setTimeout(() => reject(new Error(`API startup timed out: ${output}`)), 20000)
    server.on('error', reject)
    server.on('exit', (code) => { clearTimeout(timeout); reject(new Error(`API exited (${code}): ${output}`)) })
    server.stderr.on('data', (data) => { output += data })
    server.stdout.on('data', (data) => {
      output += data
      const match = output.match(/Now listening on: (http:\/\/127\.0\.0\.1:\d+)/)
      if (match) { clearTimeout(timeout); resolve(match[1]) }
    })
  })
})
after(async () => {
  if (server && server.exitCode === null) {
    const exited = once(server, 'exit')
    server.kill()
    await exited
  }
})

function run(operation, request, answer) {
  return new Promise((resolve, reject) => {
    const socket = new WebSocket(`${baseUrl.replace('http:', 'ws:')}/api/rules/interactive/${operation}`)
    const timeout = setTimeout(() => { socket.close(); reject(new Error('Evaluation timed out')) }, 10000)
    const prompts = []
    socket.onopen = () => socket.send(JSON.stringify(request))
    socket.onerror = reject
    socket.onclose = () => { clearTimeout(timeout); reject(new Error('Unexpected disconnect')) }
    socket.onmessage = async ({ data }) => {
      try {
        const message = JSON.parse(data)
        if (message.type === 'unknownValue') {
          prompts.push(message.name)
          const value = await answer(message.name, socket, message.choices, message.why, message.inputType)
          if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify({ type: 'answer', value }))
        } else {
          clearTimeout(timeout)
          resolve({ ...message, prompts })
          socket.close()
        }
      } catch (error) { clearTimeout(timeout); socket.close(); reject(error) }
    }
  })
}

test('calculation resumes through multiple prompts and caches each answer', async () => {
  const response = await run('calculate', { expression: 'A + B + A', context: {} }, (name) => name === 'A' ? 2 : 3)
  assert.equal(response.type, 'result')
  assert.equal(response.result.value, 7)
  assert.deepEqual(response.prompts, ['A', 'B'])
  assert.deepEqual(response.result.context, { A: 2, B: 3 })
})

for (const value of [false, 0, 2.5, 'hello']) {
  test(`answers preserve the type and value of ${JSON.stringify(value)}`, async () => {
    const response = await run('calculate', { expression: 'A', context: {} }, () => value)
    assert.equal(response.type, 'result')
    assert.equal(response.result.value, value)
  })
}

for (const value of [null, '', '   ']) {
  test(`unanswered fact ${JSON.stringify(value)} returns an error without another prompt`, async () => {
    const response = await run('calculate', { expression: 'A + 1', context: {} }, () => value)
    assert.equal(response.type, 'error')
    assert.deepEqual(response.missingFacts, ['A'])
    assert.deepEqual(response.prompts, ['A'])
  })
}

for (const mode of ['once', 'iteration', 'deduction']) {
  test(`${mode} obtains unknown input during rule execution`, async () => {
    const response = await run('evaluate', {
      source: `RuleContainer Test Rule decide If Input > 2 Then Output := 7${mode === 'iteration' ? ' and Input := 0' : ''}`,
      // Use a known left operand: the existing deduction engine declines an unknown root ID.
      mode, query: 'Expected > Output', context: { Expected: 10 },
    }, () => 3)
    assert.equal(response.type, 'result', JSON.stringify(response))
    assert.deepEqual(response.prompts, ['Input'])
    assert.equal(response.result.context.Output, 7)
    assert.equal(response.result.trace.length, 1)
    assert.deepEqual(response.result.trace[0].conclusions,
      mode === 'iteration' ? ['Output := 7', 'Input := 0'] : ['Output := 7'])
    if (mode === 'deduction') assert.equal(response.result.queryResult, true)
  })
}

test('another evaluation can complete while the first is waiting for input', async () => {
  const first = await run('calculate', { expression: 'A + A', context: {} }, async () => {
    const second = await run('calculate', { expression: 'A + A', context: {} }, () => 9)
    assert.equal(second.result.value, 18)
    return 2
  })
  assert.equal(first.result.value, 4)
})

test('parallel sessions isolate rules, choices, context, traces and declined answers', async () => {
  const count = 9
  let waiting = 0
  let release
  const allPrompting = new Promise(resolve => { release = resolve })
  const responses = await Promise.all(Array.from({ length: count }, (_, index) => {
    const value = `session-${index}`
    return run('evaluate', {
      source: `RuleContainer Shared Rule choose If Input = "${value}" Then Goal and Output := "${value}"`,
      mode: ['once', 'iteration', 'deduction'][index % 3], query: 'Goal', context: { Owner: value },
    }, async (name, _, choices) => {
      assert.equal(name, 'Input')
      assert.deepEqual(choices, [value])
      if (++waiting === count) release()
      await allPrompting
      return index % 4 === 0 ? null : value
    })
  }))
  for (const [index, response] of responses.entries()) {
    if (index % 4 === 0) {
      assert.equal(response.type, 'error')
      assert.deepEqual(response.missingFacts, ['Input'])
    } else {
      assert.equal(response.type, 'result', JSON.stringify(response))
      assert.deepEqual(response.result.context, {
        Owner: `session-${index}`, Input: `session-${index}`, Goal: true, Output: `session-${index}`,
      })
      assert.equal(response.result.trace.length, 1)
      assert.deepEqual(response.result.trace[0].conditions, [`Input = "session-${index}"`])
    }
  }
})

test('disconnecting at a prompt releases the evaluation', async () => {
  await assert.rejects(run('calculate', { expression: 'A', context: {} }, (_, socket) => socket.close()), /disconnect/)
  const response = await run('calculate', { expression: 'A', context: {} }, () => 7)
  assert.equal(response.result.value, 7)
})

test('HTTP clients retain noninteractive missing-fact responses', async () => {
  const response = await fetch(`${baseUrl}/api/rules/calculate`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ expression: 'A', context: {} }),
  })
  assert.equal(response.status, 400)
  assert.deepEqual((await response.json()).missingFacts, ['A'])
})

async function parse(source, modelSource, modelFileName) {
  const response = await fetch(`${baseUrl}/api/rules/parse`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ source, modelSource, modelFileName }),
  })
  return { status: response.status, body: await response.json() }
}

test('enforced model rejects absent files, unknown IDs and out-of-domain values', async () => {
  const modelSource = 'unit Points of integer with short = "pt" type Score of Points with min = 0 and max < 10 class Sample has Score of Score'
  const header = 'enforce model "sample-model.txt" RuleContainer C Rule R If Sample.Score = 2 Then Sample.Score := 3'
  assert.match((await parse(header)).body.error, /must be supplied/)
  assert.match((await parse(header, modelSource, 'other.txt')).body.error, /must be supplied/)
  assert.equal((await parse(header, modelSource, 'sample-model.txt')).status, 200)
  const invalid = await parse(header.replace('Sample.Score = 2', 'Sample.Unknown = 2').replace('Score := 3', 'Score := 10'), modelSource, 'sample-model.txt')
  assert.equal(invalid.status, 400)
  assert.ok(invalid.body.syntaxErrors.some(error => error.includes('Sample.Unknown')))
  assert.ok(invalid.body.syntaxErrors.some(error => error.includes("outside type 'Score'")))
  assert.equal((await parse('RuleContainer C', undefined, undefined)).status, 200)
})

for (const demo of ['Parcel-Dispatcher', 'Computer-Assembly', 'Moonbase-Clinic']) {
  test(`auto formatted ${demo} has the same parsed rules and model choices`, async () => {
    const source = readFileSync(new URL(`../../RuleParser/doc/Examples/${demo}.txt`, import.meta.url), 'utf8')
    const modelSource = readFileSync(new URL(`../../RuleParser/doc/Examples/${demo}-model.txt`, import.meta.url), 'utf8')
    const original = await parse(source, modelSource, `${demo}-model.txt`)
    const formatted = await parse(formatRuleSource(source), modelSource, `${demo}-model.txt`)
    assert.equal(original.status, 200, JSON.stringify(original.body))
    assert.ok(original.body.model)
    assert.equal(formatted.status, 200, JSON.stringify(formatted.body))
    assert.deepEqual(formatted.body, original.body)
  })
}

test('enforced model supplies nested property choices, bounds and boolean kind to prompts', async () => {
  const modelSource = 'unit Points of integer with short = "pt" type Phase of string with values = ["queued", "ready", "unused"] type Duration of Points with min > 0 and max < 5 class Child has Phase of Phase Duration of Duration Enabled of boolean class Root has Child as Child'
  const source = 'enforce model "prompt-model.txt" RuleContainer C Rule R If Root.Child.Phase = "ready" and Root.Child.Duration > 1 and Root.Child.Enabled Then Root.Child.Enabled'
  const requests = []
  const response = await run('evaluate', { source, modelSource, modelFileName: 'prompt-model.txt', mode: 'once', context: {} },
    (name, _, choices, __, inputType) => {
      requests.push({ name, choices, inputType })
      return { 'Root.Child.Phase': 'ready', 'Root.Child.Duration': 3, 'Root.Child.Enabled': true }[name]
    })
  assert.equal(response.type, 'result', JSON.stringify(response))
  assert.deepEqual(requests.map(request => request.name), ['Root.Child.Phase', 'Root.Child.Duration', 'Root.Child.Enabled'])
  assert.deepEqual(requests[0].choices, ['queued', 'ready', 'unused'])
  assert.equal(requests[0].inputType.kind, 'string')
  assert.equal(requests[1].inputType.kind, 'integer')
  assert.equal(requests[1].inputType.unit, 'pt')
  assert.deepEqual(requests[1].inputType.minimum, { value: 0, inclusive: false, isInteger: true })
  assert.deepEqual(requests[1].inputType.maximum, { value: 5, inclusive: false, isInteger: true })
  assert.equal(requests[2].inputType.kind, 'boolean')
  const invalid = await run('evaluate', { source, modelSource, modelFileName: 'prompt-model.txt', mode: 'once', context: {} },
    name => name === 'Root.Child.Phase' ? 'ready' : 5)
  assert.equal(invalid.type, 'error')
  assert.match(invalid.error, /outside its model type/)
})

test('an explicitly declared empty string can be answered as a value', async () => {
  const modelSource = 'type Phase of string with values = ["", "ready"] class Root has Phase of Phase Done of boolean'
  const source = 'enforce model "empty-model.txt" RuleContainer C Rule R If Root.Phase = "ready" Then Root.Done'
  const response = await run('evaluate', { source, modelSource, modelFileName: 'empty-model.txt', mode: 'once', context: {} },
    (_, __, choices) => { assert.deepEqual(choices, ['', 'ready']); return '' })
  assert.equal(response.type, 'result', JSON.stringify(response))
  assert.equal(response.result.context['Root.Phase'], '')
  assert.equal(response.result.context['Root.Done'], undefined)
})

test('enforced calculator does not prompt for an unmapped ID', async () => {
  const modelSource = 'class Root has Known of integer'
  const source = 'enforce model "only-known.txt" RuleContainer C Rule R If Root.Known = 1 Then Root.Known := 2'
  const response = await run('calculate', { source, modelSource, modelFileName: 'only-known.txt', expression: 'Unknown', context: {} },
    () => assert.fail('An unknown ID must not open a prompt'))
  assert.equal(response.type, 'error')
  assert.match(response.error, /does not map/)
})

function proofNodes(node) {
  return [node, ...node.children.flatMap(proofNodes)]
}

function checkProof(proof, active = false) {
  const nodes = proofNodes(proof.root)
  assert.equal(new Set(nodes.map(node => node.id)).size, nodes.length)
  for (const node of nodes) {
    assert.equal(typeof node.kind, 'string')
    assert.equal(typeof node.state, 'string')
    if (node.evidenceId !== null) assert.ok(nodes.some(target => target.id === node.evidenceId))
  }
  if (active) assert.equal(nodes.find(node => node.id === proof.activeNodeId).state, 'WaitingForInput')
  else assert.equal(proof.activeNodeId, null)
  return nodes
}

const proofRules = 'RuleContainer Demo Rule outer If Intermediate and Extra Then Goal Rule inner If Input > 10 Then Intermediate'

test('deduction finds a goal in a later container without asking for the goal', async () => {
  const response = await run('evaluate', {
    source: 'RuleContainer First Rule unrelated If Extra Then Other RuleContainer Second Rule goal If Input Then Goal',
    mode: 'deduction', query: 'Goal', context: {},
  }, (name, _, __, why) => {
    assert.equal(name, 'Input')
    assert.equal(why.container, 'All containers')
    assert.deepEqual(proofNodes(why.root).filter(node => node.kind === 'Rule').map(node => node.text), ['Second/goal'])
    return true
  })
  assert.equal(response.type, 'result', JSON.stringify(response))
  assert.equal(response.result.queryResult, true)
  assert.deepEqual(response.prompts, ['Input'])
  assert.deepEqual(response.result.trace.map(item => item.container), ['Second'])
  assert.equal(response.result.proofs.length, 1)
  assert.equal(response.result.proofs[0].container, 'All containers')
})

test('deduction chains across containers and retains duplicate rule names with distinct origins', async () => {
  const source = 'RuleContainer First Rule decide If Intermediate Then Goal ' +
    'RuleContainer Second Rule decide If Input Then Intermediate'
  const response = await run('evaluate', { source, mode: 'deduction', query: 'Goal', context: { Input: true } },
    () => assert.fail('The input is already known'))
  assert.equal(response.type, 'result', JSON.stringify(response))
  assert.equal(response.result.queryResult, true)
  assert.deepEqual(response.result.trace.map(item => [item.container, item.rule]), [
    ['Second', 'decide'], ['First', 'decide'],
  ])
  assert.deepEqual(proofNodes(response.result.proofs[0].root)
    .filter(node => node.kind === 'Rule').map(node => node.text), ['First/decide', 'Second/decide'])
})

test('deduction prompts carry stable Why snapshots and result carries the completed proof', async () => {
  const snapshots = []
  const response = await run('evaluate', { source: proofRules, mode: 'deduction', query: 'Goal', context: {} },
    (name, _, choices, why) => {
      assert.equal(why.container, 'Demo')
      const nodes = checkProof(why, true)
      assert.equal(nodes.find(node => node.id === why.activeNodeId).text, name)
      snapshots.push(JSON.stringify(why))
      if (name === 'Input') assert.deepEqual(nodes.filter(node => node.kind === 'Rule').map(node => node.text), ['Demo/outer', 'Demo/inner'])
      else assert.ok(nodes.some(node => node.source === 'UserAnswer' && node.value === 12))
      return name === 'Input' ? 12 : true
    })
  assert.equal(response.type, 'result')
  assert.equal(response.result.proofs.length, 1)
  const nodes = checkProof(response.result.proofs[0])
  assert.equal(response.result.proofs[0].root.state, 'Succeeded')
  assert.ok(nodes.some(node => node.source === 'UserAnswer' && node.value === true))
  assert.equal(response.result.trace.length, 2)
  assert.equal(snapshots.length, 2)
  for (const saved of snapshots) checkProof(JSON.parse(saved), true)
})

for (const decline of [null, '']) {
  test(`declined deduction answer ${JSON.stringify(decline)} returns an unknown proof`, async () => {
    const response = await run('evaluate', { source: proofRules, mode: 'deduction', query: 'Goal', context: {} }, () => decline)
    assert.equal(response.type, 'error')
    assert.deepEqual(response.missingFacts, ['Input'])
    assert.equal(response.proofs[0].root.state, 'Unknown')
    assert.ok(checkProof(response.proofs[0]).some(node => node.source === 'UserAnswer' && node.state === 'Unknown'))
  })
}

test('HTTP deduction returns one proof even when no rule fires and preserves Boolean false', async () => {
  const request = { source: proofRules + ' RuleContainer Second Rule spare If Other Then Goal', mode: 'deduction', query: 'Goal', context: { Goal: false } }
  const response = await fetch(`${baseUrl}/api/rules/evaluate`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(request),
  })
  assert.equal(response.status, 200)
  const result = await response.json()
  assert.deepEqual(result.proofs.map(proof => proof.container), ['All containers'])
  assert.deepEqual(result.trace, [])
  assert.equal(result.rounds, 0)
  for (const proof of result.proofs) {
    assert.equal(proof.root.state, 'Failed')
    assert.equal(proof.root.value, false)
    checkProof(proof)
  }
  const interactive = await run('evaluate', request, () => assert.fail('Known goal must not prompt'))
  assert.deepEqual(interactive.result, result)
})

test('HTTP missing facts retain a finalized partial deduction proof', async () => {
  const response = await fetch(`${baseUrl}/api/rules/evaluate`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ source: proofRules, mode: 'deduction', query: 'Goal', context: {} }),
  })
  assert.equal(response.status, 400)
  const error = await response.json()
  assert.deepEqual(error.missingFacts, ['Input'])
  assert.equal(error.proofs[0].root.state, 'Unknown')
  checkProof(error.proofs[0])
})

for (const mode of ['once', 'iteration', 'calculate']) {
  test(`${mode} prompts do not expose a deduction snapshot`, async () => {
    const response = await run(mode === 'calculate' ? mode : 'evaluate', mode === 'calculate'
      ? { expression: 'Input', context: {} }
      : { source: 'RuleContainer Demo Rule test If Input Then Goal', mode, context: {} },
    (_, __, ___, why) => { assert.equal(why, null); return true })
    assert.equal(response.type, 'result')
    if (mode !== 'calculate') assert.deepEqual(response.result.proofs, [])
  })
}

test('parallel deduction snapshots never contain another sessions answers', async () => {
  let waiting = 0
  let release
  const barrier = new Promise(resolve => { release = resolve })
  const responses = await Promise.all(['alpha', 'beta', 'gamma'].map(value => run('evaluate', {
    source: `RuleContainer Demo Rule choose If Input = "${value}" Then Goal`, mode: 'deduction', query: 'Goal', context: {},
  }, async (_, __, ___, why) => {
    assert.equal(checkProof(why, true).find(node => node.kind === 'Condition').text, `Input = "${value}"`)
    if (++waiting === 3) release()
    await barrier
    return value
  })))
  for (const [index, response] of responses.entries()) {
    const answers = checkProof(response.result.proofs[0]).filter(node => node.source === 'UserAnswer')
    assert.deepEqual(answers.map(node => node.value), [['alpha'], ['beta'], ['gamma']][index])
  }
})

test('deep deduction snapshots serialize beyond the default JSON depth', async () => {
  const source = 'RuleContainer Deep ' + Array.from({ length: 12 }, (_, i) =>
    `Rule step${i} If Fact${i + 1} Then Fact${i}`).join(' ')
  const response = await run('evaluate', { source, mode: 'deduction', query: 'Fact0', context: {} },
    (_, __, ___, why) => { checkProof(why, true); return true })
  assert.equal(response.type, 'result')
  assert.equal(checkProof(response.result.proofs[0]).filter(node => node.kind === 'Rule').length, 12)
})

const stringRules = `RuleContainer Travel
Rule short If Distance.to.travel = "small" Then Transport := "walk"
Rule long If (Distance.to.travel) = "big" Then Transport := "train"
Rule repeat If Distance.to.travel <> "small" Then Transport := "train"
Rule numeric If Age > 18 Then Adult
RuleContainer More
Rule extra If Enabled Then Distance.to.travel := "medium"`

test('model types collect distinct strings from comparisons and assignments across containers', async () => {
  const response = await parse(stringRules)
  assert.equal(response.status, 200, JSON.stringify(response.body))
  assert.deepEqual(response.body.modelTypes, {
    'Distance.to.travel': ['small', 'big', 'medium'], Transport: ['walk', 'train'],
  })
})

test('interactive prompts carry only the choices belonging to the requested ID', async () => {
  const response = await run('evaluate', { source: stringRules, mode: 'once', context: { Age: 20, Enabled: false } }, (name, _, choices) => {
    assert.equal(name, 'Distance.to.travel')
    assert.deepEqual(choices, ['small', 'big', 'medium'])
    return 'big'
  })
  assert.equal(response.type, 'result', JSON.stringify(response))
  assert.equal(response.result.context.Transport, 'train')
})

test('calculator uses the supplied rule model and preserves numeric-looking string choices', async () => {
  const response = await run('calculate', {
    expression: 'Code', context: {},
    source: 'RuleContainer Test Rule first If Code = "123" Then Done Rule second If Code = "true" Then Done',
  }, (_, __, choices) => {
    assert.deepEqual(choices, ['123', 'true'])
    return choices[0]
  })
  assert.equal(response.result.value, '123')
})

for (const conflicting of ['Value := 42', 'Value', 'Value := 1 + 2']) {
  test(`mixed string and other types are syntax errors: ${conflicting}`, async () => {
    const source = `RuleContainer Test Rule text If Flag Then Value := "small" Rule conflict If Flag Then ${conflicting}`
    const response = await parse(source)
    assert.equal(response.status, 400)
    assert.match(response.body.error, /Conflicting types.*Value/)
    const evaluation = await run('evaluate', { source, mode: 'once', context: {} }, () => assert.fail('Must reject before prompting'))
    assert.equal(evaluation.type, 'error')
    assert.ok(evaluation.syntaxErrors.length)
  })
}

test('aliases share later string literals without mixing unrelated IDs', async () => {
  const response = await parse('RuleContainer Test Rule alias If Flag Then Copy := Original Rule text If Original = "small" Then Other := "big"')
  assert.equal(response.status, 200)
  assert.deepEqual(response.body.modelTypes, { Copy: ['small'], Original: ['small'], Other: ['big'] })
})

test('computed string fragments do not become dropdown choices', async () => {
  const response = await parse('RuleContainer Test Rule computed If Name + "suffix" = "full" Then Done')
  assert.equal(response.status, 200)
  assert.equal(response.body.modelTypes.Name, undefined)
})

test('computed expressions do not merge the types of independent operands', async () => {
  const response = await parse('RuleContainer Test Rule types If Flag Then Prefix := "item" and Count := 2 Rule computed If Flag Then Label := Prefix + Count')
  assert.equal(response.status, 200, JSON.stringify(response.body))
  assert.deepEqual(response.body.modelTypes.Prefix, ['item'])
  assert.equal(response.body.modelTypes.Count, undefined)
  assert.deepEqual(response.body.modelTypes.Label, [])
})

test('different loaded models do not retain each other’s choices', async () => {
  await parse(stringRules)
  const response = await run('calculate', { expression: 'Distance.to.travel', context: {} }, (_, __, choices) => {
    assert.deepEqual(choices, [])
    return 'custom'
  })
  assert.equal(response.result.value, 'custom')
})

const parcelDemo = readFileSync(new URL('../../RuleParser/doc/Examples/Parcel-Dispatcher.txt', import.meta.url), 'utf8')
const clinicDemo = readFileSync(new URL('../../RuleParser/doc/Examples/Moonbase-Clinic.txt', import.meta.url), 'utf8')
const parcelModel = readFileSync(new URL('../../RuleParser/doc/Examples/Parcel-Dispatcher-model.txt', import.meta.url), 'utf8')
const clinicModel = readFileSync(new URL('../../RuleParser/doc/Examples/Moonbase-Clinic-model.txt', import.meta.url), 'utf8')
for (const [query, answers, expected] of [
  ['Assessment.Moonbloom', { 'Skin.Appearance': 'shimmering', 'Night.Sensation': 'tingling', 'Recent.Location': 'moon garden' }, true],
  ['Assessment.Moonbloom', { 'Skin.Appearance': 'ordinary' }, false],
  ['Assessment.Moonbloom', { 'Skin.Appearance': 'shimmering', 'Night.Sensation': 'calm' }, false],
  ['Assessment.Moonbloom', { 'Skin.Appearance': 'shimmering', 'Night.Sensation': 'tingling', 'Recent.Location': 'living quarters' }, false],
  ['Assessment.Echo.fever', { 'Voice.Effect': 'echoing', 'Ear.Sound': 'chiming', 'Recent.Location': 'resonator workshop' }, true],
  ['Assessment.Echo.fever', { 'Voice.Effect': 'echoing', 'Ear.Sound': 'quiet' }, false],
  ['Assessment.Starlight.depletion', { 'Energy.Level': 'low', 'Sleep.Pattern': 'fragmented', 'Shift.Hours': 10 }, true],
  ['Assessment.Starlight.depletion', { 'Energy.Level': 'low', 'Sleep.Pattern': 'fragmented', 'Shift.Hours': 6 }, false],
  ['Assessment.Starlight.depletion', { 'Energy.Level': 'steady' }, false],
]) {
  test(`fictional clinic ${query}: ${JSON.stringify(answers)}`, async () => {
    const response = await run('evaluate', { source: clinicDemo, modelSource: clinicModel, modelFileName: 'Moonbase-Clinic-model.txt', mode: 'deduction', query, context: {} }, (name, _, choices) => {
      assert.ok(Object.hasOwn(answers, name), `Unnecessary or internal question: ${name}`)
      if (typeof answers[name] === 'string') assert.ok(choices.includes(answers[name]))
      return answers[name]
    })
    assert.equal(response.type, 'result', JSON.stringify(response))
    assert.equal(response.result.queryResult, expected)
    assert.deepEqual(response.prompts, Object.keys(answers))
    assert.equal(response.result.context[query], expected ? true : undefined)
    if (expected) {
      assert.equal(response.result.trace.length, 3)
      assert.ok(response.result.context['Assessment.Explanation'])
    }
  })
}

test('declining a clinic observation stays unknown rather than disproving the hypothesis', async () => {
  const response = await run('evaluate', { source: clinicDemo, modelSource: clinicModel, modelFileName: 'Moonbase-Clinic-model.txt', mode: 'deduction', query: 'Assessment.Moonbloom', context: {} }, () => null)
  assert.equal(response.type, 'error')
  assert.deepEqual(response.missingFacts, ['Skin.Appearance'])
})

const computerDemo = readFileSync(new URL('../../RuleParser/doc/Examples/Computer-Assembly.txt', import.meta.url), 'utf8')
const computerModel = readFileSync(new URL('../../RuleParser/doc/Examples/Computer-Assembly-model.txt', import.meta.url), 'utf8')
for (const [profile, price] of [['office', 420], ['gaming', 1040], ['creator', 1630]]) {
  for (const mode of ['once', 'iteration']) {
    test(`computer assembly builds ${profile} in ${mode} mode`, async () => {
      const response = await run('evaluate', { source: computerDemo, modelSource: computerModel, modelFileName: 'Computer-Assembly-model.txt', mode, context: {} }, (name, _, choices) => {
        assert.equal(name, 'Build.Profile', 'Only the customer profile should require input')
        assert.deepEqual(choices, ['', 'office', 'gaming', 'creator'])
        return profile
      })
      assert.equal(response.type, 'result', JSON.stringify(response))
      assert.deepEqual(response.prompts, ['Build.Profile'])
      const facts = response.result.context
      const iterative = mode === 'iteration'
      assert.equal(facts['Build.Status'], iterative ? 'ready for delivery' : 'collecting components')
      assert.equal(facts['Platform.Status'], iterative ? 'ready' : 'pending')
      assert.equal(facts['System.Status'], iterative ? 'assembled' : 'pending')
      assert.equal(facts['Tests.Status'], iterative ? 'passed' : 'pending')
      assert.equal(facts['Software.Status'], iterative ? 'installed' : 'pending')
      assert.equal(facts['Build.Total.credits'], price + (iterative ? 80 : 0))
      assert.equal(facts['Build.Component.count'], 7)
      const components = facts['Build.Component.list'].split('; ').filter(Boolean)
      assert.equal(components.length, 7)
      assert.equal(new Set(components).size, 7)
      assert.equal(response.result.trace.length, iterative ? 13 : 8)
      if (iterative) assert.deepEqual(response.result.trace.slice(-5).map(entry => entry.rule), [
        'assemble.platform', 'assemble.system', 'test.computer', 'install.software', 'release.computer',
      ])
    })
  }
}

test('executing again reuses facts; clearing context requests fresh answers', async () => {
  const source = 'RuleContainer Test Rule choose If Size = "small" Then Price := 3 Rule other If Size = "big" Then Price := 8'
  const first = await run('evaluate', { source, mode: 'once', context: {} }, () => 'small')
  assert.equal(first.type, 'result')
  const second = await run('evaluate', { source, mode: 'once', context: first.result.context }, () => assert.fail('Known facts should not prompt'))
  assert.equal(second.type, 'result')
  assert.deepEqual(second.prompts, [])
  assert.equal(second.result.context.Price, 3)
  const fresh = await run('evaluate', { source, mode: 'once', context: {} }, () => 'big')
  assert.equal(fresh.type, 'result')
  assert.deepEqual(fresh.prompts, ['Size'])
  assert.equal(fresh.result.context.Price, 8)
})

for (const [contents, size, distance, weather, recipient, urgency, membership, declaredValue, vehicle, total, minutes] of [
  ['books', 'small', 2, 'clear', 'home', 'express', 'visitor', 25, 'drone', 23, 8],
  ['flowers', 'small', 2, 'rain', 'home', 'today', 'regular', 75, 'bike courier', 11, 85],
  ['ice cream', 'medium', 2, 'clear', 'parcel locker', 'express', 'neighbourhood hero', 20, 'refrigerated van', 26, 45],
  ['electronics', 'small', 2, 'storm', 'home', 'express', 'visitor', 300, 'delivery van', 25, 130],
  ['books', 'large', 30, 'clear', 'office', 'relaxed', 'neighbourhood hero', 25, 'regional van', 21, 210],
  ['electronics', 'medium', 5, 'clear', 'parcel locker', 'today', 'regular', 100, 'locker shuttle', 16, 105],
]) {
  for (const mode of ['once', 'iteration']) {
    test(`parcel dispatcher demo selects ${vehicle} in ${mode} mode`, async () => {
      const answers = {
        'Parcel.Contents': contents, 'Parcel.Size': size, 'Delivery.Distance.km': distance,
        'Weather.Now': weather, 'Recipient.Location': recipient, 'Delivery.Urgency': urgency,
        'Customer.Membership': membership, 'Parcel.Declared.value': declaredValue,
      }
      const response = await run('evaluate', { source: parcelDemo, modelSource: parcelModel, modelFileName: 'Parcel-Dispatcher-model.txt', mode, context: {} }, (name, _, choices) => {
        assert.ok(Object.hasOwn(answers, name), `Unexpected prompt: ${name}`)
        if (typeof answers[name] === 'string') assert.ok(choices.includes(answers[name]))
        else assert.deepEqual(choices, [])
        return answers[name]
      })
      assert.equal(response.type, 'result', JSON.stringify(response))
      const facts = response.result.context
      assert.equal(facts['Delivery.Vehicle'], vehicle)
      assert.ok(Math.abs(facts['Quote.Total'] - total) < 0.000001, `Expected ${total}, got ${facts['Quote.Total']}`)
      assert.equal(facts['Delivery.Estimated.minutes'], minutes)
      assert.equal(response.result.trace.length, 11)
      assert.equal(response.prompts.length, 8)
    })
  }
}
