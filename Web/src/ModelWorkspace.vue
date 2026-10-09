<script setup>
import { ref } from 'vue'
import RuleEditor from './RuleEditor.vue'

defineProps({
  source: { type: String, default: '' },
  fileName: { type: String, default: '' },
  tab: { type: String, default: 'types' }
})
const emit = defineEmits(['update:source', 'update:fileName', 'update:tab'])
const picker = ref(null)

async function importFile(event) {
  const file = event.target.files?.[0]
  if (!file) return
  emit('update:source', await file.text())
  emit('update:fileName', file.name)
  event.target.value = ''
}

function navigate(event) {
  const tabs = Array.from(event.currentTarget.querySelectorAll('[role="tab"]'))
  const current = tabs.indexOf(event.target)
  let target
  if (event.key === 'ArrowRight') target = (current + 1) % tabs.length
  else if (event.key === 'ArrowLeft') target = (current + tabs.length - 1) % tabs.length
  else if (event.key === 'Home') target = 0
  else if (event.key === 'End') target = tabs.length - 1
  else return
  event.preventDefault()
  tabs[target].click()
  tabs[target].focus()
}
</script>

<template>
  <article class="panel editor model-workspace">
    <header>
      <div><label>MODEL</label><h2>Model workspace</h2></div>
      <div v-if="tab === 'types'">
        <button class="text-btn" @click="picker.click()">Import file</button>
        <input ref="picker" type="file" accept=".txt,.model" hidden @change="importFile">
      </div>
    </header>
    <div class="model-tabs" role="tablist" aria-label="Model workspace" @keydown="navigate">
      <button id="model-tab-types" type="button" role="tab" :aria-selected="tab === 'types'"
        aria-controls="model-panel-types" :tabindex="tab === 'types' ? 0 : -1"
        @click="emit('update:tab', 'types')">Types</button>
      <button id="model-tab-classes" type="button" role="tab" :aria-selected="tab === 'classes'"
        aria-controls="model-panel-classes" :tabindex="tab === 'classes' ? 0 : -1"
        @click="emit('update:tab', 'classes')">Classes</button>
    </div>
    <div v-if="tab === 'types'" id="model-panel-types" role="tabpanel" aria-labelledby="model-tab-types">
      <RuleEditor :model-value="source" aria-label="Type definitions editor"
        placeholder="Import a types file or enter unit and type declarations."
        @update:model-value="emit('update:source', $event)" />
      <footer><span>{{ fileName || 'Unsaved type definitions' }}</span><span>{{ source.split('\n').length }} lines</span></footer>
    </div>
    <div v-else id="model-panel-classes" role="tabpanel" aria-labelledby="model-tab-classes"
      class="empty large">No classes defined yet.</div>
  </article>
</template>

<style scoped>
.model-workspace { min-width: 0; }
.model-tabs { display: flex; gap: 4px; padding: 8px 10px 0; border-bottom: 1px solid #34465d; background: #101a29; }
.model-tabs button { padding: 11px 16px; border: 0; border-bottom: 2px solid transparent; border-radius: 6px 6px 0 0; color: #a5b5c7; background: transparent; font: 11px 'DM Mono', monospace; }
.model-tabs button:hover { color: #edf2ee; background: #172437; }
.model-tabs button[aria-selected="true"] { color: #c8f169; border-bottom-color: #c8f169; background: #1a293c; }
.model-tabs button:focus-visible { outline: 2px solid #c8f169; outline-offset: -3px; }
.model-workspace .empty.large { min-height: 420px; }
</style>
