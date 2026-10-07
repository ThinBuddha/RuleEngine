// Keep exact source slices: comments, whitespace, and incomplete edits must survive tabs.
export function splitRuleContainers(source) {
  const tokens = /\/\/[^\r\n]*|\/\*[\s\S]*?(?:\*\/|$)|"[^"]*(?:"|$)|[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)*|[^\s]/g
  const headers = []
  let pending
  for (const match of source.matchAll(tokens)) {
    const token = match[0]
    if (token.startsWith('//') || token.startsWith('/*')) continue
    if (token.toLowerCase() === 'rulecontainer') {
      const lineStart = source.lastIndexOf('\n', match.index - 1) + 1
      const start = /^\s*$/.test(source.slice(lineStart, match.index)) ? lineStart : match.index
      pending = { start, name: 'Unnamed container' }
      headers.push(pending)
    } else if (pending) {
      if (/^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)*$/.test(token)) pending.name = token
      pending = null
    }
  }
  return headers.map((header, index) => ({
    name: header.name,
    text: source.slice(index === 0 ? 0 : header.start, headers[index + 1]?.start ?? source.length),
  }))
}
