<script setup>
import { computed, nextTick, ref, useId, watch } from 'vue'
import ProofNode from './ProofNode.vue'
import { indexProof } from './proof.js'
const props = defineProps({ proof: Object })
const prefix = useId()
const selectedId = ref(null)
watch(() => props.proof, () => { selectedId.value = null })
const index = computed(() => indexProof(props.proof.root))
const expanded = computed(() => new Set([
  props.proof.root.id,
  ...(index.value.paths.get(props.proof.activeNodeId) || []),
  ...(index.value.paths.get(selectedId.value) || []),
]))
async function showEvidence(id) {
  // Reset first so a manually collapsed target can be opened again.
  selectedId.value = null
  await nextTick()
  selectedId.value = id
  await nextTick()
  for (const ancestor of index.value.paths.get(id) || []) {
    const details = document.getElementById(`${prefix}-${ancestor}`)
    if (details) details.open = true
  }
  const target = document.getElementById(`${prefix}-${id}`)
  target?.querySelector('summary')?.focus()
  target?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}
</script>

<template>
  <section class="proof-tree" :aria-label="`Explanation for ${proof.container}`">
    <h3>{{ proof.container }} <small>{{ proof.root.state }}</small></h3>
    <p>Expand steps to inspect the reasoning. Unknown means not established, not false. Failed alternatives are retained.</p>
    <ul><ProofNode :node="proof.root" :expanded="expanded" :active-id="proof.activeNodeId" :selected-id="selectedId" :prefix="prefix" @evidence="showEvidence" /></ul>
  </section>
</template>

<style scoped>
.proof-tree { padding: 12px; text-align: left; }
h3 { margin: 0; overflow-wrap: anywhere; font-size: 15px; }
small { color: #b8c5d8; margin-left: 10px; }
p { color: #a8b8cc; font-size: 12px; }
ul { padding: 0; }
</style>
