# RuleEngine

RuleEngine is an experimental production-rule system written in C# and targeting .NET 10. It can parse a small textual rule language, evaluate expressions against a fact context, and execute rules using one of three inference strategies:

- **Fire once**: visit each rule once in priority order.
- **Iteration**: repeatedly visit rules, providing a basic forward-chaining mode.
- **Deduction**: work backwards from a query, providing a basic backward-chaining mode.

The repository also contains an interactive command shell, a query interpreter, and xUnit test projects.

## Documentation

- [Architecture](doc/architecture.md) explains the projects, runtime components, and evaluation flows.
- [Rule language](doc/rule-language.md) describes the supported syntax with examples.
- [Web workbench](Web/README.md) explains the Vue GUI and its browser runtime.

## Requirements

- .NET 10 SDK. The repository's [global.json](global.json) selects .NET 10 and rolls forward to the latest installed .NET 10 feature band.
- The sibling project `..\Utilities\Utilities.csproj`. `Rules.sln` references this project outside the repository.

## Build and test

From the repository root:

```powershell
dotnet restore Rules.sln
dotnet build Rules.sln
dotnet test Rules.sln
```

The web workbench uses the API project as its logic boundary. Run the API and Vue development server separately:

```powershell
dotnet run --project Rules.WebApi\Rules.WebApi.csproj --urls http://localhost:5215
cd Web
npm install
npm run dev
```

The solution currently builds successfully. At the time of the .NET 10 migration, 77 of 86 tests passed. The remaining failures are behavior assertions in the parser and deduction engine; see [Current constraints](doc/architecture.md#current-constraints).

## Try the command shell

```powershell
dotnet run --project CommandShell
```

Useful commands are:

| Command | Purpose |
| --- | --- |
| `lr <path>` | Load a rule file and select an evaluation strategy. |
| `er` | Execute the loaded rule containers. Deduction mode also asks for a query. |
| `calc <expression>` | Evaluate an arithmetic expression against the current context. |
| `sm` | Show the number of loaded containers and rules. |
| `cn` | Print the loaded rules. |
| `rt` | Print known facts and the last rule trace. |
| `cc` | Clear the fact context. |
| `cr` | Clear the loaded rules. |
| `help` | Show shell help. |
| `quit` | Exit. |

Example rule files are available in [`RuleParser/doc/Examples`](RuleParser/doc/Examples).

## Minimal example

```text
RuleContainer Determine.access

Rule allow.admin
Priority 1
If User.Role = "admin" and not User.Is.Suspended
Then Access.Is.Allowed
```

Loading this file in deduction mode allows a query such as:

```text
Access.Is.Allowed
```

If a required value cannot be derived from another rule, the command shell asks the user for it and stores the answer in the current fact context.

## Repository layout

| Path | Role |
| --- | --- |
| `Tokenizer` | Converts rule text into lexical tokens. |
| `RuleParser` | Parses tokens and builds executable rule/expression objects. |
| `Rules` | Contains expressions, facts, rules, inference engines, traces, and task orchestration. |
| `Interpreter/QueryInterpreter` | Converts a textual query into a comparison expression. |
| `CommandShell` | Interactive console application. |
| `Web` | Vue workbench providing the browser equivalent of the command-shell workflow. |
| `Rules.WebApi` | ASP.NET Core API exposing parsing, evaluation, and calculation to `Web`. |
| `TokenizerTest` | Tokenizer tests. |
| `RuleParserTest` | Parser and builder tests. |
| `RuleTest` | Expression and inference tests. |
| `Interpreter` | Legacy interpreter project; not included in `Rules.sln`. |
