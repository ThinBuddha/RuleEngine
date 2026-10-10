# Rule Engine Workbench

This folder contains the Vue GUI companion to `CommandShell`. It keeps the shell's core workflow visible in a browser and calls `Rules.WebApi` for all rule logic:

- edit or import a rule file, selecting its companion model file at the same time when it uses `enforce model`;
- auto format rule source with aligned conditions and conclusions;
- parse containers and inspect their rule counts;
- import a model file and inspect its types and classes;
- choose fire-once, iteration, or deduction mode;
- enter a deduction query;
- execute rules and inspect the trace as a rule list or a short story;
- inspect and clear the fact context;
- calculate an expression against the current context.

The selected mode is sent with each evaluation request. Fire once visits each rule once. Iteration revisits rules whose conditions have not yet matched, but each rule can fire only once per evaluation. It stops when a complete pass fires no new rules. Fired-rule tracking resets for each evaluation; facts remain in the browser context until cleared. Iteration processes each container in sequence and visits its rules in their current order, so conflicting assignments can still depend on rule order.

During deduction, expand **Why this question?** in a value prompt to switch between the proof tree and its short story.

## Run locally

Install the dependencies and start Vite:

```powershell
npm install
npm run dev
```

Create a production build with:

```powershell
npm run build
```

The output is written to `dist/`.

Run `npm run test:interactive` with Node.js 22 or newer to build the API and test interactive evaluation against a temporary local server, including answer types, declined answers, concurrent clients, and disconnects.

## Runtime boundary

Deduction prompts include a **Why this question?** expander showing the partial proof with the current reasoning path opened and the requested fact highlighted. Opening it does not submit an answer. Completed evaluations show an expandable **Deduction explanation** below the existing trace. With multiple containers, deduction combines their rules temporarily and produces one tree for the query, including when no rule fired. Missing-fact errors also show partial proofs when available. Evidence links open and focus the supporting step. False, unknown, failed, and skipped steps are shown distinctly; failed alternatives are retained. Clear/reset/new runs discard old explanations. See the [snapshot JSON contract](../doc/api-proof-snapshots.md) for node fields, states, and evidence references.

Use **Read as story** in the deduction explanation to see the same result as a short narrative of known facts, attempted rules, and the outcome. **Show proof tree** returns to the detailed view. The story distinguishes a false answer from a question that remains unresolved.

Run `npm run test:ui` for component-rendering/runtime tests, and `npm run test:shell` for command-shell explanation tests. These complement the API integration tests; they are not real-browser end-to-end tests.

`src/runtime.js` calls `/api/rules/parse` over HTTP and uses WebSockets at `/api/rules/interactive/evaluate` and `/api/rules/interactive/calculate` for interactive execution. Unknown-value callbacks send a prompt to the browser and wait for its answer, then resume the same evaluation. Blank or cancelled answers return null to the engine. Sessions expire after 15 minutes. The HTTP evaluate/calculate endpoints remain available for noninteractive clients.

Each evaluation has its own engines, parsed rules, fact context (`ValueBag`), input callbacks, and traces. The API runtime is request-scoped, and simultaneous WebSocket conversations cannot exchange answers or facts. Facts retained by the browser are sent as JSON for the next run and reconstructed into a fresh server-side context; no engine is shared across runs or users. Containers within a single run intentionally share that run's context.

For rules without `enforce model`, parsing derives `modelTypes`, mapping each string ID to distinct literals encountered in comparisons and assignments. Interactive prompts use these derived `choices`: the GUI shows a select box when choices exist and a text field otherwise. Numeric-looking and Boolean-looking string choices remain strings. Empty derived literals are omitted because an empty free-text answer means no value. Computed string fragments are not offered as complete values. Conflicting known scalar types for an ID are reported as syntax errors. The calculator can receive the loaded rule source through its optional `source` field to use the same choices.

For enforced rules, each interactive prompt includes `inputType` resolved through the explicit classes and types. The popup uses declared string values, a true/false selector, or a numeric field with the declared integer/real kind and bounds. It displays the unit label and checks entered values before submitting. Explicit string lists may include an empty string as a selectable value. The API checks submitted values against the same model.

The parse response also includes a `model` tree built from dotted fact IDs. The GUI's **Model** workspace displays types and classes imported from an explicit model file.

When rules start with `enforce model "filename"`, parsing and evaluation require the matching model file. Select both files in the rule import dialog, or import the model into the **Model** workspace first. The API accepts `modelSource` and `modelFileName` alongside rule `source`; it returns all model and rule validation errors in `syntaxErrors`.

The Vite development server proxies HTTP and WebSocket requests to `Rules.WebApi` at `http://localhost:5215`. Production reverse proxies must also support WebSocket upgrades. The API target can be changed with `RULES_API_URL`; for a non-proxied build or preview, set `VITE_RULES_API_URL` to the API base including `/api`.

Start the API and web client in separate terminals:

```powershell
dotnet run --project ..\Rules.WebApi\Rules.WebApi.csproj --urls http://localhost:5215
npm install
npm run dev
```

When using a different API port, set it before starting Vite:

```powershell
$env:RULES_API_URL = 'http://localhost:51104'
npm run dev
```

The current browser evaluator is intentionally not a second production implementation. Its purpose is to make the UI and interaction model testable while the API boundary is designed.
