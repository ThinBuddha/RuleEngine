# Deduction snapshots in the API

Snapshots are additive: existing request formats, answer messages, `queryResult`, `trace`, `rounds`, and `context` are unchanged. The browser displays Why snapshots in input prompts and completed/partial proofs in the evaluation trace view.

## While prompting: Why

WebSocket `/api/rules/interactive/evaluate` sends unknown-value messages with these fields:

| Field | Meaning |
| --- | --- |
| `type` | `"unknownValue"` |
| `name` | Identifier whose value is requested. |
| `choices` | Existing suggested string values. |
| `why` | Deduction snapshot at this prompt, or null outside deduction. |

The snapshot is captured before waiting for the answer. Its `activeNodeId` points to a `WaitingForInput` evidence node; following the containing tree up to the root explains the question. Subsequent prompts include newer snapshots. Previously received snapshots remain unchanged. Reading an explanation requires no new request, rerun, or special answer; reply using the existing `{ "type": "answer", "value": ... }` message.

## After evaluation: How

HTTP `/api/rules/evaluate` responses and WebSocket `result` payloads contain a `proofs` array. Deduction produces one proof for the query, even when no rule fired. With multiple containers, `container` is `"All containers"`; rule nodes retain their originating container names. `proofs` is empty for Fire once and Iteration. The legacy `trace` includes each fired rule with its original container name; `rounds` counts one combined deduction evaluation if rules fired, or zero if none fired.

When evaluation finishes but requires missing facts (including a declined answer), the existing HTTP 400 or WebSocket error includes `proofs` alongside `error` and `missingFacts`. These snapshots have finalized `Unknown` nodes and no active node. Other runtime errors return an empty proofs array when no completed container results are available; syntax/protocol errors do not promise a proof. A disconnected client cannot receive a final snapshot. Calculator results are unchanged, and calculator prompts have `why: null`.

## Snapshot shape

Each snapshot contains `container` (name), `root` (query node), and `activeNodeId` (integer during a prompt, null after completion).

Each node contains:

| Field | JSON type / meaning |
| --- | --- |
| `id` | Integer unique within this snapshot. |
| `kind` | `Query`, `Goal`, `Rule`, `Condition`, `Operand`, `Conclusion`, or `Evidence`. |
| `text` | Query/expression/identifier text, or container-qualified rule name. |
| `state` | `Evaluating`, `WaitingForInput`, `Succeeded`, `Failed`, `Unknown`, `Skipped`, or `Error`. |
| `value` | Boolean, number, string, or null; never a formatted replacement for the scalar value. |
| `source` | `SuppliedFact`, `UserAnswer`, `RuleConclusion`, or null. |
| `evidenceId` | Earlier supporting node ID in this same snapshot, or null. |
| `children` | Ordered array of child nodes. |

All property names use camelCase; enum names use the exact strings above. References use IDs, not nested object links, so reused evidence does not create JSON cycles. Node IDs are stable across prompts and the final snapshot of the same deduction evaluation, but are not global identifiers. Keep snapshots from different runs separate. Container names are labels, not globally unique keys.

The complete tree includes failed alternatives and skipped conditions. The current How view retains them and provides links to supporting evidence. Any future filtering must preserve evidence referenced from those branches. A resolved Boolean false is successful evidence; a false condition is failed; lack of evidence is unknown. The tree does not change the legacy query-result policy, which can return false for an unestablished query.

Snapshots contain reasoning and observed values, so treat them as session data just like the fact context. They are not retained in a shared server store. HTTP and WebSocket serialization allow up to 256 nested JSON levels; extremely deep proofs remain outside that limit. This stage does not add cycle detection, cross-container provenance, or persisted proof history.
