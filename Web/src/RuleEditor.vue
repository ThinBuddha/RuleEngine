<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({ modelValue: { type: String, default: '' } })
const emit = defineEmits(['update:modelValue'])
const input = ref(null)
const highlight = ref(null)
let resizeObserver

const keywords = new Set(['rulecontainer', 'rule', 'priority', 'description', 'if', 'then', 'and', 'not'])
const tokens = computed(() => {
  // Match strings and comments first so their contents are never treated as code.
  // Also accept unfinished strings/comments while the user is typing.
  const pattern = /\/\/[^\n\r]*|\/\*[\s\S]*?(?:\*\/|$)|"[^"\n\r]*(?:"|$)|[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)*|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|:=|<>|<=|>=|[=<>+*/()\-]|\s+|[^\s]/g
  let declaration = false
  return Array.from(props.modelValue.matchAll(pattern), ([text]) => {
    let kind = ''
    if (text.startsWith('//') || text.startsWith('/*')) kind = 'comment'
    else if (text.startsWith('"')) kind = 'string'
    else if (/^\d/.test(text)) kind = 'number'
    else if (/^[A-Za-z_]/.test(text)) {
      const word = text.toLowerCase()
      kind = keywords.has(word) ? 'keyword' : declaration ? 'declaration' : 'identifier'
      declaration = word === 'rule' || word === 'rulecontainer'
    } else if (/\S/.test(text)) kind = 'operator'
    return { text, kind }
  })
})

function syncScroll() {
  if (!input.value || !highlight.value) return
  // Use the textarea's client dimensions to exclude native scrollbar space.
  highlight.value.style.width = `${input.value.clientWidth}px`
  highlight.value.style.height = `${input.value.clientHeight}px`
  highlight.value.scrollTop = input.value.scrollTop
  highlight.value.scrollLeft = input.value.scrollLeft
}

watch(() => props.modelValue, async () => {
  await nextTick()
  syncScroll()
})
onMounted(() => {
  resizeObserver = new ResizeObserver(syncScroll)
  resizeObserver.observe(input.value)
  syncScroll()
})
onBeforeUnmount(() => resizeObserver?.disconnect())
</script>

<template>
  <div class="rule-editor">
    <pre ref="highlight" class="rule-highlight" aria-hidden="true"><template v-for="(token, index) in tokens" :key="index"><span :class="token.kind">{{ token.text }}</span></template>{{ '\n' }}</pre>
    <textarea
      ref="input"
      class="rule-input"
      :value="modelValue"
      wrap="off"
      spellcheck="false"
      autocapitalize="off"
      autocomplete="off"
      autocorrect="off"
      aria-label="Rule source editor"
      @input="emit('update:modelValue', $event.target.value)"
      @scroll="syncScroll"
    ></textarea>
  </div>
</template>

<style scoped>
.rule-editor {
  position: relative;
  height: clamp(420px, 65vh, 900px);
  min-height: 360px;
  overflow: hidden;
  resize: vertical;
  background: #0e1725;
}
.rule-editor .rule-highlight, .rule-editor .rule-input {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  min-height: 0;
  margin: 0;
  padding: 19px;
  border: 0;
  border-radius: 0;
  font: 12px/1.75 'DM Mono', monospace;
  font-variant-ligatures: none;
  letter-spacing: normal;
  tab-size: 4;
  white-space: pre;
  overflow-wrap: normal;
}
.rule-editor .rule-highlight { overflow: hidden; pointer-events: none; color: #bfcbd8; }
.rule-editor .rule-input { overflow: auto; resize: none; background: transparent; color: transparent; caret-color: #edf2ee; -webkit-text-fill-color: transparent; }
.rule-input::selection { background: #44678a99; }
.keyword { color: #c8f169; }
.declaration { color: #f3ce85; }
.identifier { color: #a8cdeb; }
.string { color: #edb88b; }
.number { color: #c4a7f5; }
.operator { color: #e3e8f0; }
.comment { color: #83968b; }
@media (max-width: 520px) {
  .rule-editor .rule-highlight, .rule-editor .rule-input { padding: 14px; }
}
@media (forced-colors: active) {
  .rule-highlight { visibility: hidden; }
  .rule-editor .rule-input { color: CanvasText; -webkit-text-fill-color: CanvasText; caret-color: CanvasText; }
}
</style>
