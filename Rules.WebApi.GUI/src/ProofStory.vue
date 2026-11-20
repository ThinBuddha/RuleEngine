<script setup>
import { computed } from 'vue'
import { proofStory } from './proofStory.js'

const props = defineProps({ proof: { type: Object, required: true } })
const paragraphs = computed(() => proofStory(props.proof))
</script>

<template>
  <article class="proof-story" :aria-label="`Story explanation for ${proof.container}`">
    <p v-for="(paragraph, index) in paragraphs" :key="index">{{ paragraph }}</p>
  </article>
</template>

<style scoped>
.proof-story { max-width: 72ch; padding: 18px 22px 26px; color: #dce5df; }
p { margin: 0 0 16px; font-size: 14px; line-height: 1.8; overflow-wrap: anywhere; }
p:first-child { color: #edf2ee; font-size: 16px; font-weight: 700; }
p:last-child { margin-bottom: 0; color: #c8f169; font-weight: 700; }
</style>
