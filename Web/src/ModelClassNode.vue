<script setup>
import { computed } from 'vue'

const props = defineProps({ node: { type: Object, required: true }, depth: { type: Number, default: 0 } })
const properties = computed(() => Object.values(props.node.properties || {}))
const classes = computed(() => Object.values(props.node.classes || {}))
</script>

<template>
  <details class="model-class" :open="depth < 2">
    <summary><span class="class-name">{{ node.name || 'Model' }}</span><span class="class-count">{{ properties.length }} properties · {{ classes.length }} classes</span></summary>
    <div class="class-body">
      <div v-for="property in properties" :key="property.fullName" class="model-property">
        <div class="property-heading"><strong :title="property.fullName">{{ property.name }}</strong><span class="kind">{{ property.kind || 'unknown' }}</span></div>
        <div v-if="property.stringValues?.length" class="choices" :aria-label="`Known values for ${property.fullName}`">
          <span v-for="value in property.stringValues" :key="value">{{ value }}</span>
        </div>
      </div>
      <ModelClassNode v-for="child in classes" :key="child.fullName" :node="child" :depth="depth + 1" />
    </div>
  </details>
</template>

<style scoped>
.model-class { margin: 7px 0; border: 1px solid #2b3c51; border-radius: 8px; background: #121e2e; }
.model-class .model-class { margin-left: 15px; background: #101a29; }
summary { display: flex; align-items: center; gap: 11px; padding: 12px 14px; cursor: pointer; list-style-position: inside; }
summary::marker { color: #c8f169; }
.class-name { color: #dce5df; font-size: 12px; font-weight: 700; overflow-wrap: anywhere; }
.class-count { margin-left: auto; color: #8192a7; font: 10px 'DM Mono', monospace; white-space: nowrap; }
.class-body { padding: 0 12px 12px; }
.model-property { margin: 7px 0; padding: 10px 12px; border: 1px solid #26384c; border-radius: 7px; background: #172437; }
.property-heading { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.property-heading strong { color: #e6efe8; font: 11px 'DM Mono', monospace; overflow-wrap: anywhere; }
.kind { margin-left: auto; padding: 4px 7px; border-radius: 5px; color: #c8f169; background: #263d32; font: 10px 'DM Mono', monospace; }
.choices { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
.choices span { padding: 4px 6px; border: 1px solid #34465d; border-radius: 5px; color: #a9b9cc; font: 10px 'DM Mono', monospace; overflow-wrap: anywhere; }
@media (max-width: 520px) { .model-class .model-class { margin-left: 8px; } .class-count { white-space: normal; text-align: right; } }
</style>
