// Preserve the original text in each tab, including comments and whitespace.
export function splitModelSource(source) {
  const tokens = /\/\/[^\r\n]*|\/\*[\s\S]*?(?:\*\/|$)|"[^"]*(?:"|$)|[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)*|[^\s]/g

  for (const match of source.matchAll(tokens)) {
    const token = match[0]
    if (token.startsWith('//') || token.startsWith('/*') || token.startsWith('"')) continue
    if (token.toLowerCase() !== 'class') continue

    const lineStart = source.lastIndexOf('\n', match.index - 1) + 1
    const start = /^\s*$/.test(source.slice(lineStart, match.index)) ? lineStart : match.index
    return { types: source.slice(0, start), classes: source.slice(start) }
  }

  return { types: source, classes: '' }
}
