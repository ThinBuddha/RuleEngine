function quote(value) { return `“${value}”` }

function list(values) {
  if (values.length < 2) return values[0] || ''
  if (values.length === 2) return `${values[0]} and ${values[1]}`
  return `${values.slice(0, -1).join(', ')}, and ${values.at(-1)}`
}

function outcome(conclusion) {
  const assignment = /^(.+?)\s*:=\s*(.+)$/.exec(conclusion)
  return assignment
    ? `set ${quote(assignment[1])} to ${assignment[2]}`
    : `established ${quote(conclusion)}`
}

export function traceStory(trace, mode) {
  const paragraphs = [mode === 'iteration'
    ? 'We checked the rules repeatedly so that new conclusions could enable other rules.'
    : 'We checked the rules once in priority order.']

  if (!trace.length) {
    paragraphs.push('No rule fired during this run.')
    return paragraphs
  }

  trace.forEach((entry, index) => {
    const conditions = (entry.conditions || []).map(quote)
    const conclusions = (entry.conclusions || []).map(outcome)
    const reason = conditions.length ? ` because ${list(conditions)} held` : ''
    const result = conclusions.length ? ` It ${list(conclusions)}.` : ''
    paragraphs.push(`${index === 0 ? 'First' : 'Then'}, the rule ${quote(entry.rule)} in ${quote(entry.container)} fired${reason}.${result}`)
  })

  paragraphs.push(mode === 'iteration'
    ? 'After another check, no further rule fired.'
    : 'That completed this run through the rules.')
  return paragraphs
}
