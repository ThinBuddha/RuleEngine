<script setup>
import { computed, nextTick, onBeforeUnmount, ref } from 'vue'
import ProofTree from './ProofTree.vue'
import { inputHint, parsePromptValue } from './promptValue'

const dialog = ref(null)
const prompt = ref(null)
const answer = ref('')
const validationError = ref('')
const promptVersion = ref(0)
const isSelection = computed(() => prompt.value?.inputType?.kind === 'boolean' || !!prompt.value?.choices.length)
let resolveAnswer
let removeAbortListener

function finish(value = null) {
  const resolve = resolveAnswer
  resolveAnswer = null
  removeAbortListener?.()
  dialog.value?.close()
  prompt.value = null
  resolve?.(value)
}

function submit() {
  const result = parsePromptValue(answer.value, prompt.value.choices, prompt.value.inputType)
  if (result.error) { validationError.value = result.error; return }
  finish(result.value)
}

async function ask(name, choices, signal, why, inputType = null) {
  if (signal?.aborted) return null
  if (resolveAnswer) throw new Error('Another value is already being requested.')
  prompt.value = { name, choices, why, inputType }
  promptVersion.value++
  answer.value = inputType?.kind === 'boolean' || choices.length ? null : ''
  validationError.value = ''
  const result = new Promise((resolve) => { resolveAnswer = resolve })
  const cancel = () => finish()
  signal?.addEventListener('abort', cancel, { once: true })
  removeAbortListener = () => signal?.removeEventListener('abort', cancel)
  await nextTick()
  if (resolveAnswer) dialog.value.showModal()
  return result
}

onBeforeUnmount(() => finish())
defineExpose({ ask })
</script>

<template>
  <dialog ref="dialog" class="value-dialog" aria-labelledby="value-title" @cancel.prevent="finish()">
    <form v-if="prompt" @submit.prevent="submit">
      <h2 id="value-title">Value needed</h2>
      <label for="fact-value">{{ prompt.name }}</label>
      <select v-if="isSelection" id="fact-value" v-model="answer" autofocus>
        <option :value="null" disabled>Choose a value…</option>
        <template v-if="prompt.inputType?.kind === 'boolean'">
          <option value="true">True</option><option value="false">False</option>
        </template>
        <option v-for="choice in prompt.inputType?.kind === 'boolean' ? [] : prompt.choices" :key="choice" :value="choice">{{ choice === '' ? '(empty string)' : choice }}</option>
      </select>
      <input v-else id="fact-value" v-model="answer" :type="['integer', 'real'].includes(prompt.inputType?.kind) ? 'number' : 'text'"
        :step="prompt.inputType?.kind === 'integer' ? '1' : 'any'" autocomplete="off" autofocus>
      <small v-if="inputHint(prompt.inputType)" class="type-hint">{{ inputHint(prompt.inputType) }}</small>
      <p v-if="validationError" class="input-error" role="alert">{{ validationError }}</p>
      <details v-if="prompt.why" :key="promptVersion" class="why">
        <summary>Why this question?</summary>
        <ProofTree :proof="prompt.why" />
      </details>
      <div class="value-actions">
        <button type="button" class="outline-btn" @click="finish()">Cancel</button>
        <button type="submit" class="primary compact">Continue</button>
      </div>
    </form>
  </dialog>
</template>

<style scoped>
.value-dialog { width: min(420px, calc(100% - 32px)); padding: 24px; border: 1px solid #34465d; border-radius: 12px; color: #edf2ee; background: #121c2b; }
.value-dialog::backdrop { background: #0009; }
h2 { margin: 0 0 20px; font-size: 18px; }
label { display: block; margin-bottom: 10px; overflow-wrap: anywhere; }
select, input { width: 100%; padding: 12px; border: 1px solid #34465d; border-radius: 6px; background: #101a29; color: #edf2ee; font: inherit; }
select:focus, input:focus { outline: 2px solid #c8f169; }
.value-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 20px; }
.value-actions .primary { margin: 0; }
.value-dialog { width: min(820px, calc(100% - 32px)); max-height: 90vh; overflow: auto; }
.why { margin-top: 18px; }
.why > summary { cursor: pointer; color: #c8f169; }
.type-hint { display: block; margin-top: 8px; color: #a5b5c7; }
.input-error { margin: 8px 0 0; color: #ffc0b3; font-size: 12px; }
</style>
