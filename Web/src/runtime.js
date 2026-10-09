const configuredApi = import.meta.env?.VITE_RULES_API_URL?.replace(/\/$/, '')
const api = (path) => configuredApi ? `${configuredApi}${path}` : `/api${path}`

async function request(path, body) {
  const response = await fetch(api(path), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  const payload = await response.json().catch(() => ({}))
  if (!response.ok) {
    const missing = payload.missingFacts?.length ? ` Missing facts: ${payload.missingFacts.join(', ')}` : ''
    const error = new Error(`${payload.syntaxErrors?.length ? payload.syntaxErrors.join('; ') : payload.error || 'The Rules API request failed.'}${missing}`)
    error.missingFacts = payload.missingFacts || []
    error.proofs = payload.proofs || []
    throw error
  }
  return payload
}

export function starterRules() {
  return `RuleContainer Determine.how.to.travel

Rule walking
Priority 1
If not Outside.it.rains and Distance.to.travel = "small"
Then Travel.by.walking

Rule train
Priority 2
If Outside.it.rains and Distance.to.travel = "big"
Then Travel.by.train

Rule price
Priority 3
If Travel.country = "Netherlands"
Then Price.of.gas := "good"`
}

export async function parseRules(source, modelSource, modelFileName) {
  return request('/rules/parse', modelSource === undefined ? { source } : { source, modelSource, modelFileName })
}

function interactiveRequest(operation, body, getUserInput) {
  return new Promise((resolve, reject) => {
    const url = new URL(api(`/rules/interactive/${operation}`), window.location.href)
    url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:'
    const socket = new WebSocket(url)
    const promptLifetime = new AbortController()
    let settled = false
    function finish(error, result) {
      if (settled) return
      settled = true
      promptLifetime.abort()
      socket.close()
      if (error) reject(error)
      else resolve(result)
    }
    socket.onopen = () => socket.send(JSON.stringify(body))
    socket.onmessage = async ({ data }) => {
      try {
        const message = JSON.parse(data)
        if (message.type === 'unknownValue') {
          const value = await getUserInput(message.name, message.choices || [], promptLifetime.signal, message.why)
          if (!settled) socket.send(JSON.stringify({ type: 'answer', value: value ?? null }))
        } else if (message.type === 'result') {
          finish(null, message.result)
        } else if (message.type === 'error') {
          const error = new Error(message.syntaxErrors?.length ? message.syntaxErrors.join('; ') : message.error)
          error.proofs = message.proofs || []
          error.missingFacts = message.missingFacts || []
          finish(error)
        } else {
          finish(new Error('Unexpected Rules API response.'))
        }
      } catch (error) {
        finish(error)
      }
    }
    socket.onerror = () => finish(new Error('The Rules API connection failed.'))
    socket.onclose = () => finish(new Error('The Rules API connection closed before evaluation completed.'))
  })
}

export async function evaluate({ source, mode, context, query, modelSource, modelFileName }, getUserInput) {
  const body = { source, mode, query, context, modelSource, modelFileName }
  const response = getUserInput
    ? await interactiveRequest('evaluate', body, getUserInput)
    : await request('/rules/evaluate', body)
  return { trace: response.trace, rounds: response.rounds, queryResult: response.queryResult, context: response.context, proofs: response.proofs || [] }
}

export async function calculate(expression, context, getUserInput, source, modelSource, modelFileName) {
  const body = { expression, context, source, modelSource, modelFileName }
  const response = getUserInput
    ? await interactiveRequest('calculate', body, getUserInput)
    : await request('/rules/calculate', body)
  return response.value
}
