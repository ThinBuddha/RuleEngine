<script setup>
import { computed } from 'vue'
import { traceStory } from './traceStory.js'

const props = defineProps({
  trace: { type: Array, required: true },
  mode: { type: String, required: true }
})
const paragraphs = computed(() => traceStory(props.trace, props.mode))
</script>

<template>
  <article class="trace-story" aria-label="Story of the evaluation">
    <p v-for="(paragraph, index) in paragraphs" :key="index">{{ paragraph }}</p>
  </article>
</template>

<style scoped>
.trace-story { max-width: 72ch; padding: 18px 22px 26px; color: #dce5df; }
p { margin: 0 0 16px; font-size: 14px; line-height: 1.8; overflow-wrap: anywhere; }
p:first-child { color: #edf2ee; font-size: 16px; font-weight: 700; }
p:last-child { margin-bottom: 0; color: #c8f169; font-weight: 700; }
</style>
