// Format rule structure without interpreting expressions or changing quoted text/comments.
function codeMask(source) {
  const mask = source.split('')
  let mode = 'code'
  for (let i = 0; i < source.length; i++) {
    const char = source[i]
    const next = source[i + 1]
    if (mode === 'code') {
      if (char === '"') { mask[i] = ' '; mode = 'string'; continue }
      else if (char === '/' && next === '/') mode = 'line'
      else if (char === '/' && next === '*') mode = 'block'
      else continue
    }
    if (mode === 'string') {
      mask[i] = ' '
      if (char === '"') mode = 'code'
    } else if (mode === 'line') {
      mask[i] = ' '
      if (char === '\n' || char === '\r') mode = 'code'
    } else if (mode === 'block') {
      mask[i] = ' '
      if (char === '*' && next === '/') { mask[++i] = ' '; mode = 'code' }
    }
  }
  return mask.join('')
}

function words(source) {
  return Array.from(codeMask(source).matchAll(/[A-Za-z_][A-Za-z0-9_.]*/g), match =>
    ({ text: match[0].toLowerCase(), start: match.index, end: match.index + match[0].length }))
}

function compact(source) {
  if (source.includes('//') || source.includes('/*')) return source.trim()
  let result = ''
  let space = false
  for (let i = 0; i < source.length;) {
    const char = source[i]
    if (/\s/.test(char)) { space = true; i++; continue }
    if (space && result && !result.endsWith('\n')) result += ' '
    space = false
    if (char === '"') {
      const start = i++
      while (i < source.length) {
        if (source[i++] === '"') break
      }
      result += source.slice(start, i)
    } else result += source[i++]
  }
  return result.trim()
}

function clauses(source, start, end, conjunctions) {
  const boundaries = [start, ...conjunctions.map(word => word.start), end]
  return boundaries.slice(0, -1).map((position, index) =>
    compact(source.slice(index ? conjunctions[index - 1].end : position, boundaries[index + 1])))
}

function formatRule(source, start, end, tokens, newline) {
  const inside = tokens.filter(token => token.start >= start && token.start < end)
  const ifToken = inside.find(token => token.text === 'if')
  const thenToken = inside.find(token => token.text === 'then' && token.start > (ifToken?.start ?? end))
  if (!ifToken || !thenToken) return source.slice(start, end).trim()

  const metadata = inside.filter(token => token.start < ifToken.start &&
    (token.text === 'priority' || token.text === 'description'))
  const fields = [start, ...metadata.map(token => token.start), ifToken.start]
  const lines = fields.slice(0, -1).map((position, index) =>
    `\t${compact(source.slice(position, fields[index + 1]))}`)
  const conditionAnds = inside.filter(token => token.text === 'and' &&
    token.start > ifToken.end && token.start < thenToken.start)
  const conclusionAnds = inside.filter(token => token.text === 'and' && token.start > thenToken.end)
  const conditions = clauses(source, ifToken.end, thenToken.start, conditionAnds)
  const conclusions = clauses(source, thenToken.end, end, conclusionAnds)
  lines.push(`\tIf   ${conditions[0]}`)
  lines.push(...conditions.slice(1).map(clause => `\tand  ${clause}`))
  lines.push(`\tThen ${conclusions[0]}`)
  lines.push(...conclusions.slice(1).map(clause => `\tand  ${clause}`))
  return lines.join(newline)
}

export function formatRuleSource(source) {
  const tokens = words(source)
  const containers = tokens.filter(token => token.text === 'rulecontainer')
  if (!containers.length) return source
  const newline = source.includes('\r\n') ? '\r\n' : '\n'
  const parts = []
  const preamble = compact(source.slice(0, containers[0].start))
  if (preamble) parts.push(preamble)
  for (let i = 0; i < containers.length; i++) {
    const start = containers[i].start
    const end = containers[i + 1]?.start ?? source.length
    const rules = tokens.filter(token => token.text === 'rule' && token.start > start && token.start < end)
    const heading = compact(source.slice(start, rules[0]?.start ?? end))
    const formatted = [heading]
    for (let j = 0; j < rules.length; j++)
      formatted.push(formatRule(source, rules[j].start, rules[j + 1]?.start ?? end, tokens, newline))
    parts.push(formatted.join(newline + newline))
  }
  return parts.join(newline + newline) + (/\r?\n$/.test(source) ? newline : '')
}
