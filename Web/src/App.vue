<script setup>
import { computed, ref } from 'vue'
import { calculate, evaluate, parseRules, starterRules } from './runtime'
import ValuePrompt from './ValuePrompt.vue'
import ContainerRuleEditor from './ContainerRuleEditor.vue'

const valuePrompt = ref(null)
const busy = ref(false)

const source = ref(starterRules()); const mode = ref('once'); const query = ref('Travel.by.train'); const view = ref('workspace'); const containers = ref([]); const context = ref({}); const trace = ref([]); const rounds = ref(0); const result = ref(null); const error = ref(''); const status = ref('Ready'); const calcInput = ref('2 + 3 * 4'); const calcResult = ref(''); const picker = ref(null)
const modes = [{ value: 'once', label: 'Fire once', hint: 'Priority order' }, { value: 'iteration', label: 'Iteration', hint: 'Forward chaining' }, { value: 'deduction', label: 'Deduction', hint: 'Backward chaining' }]
const ruleCount = computed(() => containers.value.reduce((n, c) => n + c.rules.length, 0)); const factCount = computed(() => Object.keys(context.value).length); const modeLabel = computed(() => modes.find((m) => m.value === mode.value)?.label); const resultLabel = computed(() => result.value === null ? 'No query' : result.value ? 'Proven' : 'Not proven')
function parseAnswer(answer) { const value = answer.trim(); if (/^(true|false)$/i.test(value)) return value.toLowerCase() === 'true'; if (value !== '' && Number.isFinite(Number(value))) return Number(value); return answer }
async function requestValue(id, choices, signal) {
  const answer = await valuePrompt.value.ask(id, choices, signal)
  if (answer === null || answer.trim() === '') return null
  const value = choices.length ? answer : parseAnswer(answer)
  context.value[id] = value
  return value
}
async function load() { try { containers.value = await parseRules(source.value); trace.value = []; result.value = null; error.value = ''; status.value = `Loaded ${containers.value.length} container${containers.value.length === 1 ? '' : 's'}`; view.value = 'rules' } catch (e) { error.value = e.message; status.value = 'Load failed' } }
const completedRuns = ref(0)
async function execute(fresh = false) {
  if (busy.value) return
  busy.value = true
  status.value = 'Executing rules…'
  error.value = ''
  try {
    if (fresh) {
      clearContext()
      trace.value = []
      rounds.value = 0
      status.value = 'Executing with fresh answers…'
    }
    if (!containers.value.length) await load()
    if (!containers.value.length) return
    const run = await evaluate({ source: source.value, mode: mode.value, context: context.value, query: query.value }, requestValue)
    trace.value = run.trace
    rounds.value = run.rounds
    result.value = run.queryResult
    context.value = run.context || context.value
    completedRuns.value++
    status.value = mode.value === 'deduction'
      ? `Run ${completedRuns.value} completed · ${resultLabel.value} · ${trace.value.length} rules used`
      : `Run ${completedRuns.value} completed · ${trace.value.length} rules fired`
    view.value = 'trace'
  } catch (e) {
    error.value = e.message
    status.value = 'Execution stopped'
  } finally {
    busy.value = false
  }
}
function readFile(event) { const file = event.target.files?.[0]; if (!file) return; const reader = new FileReader(); reader.onload = () => { source.value = String(reader.result || ''); void load() }; reader.readAsText(file); event.target.value = '' }
function clearContext() { context.value = {}; result.value = null; status.value = 'Context cleared' }; function clearRules() { containers.value = []; trace.value = []; result.value = null; status.value = 'Rules cleared' }; async function calc() { if (busy.value) return; busy.value = true; try { const value = await calculate(calcInput.value, context.value, requestValue, containers.value.length ? source.value : undefined); calcResult.value = String(value); error.value = '' } catch (e) { error.value = e.message; calcResult.value = '' } finally { busy.value = false } }; function reset() { source.value = starterRules(); query.value = 'Travel.by.train'; clearContext(); void load() }
void load()
</script>

