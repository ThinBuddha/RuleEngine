<script setup>
import { ref } from 'vue'
import RuleEditor from './RuleEditor.vue'
import { splitModelSource } from './modelSource'

defineProps({
  typesSource: { type: String, default: '' },
  classesSource: { type: String, default: '' },
  fileName: { type: String, default: '' },
  tab: { type: String, default: 'types' }
})
const emit = defineEmits(['update:typesSource', 'update:classesSource', 'update:fileName', 'update:tab'])
const picker = ref(null)

async function importFile(event) {
  const file = event.target.files?.[0]
  if (!file) return
  const { types, classes } = splitModelSource(await file.text())
  emit('update:typesSource', types)
  emit('update:classesSource', classes)
  emit('update:fileName', file.name)
  emit('update:tab', types ? 'types' : 'classes')
  event.target.value = ''
}

function lineCount(source) { return source ? source.split(/\r\n|\r|\n/).length : 0 }

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
      <div>
        <button class="text-btn" @click="picker.click()">Import model file</button>
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
      <RuleEditor :model-value="typesSource" aria-label="Type definitions editor"
        placeholder="Import a model file or enter unit and type declarations."
        @update:model-value="emit('update:typesSource', $event)" />
      <footer><span>{{ fileName || 'Unsaved model' }} · Types</span><span>{{ lineCount(typesSource) }} lines</span></footer>
    </div>
    <div v-else id="model-panel-classes" role="tabpanel" aria-labelledby="model-tab-classes">
      <RuleEditor :model-value="classesSource" aria-label="Class definitions editor"
        placeholder="Class declarations appear here when a model file is imported."
        @update:model-value="emit('update:classesSource', $event)" />
      <footer><span>{{ fileName || 'Unsaved model' }} · Classes</span><span>{{ lineCount(classesSource) }} lines</span></footer>
    </div>
  </article>
</template>

<style scoped>
.model-workspace { min-width: 0; }
.model-tabs { display: flex; gap: 4px; padding: 8px 10px 0; border-bottom: 1px solid #34465d; background: #101a29; }
.model-tabs button { padding: 11px 16px; border: 0; border-bottom: 2px solid transparent; border-radius: 6px 6px 0 0; color: #a5b5c7; background: transparent; font: 11px 'DM Mono', monospace; }
.model-tabs button:hover { color: #edf2ee; background: #172437; }
.model-tabs button[aria-selected="true"] { color: #c8f169; border-bottom-color: #c8f169; background: #1a293c; }
.model-tabs button:focus-visible { outline: 2px solid #c8f169; outline-offset: -3px; }
</style>
