// Produce an editable draft; leave uninferred constraints for the user to review.
export function derivedModelSource(model) {
  const types = ['// Derived from rules. Review types and known string values before enforcing.']
  const classes = []
  const names = new Map()
  const used = new Set(Object.values(model.classes || {}).map(node => node.name))
  const properties = []
  const reserved = new Set(['unit', 'type', 'of', 'with', 'short', 'integer', 'real', 'string',
    'boolean', 'min', 'max', 'values', 'class', 'has', 'as', 'and', 'not'])
  const hasDeclaration = node => Object.values(node.properties || {}).some(property => property.kind && !reserved.has(property.name)) ||
    Object.values(node.classes || {}).some(child => !reserved.has(child.name) && hasDeclaration(child))

  function register(node, top = false) {
    let name = top ? node.name : node.fullName.replaceAll('.', '_')
    const base = name
    for (let suffix = 2; !top && used.has(name); suffix++) name = `${base}_${suffix}`
    used.add(name)
    names.set(node, name)
    properties.push(...Object.values(node.properties || {}))
    for (const child of Object.values(node.classes || {})) register(child)
  }
  properties.push(...Object.values(model.properties || {}))
  for (const child of Object.values(model.classes || {})) register(child, true)

  for (const property of properties) {
    if (property.kind === 'numeric') {
      types.push(`// ${property.fullName}: numeric; property uses real. Units, bounds and integer constraints are not inferred.`)
      continue
    }
    const kind = { Boolean: 'boolean', string: 'string' }[property.kind]
    if (!kind) {
      types.push(`// TODO: choose a type for ${property.fullName} (not inferred).`)
      continue
    }
    const values = property.kind === 'string' && property.stringValues?.length
      ? ` with values = [${property.stringValues.map(value => `"${value}"`).join(', ')}]` : ''
    types.push(`type ${property.fullName} of ${kind}${values}`)
  }

  for (const property of Object.values(model.properties || {})) {
    classes.push(`// Unqualified fact ${property.fullName}: assign it to a class before enforcing a model.`)
  }
  function declare(node) {
    const lines = []
    const notes = []
    for (const property of Object.values(node.properties || {})) {
      if (reserved.has(property.name)) notes.push(`// TODO: ${property.fullName} uses a reserved model keyword; rename it before enforcing.`)
      else if (property.kind) lines.push(`  ${property.name} of ${property.kind === 'numeric' ? 'real' : property.fullName}`)
      else notes.push(`// TODO: ${property.fullName} needs a type before adding it as a property.`)
    }
    for (const child of Object.values(node.classes || {})) {
      if (reserved.has(child.name)) notes.push(`// TODO: ${child.fullName} uses a reserved model keyword; rename it before enforcing.`)
      else if (hasDeclaration(child)) lines.push(`  ${child.name} as ${names.get(child)}`)
      else notes.push(`// TODO: ${child.fullName} needs typed properties before linking this class.`)
    }
    classes.push(...notes, lines.length ? `class ${names.get(node)} has\n${lines.join('\n')}`
      : `// TODO: class ${names.get(node)} has no properties with inferred types.`)
    for (const child of Object.values(node.classes || {})) declare(child)
  }
  for (const child of Object.values(model.classes || {})) declare(child)
  // The model tokenizer requires CRLF to terminate line comments.
  return {
    types: (types.join('\n') + '\n').replaceAll('\n', '\r\n'),
    classes: (classes.join('\n\n') + '\n').replaceAll('\n', '\r\n'),
  }
}

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
