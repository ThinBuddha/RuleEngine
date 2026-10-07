<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import RuleEditor from './RuleEditor.vue'
import { splitRuleContainers } from './ruleContainers'

const props = defineProps({ modelValue: { type: String, default: '' } })
const emit = defineEmits(['update:modelValue'])
const sections = ref(splitRuleContainers(props.modelValue))
const active = ref(sections.value.length ? 0 : -1)
const tabs = ref(null)
let emittedSource = props.modelValue

watch(() => props.modelValue, (source) => {
  if (source === emittedSource) return
  sections.value = splitRuleContainers(source)
  active.value = sections.value.length ? 0 : -1
  emittedSource = source
})

const visibleSource = computed(() => active.value < 0 ? props.modelValue : sections.value[active.value]?.text ?? '')

function updateSource(text) {
  if (active.value < 0) {
    emittedSource = text
  } else {
    sections.value[active.value].text = text
    emittedSource = sections.value.map(section => section.text).join('')
  }
  emit('update:modelValue', emittedSource)
}

function refreshSections() {
  // Re-split after editing, not on every keystroke: partially typed headers
  // should not make the active editor jump to a different container.
  const offset = sections.value.slice(0, Math.max(0, active.value)).reduce((sum, section) => sum + section.text.length, 0)
  sections.value = splitRuleContainers(emittedSource)
  if (active.value >= 0) {
    let start = 0
    active.value = sections.value.findIndex(section => {
      start += section.text.length
      return start > offset
    })
  }
}

async function navigate(event) {
  const buttons = Array.from(tabs.value.querySelectorAll('[role="tab"]'))
  const current = buttons.indexOf(event.target)
  let target
  if (event.key === 'ArrowRight') target = (current + 1) % buttons.length
  else if (event.key === 'ArrowLeft') target = (current + buttons.length - 1) % buttons.length
  else if (event.key === 'Home') target = 0
  else if (event.key === 'End') target = buttons.length - 1
  else return
  event.preventDefault()
  buttons[target].click()
  await nextTick()
  buttons[target].focus()
  buttons[target].scrollIntoView({ block: 'nearest', inline: 'nearest' })
}
</script>

<template>
  <div class="container-editor">
    <div ref="tabs" class="container-tabs" role="tablist" aria-label="Rule containers" @keydown="navigate">
      <button v-for="(section, index) in sections" :id="`container-tab-${index}`" :key="index"
        type="button" role="tab" :aria-selected="active === index" aria-controls="container-source"
        :tabindex="active === index ? 0 : -1" :title="section.name" @click="active = index">
        {{ section.name }}
      </button>
      <button id="container-tab-all" type="button" role="tab" :aria-selected="active < 0"
        aria-controls="container-source" :tabindex="active < 0 ? 0 : -1" @click="active = -1">All source</button>
    </div>
    <div id="container-source" role="tabpanel" :aria-labelledby="active < 0 ? 'container-tab-all' : `container-tab-${active}`"
      @focusout="refreshSections">
      <RuleEditor :key="active" :model-value="visibleSource" @update:model-value="updateSource" />
    </div>
    <div class="container-caption">{{ active < 0 ? 'Complete rule file' : sections[active]?.name }} · {{ visibleSource.split('\n').length }} lines</div>
  </div>
</template>

<style scoped>
.container-editor { min-width: 0; }
.container-tabs { display: flex; gap: 4px; overflow-x: auto; padding: 8px 10px 0; border-bottom: 1px solid #34465d; background: #101a29; }
.container-tabs button { flex: 0 0 auto; max-width: 320px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; padding: 11px 14px; border: 0; border-bottom: 2px solid transparent; border-radius: 6px 6px 0 0; color: #a5b5c7; background: transparent; font: 11px 'DM Mono', monospace; }
.container-tabs button:hover { color: #edf2ee; background: #172437; }
.container-tabs button[aria-selected="true"] { color: #c8f169; border-bottom-color: #c8f169; background: #1a293c; }
.container-tabs button:focus-visible { outline: 2px solid #c8f169; outline-offset: -3px; }
.container-caption { padding: 8px 19px; color: #8494a9; font: 10px 'DM Mono', monospace; overflow-wrap: anywhere; }
</style>
