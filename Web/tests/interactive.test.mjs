import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { once } from 'node:events'
import { before, after, test } from 'node:test'
import { fileURLToPath } from 'node:url'
import { readFileSync } from 'node:fs'

let server
let baseUrl
before(async () => {
  const assembly = fileURLToPath(new URL('../../Rules.WebApi/bin/Debug/net10.0/Rules.WebApi.dll', import.meta.url))
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
          const value = await answer(message.name, socket, message.choices)
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

async function parse(source) {
  const response = await fetch(`${baseUrl}/api/rules/parse`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ source }),
  })
  return { status: response.status, body: await response.json() }
}

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

const parcelDemo = readFileSync(new URL('../../RuleParser/doc/Examples/Demo-Parcel-Dispatcher.txt', import.meta.url), 'utf8')
for (const [contents, size, distance, weather, recipient, urgency, membership, declaredValue, vehicle, total, minutes] of [
  ['books', 'small', 2, 'clear', 'home', 'express', 'visitor', 25, 'drone', 23, 8],
  ['flowers', 'small', 2, 'rain', 'home', 'today', 'regular', 75, 'bike courier', 11, 85],
  ['ice cream', 'medium', 2, 'clear', 'parcel locker', 'express', 'neighbourhood hero', 20, 'refrigerated van', 26, 45],
  ['electronics', 'small', 2, 'storm', 'home', 'express', 'visitor', 300, 'delivery van', 25, 130],
  ['books', 'large', 30, 'clear', 'office', 'relaxed', 'neighbourhood hero', 25, 'regional van', 21, 210],
  ['electronics', 'medium', 5, 'clear', 'parcel locker', 'today', 'regular', 100, 'locker shuttle', 16, 105],
]) {
  test(`parcel dispatcher demo selects ${vehicle}`, async () => {
    const answers = {
      'Parcel.Contents': contents, 'Parcel.Size': size, 'Delivery.Distance.km': distance,
      'Weather.Now': weather, 'Recipient.Location': recipient, 'Delivery.Urgency': urgency,
      'Customer.Membership': membership, 'Parcel.Declared.value': declaredValue,
    }
    const response = await run('evaluate', { source: parcelDemo, mode: 'once', context: {} }, (name, _, choices) => {
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
