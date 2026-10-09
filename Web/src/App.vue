<script setup>
import { computed, ref } from 'vue'
import { calculate, evaluate, parseRules, starterRules } from './runtime'
import ValuePrompt from './ValuePrompt.vue'
import ContainerRuleEditor from './ContainerRuleEditor.vue'
import ProofTree from './ProofTree.vue'
import ModelWorkspace from './ModelWorkspace.vue'
import { splitModelSource } from './modelSource'

const valuePrompt = ref(null)
const busy = ref(false)
const proofs = ref([])
const typesSource = ref('')
const classesSource = ref('')
const typesFileName = ref('')
const modelTab = ref('types')
const modelSource = computed(() => [typesSource.value, classesSource.value].filter(Boolean).join('\n'))
let loadSequence = 0

const source = ref(starterRules()); const mode = ref('once'); const query = ref('Travel.by.train'); const view = ref('workspace'); const containers = ref([]); const context = ref({}); const trace = ref([]); const rounds = ref(0); const result = ref(null); const error = ref(''); const status = ref('Ready'); const calcInput = ref('2 + 3 * 4'); const calcResult = ref(''); const picker = ref(null)
const modes = [{ value: 'once', label: 'Fire once', hint: 'Priority order' }, { value: 'iteration', label: 'Iteration', hint: 'Forward chaining' }, { value: 'deduction', label: 'Deduction', hint: 'Backward chaining' }]
const ruleCount = computed(() => containers.value.reduce((n, c) => n + c.rules.length, 0)); const factCount = computed(() => Object.keys(context.value).length); const modeLabel = computed(() => modes.find((m) => m.value === mode.value)?.label); const resultLabel = computed(() => result.value === null ? 'No query' : result.value ? 'Proven' : 'Not proven')
async function requestValue(id, choices, signal, why, inputType) {
  const answer = await valuePrompt.value.ask(id, choices, signal, why, inputType)
  if (answer === null) return null
  context.value[id] = answer
  return answer
}
async function load() {
  const sequence = ++loadSequence
  const text = source.value
  proofs.value = []
  containers.value = []
  trace.value = []
  result.value = null
  error.value = ''
  status.value = 'Parsing rules…'
  try {
    const parsed = await parseRules(text, modelSource.value || undefined, typesFileName.value || undefined)
    if (sequence !== loadSequence) return
    containers.value = parsed.containers || []
    status.value = `Loaded ${containers.value.length} container${containers.value.length === 1 ? '' : 's'}`
    view.value = 'rules'
  } catch (e) {
    if (sequence !== loadSequence) return
    error.value = e.message
    status.value = 'Load failed'
  }
}
const completedRuns = ref(0)
async function execute(fresh = false) {
  if (busy.value) return
  busy.value = true
  status.value = 'Executing rules…'
  error.value = ''
  proofs.value = []
  trace.value = []
  result.value = null
  rounds.value = 0
  try {
    if (fresh) {
      clearContext()
      trace.value = []
      rounds.value = 0
      status.value = 'Executing with fresh answers…'
    }
    if (!containers.value.length) await load()
    if (!containers.value.length) return
    const run = await evaluate({ source: source.value, mode: mode.value, context: context.value, query: query.value,
      modelSource: modelSource.value || undefined, modelFileName: typesFileName.value || undefined }, requestValue)
    trace.value = run.trace
    proofs.value = run.proofs
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
    proofs.value = e.proofs || []
    if (proofs.value.length) view.value = 'trace'
    status.value = 'Execution stopped'
  } finally {
    busy.value = false
  }
}
async function readFile(event) {
  const files = Array.from(event.target.files || [])
  event.target.value = ''
  if (!files.length) return
  const contents = await Promise.all(files.map(async file => ({ name: file.name, text: await file.text() })))
  const rules = contents.find(file => /\benforce\s+model\s+"/i.test(file.text)) || contents[0]
  const modelName = rules.text.match(/\benforce\s+model\s+"([^"]+)"/i)?.[1]
  const companion = contents.find(file => file !== rules && file.name === modelName)
  if (companion) {
    const { types, classes } = splitModelSource(companion.text)
    typesSource.value = types
    classesSource.value = classes
    typesFileName.value = companion.name
  }
  source.value = rules.text
  void load()
}
function clearContext() { proofs.value = []; context.value = {}; result.value = null; status.value = 'Context cleared' }; function clearRules() { loadSequence++; proofs.value = []; containers.value = []; trace.value = []; result.value = null; status.value = 'Rules cleared' }; async function calc() { if (busy.value) return; busy.value = true; try { const value = await calculate(calcInput.value, context.value, requestValue, containers.value.length ? source.value : undefined, modelSource.value || undefined, typesFileName.value || undefined); calcResult.value = String(value); error.value = '' } catch (e) { error.value = e.message; calcResult.value = '' } finally { busy.value = false } }; function reset() { source.value = starterRules(); query.value = 'Travel.by.train'; clearContext(); void load() }
void load()
</script>

