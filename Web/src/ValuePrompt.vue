<script setup>
import { nextTick, onBeforeUnmount, ref } from 'vue'
import ProofTree from './ProofTree.vue'

const dialog = ref(null)
const prompt = ref(null)
const answer = ref('')
const promptVersion = ref(0)
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

async function ask(name, choices, signal, why) {
  if (signal?.aborted) return null
  if (resolveAnswer) throw new Error('Another value is already being requested.')
  prompt.value = { name, choices, why }
  promptVersion.value++
  answer.value = ''
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
    <form v-if="prompt" @submit.prevent="finish(answer.trim() === '' ? null : answer)">
      <h2 id="value-title">Value needed</h2>
      <label for="fact-value">{{ prompt.name }}</label>
      <select v-if="prompt.choices.length" id="fact-value" v-model="answer" autofocus>
        <option value="">Choose a value…</option>
        <option v-for="choice in prompt.choices" :key="choice" :value="choice">{{ choice }}</option>
      </select>
      <input v-else id="fact-value" v-model="answer" autocomplete="off" autofocus>
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
</style>
