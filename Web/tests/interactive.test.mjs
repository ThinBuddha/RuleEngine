import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { once } from 'node:events'
import { before, after, test } from 'node:test'
import { fileURLToPath } from 'node:url'
import { readFileSync } from 'node:fs'

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
const clinicDemo = readFileSync(new URL('../../RuleParser/doc/Examples/Demo-Moonbase-Clinic.txt', import.meta.url), 'utf8')
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
    const response = await run('evaluate', { source: clinicDemo, mode: 'deduction', query, context: {} }, (name, _, choices) => {
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
  const response = await run('evaluate', { source: clinicDemo, mode: 'deduction', query: 'Assessment.Moonbloom', context: {} }, () => null)
  assert.equal(response.type, 'error')
  assert.deepEqual(response.missingFacts, ['Skin.Appearance'])
})

const computerDemo = readFileSync(new URL('../../RuleParser/doc/Examples/Demo-Computer-Assembly.txt', import.meta.url), 'utf8')
for (const [profile, price] of [['office', 420], ['gaming', 1040], ['creator', 1630]]) {
  for (const mode of ['once', 'iteration']) {
    test(`computer assembly builds ${profile} in ${mode} mode`, async () => {
      const response = await run('evaluate', { source: computerDemo, mode, context: {} }, (name, _, choices) => {
        assert.equal(name, 'Build.Profile', 'Only the customer profile should require input')
        assert.deepEqual(choices, ['office', 'gaming', 'creator'])
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
      const response = await run('evaluate', { source: parcelDemo, mode, context: {} }, (name, _, choices) => {
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
