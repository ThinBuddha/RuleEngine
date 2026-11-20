import { proofValue } from './proof.js'

function quote(text) { return `“${text}”` }

function conditionText(condition) {
  if (condition.state === 'Succeeded') return `${quote(condition.text)} held`
  if (condition.state === 'Failed') return `${quote(condition.text)} did not hold`
  if (condition.state === 'Unknown' || condition.state === 'WaitingForInput')
    return `${quote(condition.text)} could not be settled`
  return null
}

function ruleSentence(rule) {
  const conditions = rule.children.filter(child => child.kind === 'Condition')
  const conclusions = rule.children.filter(child => child.kind === 'Conclusion')
  const passing = conditions.filter(child => child.state === 'Succeeded')
  const failing = conditions.find(child => child.state === 'Failed')
  const unresolved = conditions.find(child => child.state === 'Unknown' || child.state === 'WaitingForInput')
  const skipped = conditions.some(child => child.state === 'Skipped')
  const name = quote(rule.text)

  if (rule.state === 'Succeeded') {
    const reason = passing.length
      ? ` because ${passing.map(conditionText).join(' and ')}` : ''
    const result = conclusions.length
      ? ` It established ${conclusions.map(child => quote(child.text)).join(' and ')}.` : ' It applied.'
    return `The rule ${name} applied${reason}.${result}`
  }
  if (failing) return `We also tried ${name}, but it could not apply because ${conditionText(failing)}.${skipped ? ' Its remaining conditions were not checked.' : ''}`
  if (unresolved) return `The rule ${name} remains open because ${conditionText(unresolved)}.`
  if (rule.state === 'Error') return `The rule ${name} stopped with an error.`
  return `The rule ${name} did not establish its conclusion.`
}

export function proofStory(proof) {
  if (!proof?.root) return []
  const root = proof.root
  const paragraphs = [`We looked into ${quote(root.text)}.`]
  const facts = []
  const rules = []

  function visit(node) {
    if (node.kind === 'Evidence' && (node.source === 'SuppliedFact' || node.source === 'UserAnswer')) {
      const value = proofValue(node.value)
      if (node.state === 'Succeeded' && value !== '')
        facts.push(node.source === 'UserAnswer'
          ? `You told us ${quote(node.text)} was ${value}.`
          : `We already knew ${quote(node.text)} was ${value}.`)
      else if (node.state === 'WaitingForInput') facts.push(`We are waiting for a value for ${quote(node.text)}.`)
      else if (node.state === 'Unknown') facts.push(`We still need a value for ${quote(node.text)}.`)
    }
    for (const child of node.children || []) visit(child)
    if (node.kind === 'Rule') rules.push(ruleSentence(node))
  }
  visit(root)

  paragraphs.push(...facts, ...rules)
  if (root.state === 'Succeeded' && root.value === true)
    paragraphs.push(`So the answer to ${quote(root.text)} is yes.`)
  else if (root.state === 'Failed' && root.value === false)
    paragraphs.push(`So the answer to ${quote(root.text)} is no.`)
  else if (root.state === 'Unknown' || root.state === 'WaitingForInput')
    paragraphs.push(`For now, we cannot settle ${quote(root.text)}.`)
  else if (root.state === 'Error')
    paragraphs.push(`The reasoning stopped before we could settle ${quote(root.text)}.`)
  else
    paragraphs.push(`The reasoning about ${quote(root.text)} is still underway.`)
  return paragraphs
}
