import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { once } from 'node:events'
import { before, after, test } from 'node:test'
import { fileURLToPath } from 'node:url'

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
          const value = await answer(message.name, socket)
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
