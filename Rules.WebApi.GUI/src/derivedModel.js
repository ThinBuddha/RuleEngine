import { computed, ref } from 'vue'
import { derivedModelSource } from './modelSource.js'

export function useDerivedModel(source) {
  const parsed = ref(null)
  const canDeriveModel = computed(() => parsed.value !== null && parsed.value.source === source.value)
  function clearDerivedModel() { parsed.value = null }
  function acceptDerivedModel(text, response) {
    // Enforced models return no derived model from the API.
    parsed.value = response.containers?.length && response.model
      ? { source: text, model: response.model } : null
  }
  function createDerivedModelDraft() {
    return canDeriveModel.value ? derivedModelSource(parsed.value.model) : null
  }
  return { canDeriveModel, clearDerivedModel, acceptDerivedModel, createDerivedModelDraft }
}
