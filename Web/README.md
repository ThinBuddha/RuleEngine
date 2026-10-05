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

## Runtime boundary

`src/runtime.js` is an HTTP client, not a second rule engine. It calls `/api/rules/parse`, `/api/rules/evaluate`, and `/api/rules/calculate`; the Vite development server proxies those requests to `Rules.WebApi` at `http://localhost:5215`. The API target can be changed with `RULES_API_URL`; for a non-proxied build or preview, set `VITE_RULES_API_URL` to the API base including `/api`.

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
