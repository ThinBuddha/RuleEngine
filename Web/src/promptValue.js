export function parsePromptValue(raw, choices = [], inputType = null) {
  if (raw === null || raw === undefined) return { value: null }
  const value = String(raw)
  if (!inputType) {
    if (value.trim() === '') return { value: null }
    if (choices.length) return { value }
    if (/^(true|false)$/i.test(value.trim())) return { value: value.trim().toLowerCase() === 'true' }
    if (Number.isFinite(Number(value))) return { value: Number(value) }
    return { value }
  }
  if (inputType.kind === 'string') {
    if (inputType.values?.length) return inputType.values.includes(value)
      ? { value } : { error: 'Choose a permitted value.' }
    return value.trim() === '' ? { value: null } : { value }
  }
  if (inputType.kind === 'boolean') {
    if (value === 'true' || value === 'false') return { value: value === 'true' }
    return { error: 'Choose true or false.' }
  }
  if (value.trim() === '') return { value: null }
  const number = Number(value)
  if (!Number.isFinite(number)) return { error: 'Enter a number.' }
  if (inputType.kind === 'integer' && !Number.isInteger(number)) return { error: 'Enter a whole number.' }
  const min = inputType.minimum
  const max = inputType.maximum
  if (min && (number < min.value || (number === min.value && !min.inclusive)))
    return { error: `Enter a value ${min.inclusive ? 'at least' : 'greater than'} ${min.value}.` }
  if (max && (number > max.value || (number === max.value && !max.inclusive)))
    return { error: `Enter a value ${max.inclusive ? 'at most' : 'less than'} ${max.value}.` }
  return { value: number }
}

export function inputHint(type) {
  if (!type) return ''
  const range = [type.minimum && `${type.minimum.inclusive ? '≥' : '>'} ${type.minimum.value}`,
    type.maximum && `${type.maximum.inclusive ? '≤' : '<'} ${type.maximum.value}`].filter(Boolean).join(' and ')
  return [type.kind === 'integer' ? 'Whole number' : type.kind === 'real' ? 'Number' : '', range, type.unit].filter(Boolean).join(' · ')
}
