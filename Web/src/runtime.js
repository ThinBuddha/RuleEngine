const configuredApi = import.meta.env.VITE_RULES_API_URL?.replace(/\/$/, '')
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
    const error = new Error(`${payload.error || 'The Rules API request failed.'}${missing}`)
    error.missingFacts = payload.missingFacts || []
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

export async function parseRules(source) {
  const response = await request('/rules/parse', { source })
  return response.containers
}

export async function evaluate({ source, mode, context, query }) {
  const response = await request('/rules/evaluate', { source, mode, query, context })
  return { trace: response.trace, rounds: response.rounds, queryResult: response.queryResult, context: response.context }
}

export async function calculate(expression, context) {
  const response = await request('/rules/calculate', { expression, context })
  return response.value
}