<template>
  <div class="app-shell">
    <ValuePrompt ref="valuePrompt" />
    <header class="topbar"><div class="brand"><div class="brand-mark"><i></i><i></i><i></i></div><div><strong>Rule Engine</strong><small>Workbench / Rules.WebApi client</small></div></div><div class="top-actions"><span class="status"><i></i>{{ status }}</span><button class="icon-btn" title="Reset demo" @click="reset">↺</button></div></header>
    <main class="layout">
      <aside class="sidebar"><span class="eyebrow">Workspace</span><nav><button :class="{ active: view === 'workspace' || view === 'rules' }" @click="view = 'workspace'">⌘ <span>Rule workspace</span></button><button :class="{ active: view === 'trace' }" @click="view = 'trace'">↳ <span>Evaluation trace</span><b v-if="trace.length">{{ trace.length }}</b></button><button :class="{ active: view === 'context' }" @click="view = 'context'">◈ <span>Fact context</span><b>{{ factCount }}</b></button><button :class="{ active: view === 'calculator' }" @click="view = 'calculator'">∑ <span>Calculator</span></button></nav><div class="sidebar-bottom"><div class="mini-card"><small>Loaded model</small><strong>{{ ruleCount }} <em>rules</em></strong><span>{{ containers.length }} container{{ containers.length === 1 ? '' : 's' }}</span></div><small class="version">GUI foundation · v0.1</small></div></aside>
      <section class="content"><div class="heading"><div><span class="eyebrow">Production rules / control room</span></div><span class="local"><i></i> Rules.WebApi connected</span></div>
        <div v-if="error" class="error" role="alert"><b>!</b>{{ error }}<button @click="error = ''">×</button></div>
        <template v-if="view === 'workspace' || view === 'rules'"><div class="top-grid"><article class="panel editor"><header><div><label>01</label><h2>Rule source</h2></div><div><button class="text-btn" @click="picker.click()">Import file</button><input ref="picker" type="file" accept=".txt,.rules" hidden @change="readFile"><button class="outline-btn" @click="load">Parse source</button></div></header><ContainerRuleEditor v-model="source" /><footer><span>● Rule syntax</span><span>{{ source.split('\n').length }} lines</span></footer></article><article class="panel execute"><header><div><label>02</label><h2>Run model</h2></div><span class="chip">{{ modeLabel }}</span></header><div class="modes"><button v-for="item in modes" :key="item.value" :class="{ selected: mode === item.value }" @click="mode = item.value"><i></i><span><strong>{{ item.label }}</strong><small>{{ item.hint }}</small></span></button></div><label v-if="mode === 'deduction'" class="field">Query<input v-model="query" placeholder="e.g. Travel.by.train"></label><button class="primary" :disabled="busy" @click="execute()">{{ busy ? 'Executing…' : 'Execute rules' }} <b>↗</b></button><button class="outline-btn fresh-run" :disabled="busy" @click="execute(true)">Start fresh</button><p class="helper">Execute reuses saved facts. Start fresh clears them and asks for values again.</p></article></div><div class="bottom-grid"><article class="panel"><header><div><label>03</label><h2>Loaded containers</h2></div><button class="text-btn" @click="clearRules">Clear</button></header><div v-if="containers.length" class="container-list"><div v-for="c in containers" :key="c.name" class="container-row"><span>◫</span><div><strong>{{ c.name }}</strong><small>{{ c.rules.length }} rules · priority-aware</small></div><b>→</b></div></div><div v-else class="empty">Parse a rule source to see its containers.</div></article><article class="panel result"><header><div><label>04</label><h2>Last result</h2></div><span :class="['badge', { proven: result === true, unknown: result === null }]">{{ resultLabel }}</span></header><strong>{{ result === null ? '—' : result ? 'YES' : 'NO' }}</strong><p>{{ trace.length ? `${trace.length} rule${trace.length === 1 ? '' : 's'} contributed to the last evaluation.` : 'Execute a model to populate the decision trace.' }}</p></article></div></template>
        <section v-if="view === 'trace'" class="panel detail"><header><div><label>TRACE</label><h2>Evaluation trace</h2></div><div class="trace-actions"><span class="chip">{{ modeLabel }} · {{ rounds }} pass{{ rounds === 1 ? '' : 'es' }}</span><button class="primary compact" :disabled="busy" @click="execute()">{{ busy ? 'Executing…' : 'Execute again' }}</button><button class="outline-btn" :disabled="busy" @click="execute(true)">Start fresh</button></div></header><div v-if="trace.length" class="trace-list"><div v-for="(item, i) in trace" :key="`${item.rule}-${i}`"><label>{{ String(i + 1).padStart(2, '0') }}</label><span><strong>{{ item.rule }}</strong><small>{{ item.container }} · because {{ item.reason }}</small></span><b>✓</b></div></div><div v-else class="empty large">Nothing traced yet. Choose a mode and execute the loaded rules.</div></section>
        <section v-if="view === 'context'" class="panel detail"><header><div><label>FACTS</label><h2>Fact context</h2></div><button class="text-btn" @click="clearContext">Clear context</button></header><div v-if="factCount" class="facts"><div v-for="(value, key) in context" :key="key"><small>{{ key }}</small><strong>{{ String(value) }}</strong></div></div><div v-else class="empty large">No facts have been established. Execute a rule set to build the context.</div></section>
        <section v-if="view === 'calculator'" class="panel detail calculator"><header><div><label>CALC</label><h2>Expression calculator</h2></div><span class="chip">Uses current context</span></header><label class="field">Expression<textarea v-model="calcInput" rows="3" spellcheck="false"></textarea></label><button class="primary compact" @click="calc">Calculate <b>↗</b></button><div class="calc-result"><small>Result</small><strong>{{ calcResult || '—' }}</strong></div></section>
        <footer class="content-footer"><span>Rule Engine Workbench</span><span>Browser UI · Rules.WebApi connected</span></footer>
      </section>
    </main>
  </div>
</template>

<style scoped>
.fresh-run { margin: 0 17px; }
button:disabled { cursor: wait; opacity: .6; }
.trace-actions { flex-wrap: wrap; padding: 8px 0; }
</style>
