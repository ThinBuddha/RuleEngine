<script setup>
import { computed } from 'vue'
import ModelClassNode from './ModelClassNode.vue'

const props = defineProps({ model: { type: Object, required: true } })

function count(node) {
  const children = Object.values(node.classes || {})
  return children.reduce((total, child) => {
    const nested = count(child)
    return { classes: total.classes + nested.classes + 1, properties: total.properties + nested.properties }
  }, { classes: 0, properties: Object.keys(node.properties || {}).length })
}
const totals = computed(() => count(props.model))
</script>

<template>
  <div class="model-tree">
    <p>{{ totals.classes }} {{ totals.classes === 1 ? 'class' : 'classes' }} · {{ totals.properties }} {{ totals.properties === 1 ? 'property' : 'properties' }} inferred from the loaded rules. Expand a class to inspect its properties and known string values.</p>
    <ModelClassNode :node="model" />
  </div>
</template>

<style scoped>
.model-tree { padding: 16px 20px 24px; }
.model-tree p { margin: 0 0 15px; color: #a8b8cc; font-size: 12px; line-height: 1.5; }
</style>