<template>
  <div class="app-shell">
    <ValuePrompt ref="valuePrompt" />
    <header class="topbar"><div class="brand"><div class="brand-mark"><i></i><i></i><i></i></div><div><strong>Rule Engine</strong><small>Workbench / Rules.WebApi client</small></div></div><div class="top-actions"><span class="status"><i></i>{{ status }}</span><button class="icon-btn" title="Reset demo" @click="reset">↺</button></div></header>
    <main class="layout">
      <aside class="sidebar"><span class="eyebrow">Workspace</span><nav><button :class="{ active: view === 'model-workspace' }" @click="view = 'model-workspace'">▦ <span>Model</span></button><button :class="{ active: view === 'workspace' || view === 'rules' }" @click="view = 'workspace'">⌘ <span>Rule workspace</span></button><button :class="{ active: view === 'trace' }" @click="view = 'trace'">↳ <span>Evaluation trace</span><b v-if="trace.length">{{ trace.length }}</b></button><button :class="{ active: view === 'context' }" @click="view = 'context'">◈ <span>Fact context</span><b>{{ factCount }}</b></button><button :class="{ active: view === 'calculator' }" @click="view = 'calculator'">∑ <span>Calculator</span></button></nav><div class="sidebar-bottom"><div class="mini-card"><small>Loaded model</small><strong>{{ ruleCount }} <em>rules</em></strong><span>{{ containers.length }} container{{ containers.length === 1 ? '' : 's' }}</span></div><small class="version">GUI foundation · v0.1</small></div></aside>
      <section class="content"><div class="heading"><div><span class="eyebrow">Production rules / control room</span></div><span class="local"><i></i> Rules.WebApi connected</span></div>
        <div v-if="error" class="error" role="alert"><b>!</b>{{ error }}<button @click="error = ''">×</button></div>
        <ModelWorkspace v-if="view === 'model-workspace'" v-model:types-source="typesSource"
          v-model:classes-source="classesSource"
          v-model:file-name="typesFileName" v-model:tab="modelTab" />
        <template v-if="view === 'workspace' || view === 'rules'"><div class="top-grid"><article class="panel editor"><header><div><label>01</label><h2>Rule source</h2></div><div><button class="text-btn" @click="picker.click()">Import file</button><input ref="picker" type="file" accept=".txt,.rules" multiple hidden @change="readFile"><button class="outline-btn" @click="load">Parse source</button></div></header><ContainerRuleEditor v-model="source" /><footer><span>● Rule syntax</span><span>{{ source.split('\n').length }} lines</span></footer></article><article class="panel execute"><header><div><label>02</label><h2>Run model</h2></div><span class="chip">{{ modeLabel }}</span></header><div class="modes"><button v-for="item in modes" :key="item.value" :class="{ selected: mode === item.value }" @click="mode = item.value"><i></i><span><strong>{{ item.label }}</strong><small>{{ item.hint }}</small></span></button></div><label v-if="mode === 'deduction'" class="field">Query<input v-model="query" placeholder="e.g. Travel.by.train"></label><button class="primary" :disabled="busy" @click="execute()">{{ busy ? 'Executing…' : 'Execute rules' }} <b>↗</b></button><button class="outline-btn fresh-run" :disabled="busy" @click="execute(true)">Start fresh</button><p class="helper">Execute reuses saved facts. Start fresh clears them and asks for values again.</p></article></div><div class="bottom-grid"><article class="panel"><header><div><label>03</label><h2>Loaded containers</h2></div><button class="text-btn" @click="clearRules">Clear</button></header><div v-if="containers.length" class="container-list"><div v-for="c in containers" :key="c.name" class="container-row"><span>◫</span><div><strong>{{ c.name }}</strong><small>{{ c.rules.length }} rules · priority-aware</small></div><b>→</b></div></div><div v-else class="empty">Parse a rule source to see its containers.</div></article><article class="panel result"><header><div><label>04</label><h2>Last result</h2></div><span :class="['badge', { proven: result === true, unknown: result === null }]">{{ resultLabel }}</span></header><strong>{{ result === null ? '—' : result ? 'YES' : 'NO' }}</strong><p>{{ trace.length ? `${trace.length} rule${trace.length === 1 ? '' : 's'} contributed to the last evaluation.` : 'Execute a model to populate the decision trace.' }}</p></article></div></template>
        <section v-if="view === 'trace'" class="panel detail"><header><div><label>TRACE</label><h2>Evaluation trace</h2></div><div class="trace-actions"><span class="chip">{{ modeLabel }} · {{ rounds }} pass{{ rounds === 1 ? '' : 'es' }}</span><button class="primary compact" :disabled="busy" @click="execute()">{{ busy ? 'Executing…' : 'Execute again' }}</button><button class="outline-btn" :disabled="busy" @click="execute(true)">Start fresh</button></div></header><div v-if="trace.length" class="trace-list"><div v-for="(item, i) in trace" :key="`${item.rule}-${i}`"><label>{{ String(i + 1).padStart(2, '0') }}</label><span><strong>{{ item.rule }}</strong><small>{{ item.container }} · because {{ item.conditions?.join(' and ') || item.reason }}</small></span><b>✓</b></div></div><div v-else-if="!proofs.length" class="empty large">Nothing traced yet. Choose a mode and execute the loaded rules.</div></section>
        <section v-if="view === 'context'" class="panel detail"><header><div><label>FACTS</label><h2>Fact context</h2></div><button class="text-btn" @click="clearContext">Clear context</button></header><div v-if="factCount" class="facts"><div v-for="(value, key) in context" :key="key"><small>{{ key }}</small><strong>{{ String(value) }}</strong></div></div><div v-else class="empty large">No facts have been established. Execute a rule set to build the context.</div></section>
        <section v-if="view === 'calculator'" class="panel detail calculator"><header><div><label>CALC</label><h2>Expression calculator</h2></div><span class="chip">Uses current context</span></header><label class="field">Expression<textarea v-model="calcInput" rows="3" spellcheck="false"></textarea></label><button class="primary compact" @click="calc">Calculate <b>↗</b></button><div class="calc-result"><small>Result</small><strong>{{ calcResult || '—' }}</strong></div></section>
        <section v-if="view === 'trace' && proofs.length" class="panel detail">
          <header><div><label>HOW</label><h2>Deduction explanation</h2></div></header>
          <ProofTree v-for="(proof, index) in proofs" :key="index" :proof="proof" />
        </section>
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
