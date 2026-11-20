<script setup>
import { proofValue } from './proof.js'
defineProps({ node: Object, expanded: Set, activeId: Number, selectedId: Number, prefix: String })
defineEmits(['evidence'])
</script>

<template>
  <li>
    <details :id="`${prefix}-${node.id}`" :open="expanded.has(node.id)" :class="{ active: node.id === activeId, selected: node.id === selectedId }">
      <summary tabindex="0">
        <span class="kind">{{ node.kind }}</span> <strong>{{ node.text }}</strong>
        <span class="state" :class="node.state">{{ node.state }}</span>
        <code v-if="node.value !== null">{{ proofValue(node.value) }}</code>
        <span v-if="node.id === activeId">— current question</span>
      </summary>
      <p v-if="node.source" class="source">Source: {{ node.source }}</p>
      <button v-if="node.evidenceId !== null" type="button" class="evidence" @click="$emit('evidence', node.evidenceId)">Show supporting evidence #{{ node.evidenceId }}</button>
      <p v-if="!node.children.length && !node.source && node.evidenceId === null" class="source">No further steps recorded.</p>
      <ul v-if="node.children.length">
        <ProofNode v-for="child in node.children" :key="child.id" :node="child" :expanded="expanded" :active-id="activeId" :selected-id="selectedId" :prefix="prefix" @evidence="$emit('evidence', $event)" />
      </ul>
    </details>
  </li>
</template>

<style scoped>
li { list-style: none; margin: 6px 0; }
ul { padding-left: 18px; border-left: 1px solid #34465d; }
details { padding: 6px; border: 1px solid transparent; border-radius: 5px; }
summary { cursor: pointer; overflow-wrap: anywhere; line-height: 1.8; }
summary:focus-visible, button:focus-visible { outline: 2px solid #c8f169; }
.active { border-color: #c8f169; background: #c8f1690a; }
.selected { border-color: #7eb9ff; }
.kind, .source { color: #a8b8cc; font-size: 12px; }
.state { font-size: 11px; margin: 0 8px; color: #b8c5d8; }
.Succeeded { color: #c8f169; }
.Failed, .Error { color: #ffabab; }
.Unknown, .WaitingForInput { color: #ffd385; }
code { color: #a3cfff; white-space: pre-wrap; }
.source { margin: 4px 0; }
.evidence { background: transparent; border: 0; color: #a3cfff; text-decoration: underline; cursor: pointer; padding: 6px; }
</style>
