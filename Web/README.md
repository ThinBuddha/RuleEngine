# Rule Engine Workbench

This folder contains the Vue GUI companion to `CommandShell`. It keeps the shell's core workflow visible in a browser and calls `Rules.WebApi` for all rule logic:

- edit or import a rule file;
- parse containers and inspect their rule counts;
- choose fire-once, iteration, or deduction mode;
- enter a deduction query;
- execute rules and inspect the trace;
- inspect and clear the fact context;
- calculate an expression against the current context.

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

`src/runtime.js` calls `/api/rules/parse` over HTTP and uses WebSockets at `/api/rules/interactive/evaluate` and `/api/rules/interactive/calculate` for interactive execution. Unknown-value callbacks send a prompt to the browser and wait for its answer, then resume the same evaluation. Blank or cancelled answers return null to the engine. Sessions expire after 15 minutes. The HTTP evaluate/calculate endpoints remain available for noninteractive clients.

Parsing derives `modelTypes`, mapping each string ID to distinct literals encountered in comparisons and assignments throughout the rule file. Interactive prompts include these `choices`; the GUI shows a select box when choices exist and a text field otherwise. Numeric-looking and Boolean-looking string choices remain strings. Empty literals are omitted because an empty answer means no value. Computed string fragments are not offered as complete values. Conflicting known scalar types for an ID are reported as syntax errors. The calculator can receive the loaded rule source through its optional `source` field to use the same choices.

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
