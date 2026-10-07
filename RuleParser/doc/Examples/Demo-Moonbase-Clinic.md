# Moonbase clinic: a fictional deduction demo

This example resembles a symptom assessment, but its conditions, combinations, and thresholds are invented. It is not suitable for assessing anyone's health or making medical decisions.

Import `Demo-Moonbase-Clinic.txt`, choose **Deduction**, and enter one of these queries exactly, without `= true`:

- `Assessment.Moonbloom`
- `Assessment.Echo.fever`
- `Assessment.Starlight.depletion`

Choose **Start fresh** when comparing independent scenarios. There are 20 rules in one container. Keeping the rules together is important because the deduction engine searches the current container for rules that establish a requested fact.

## Try these scenarios

| Query | Answers in prompt order | Expected result |
|---|---|---|
| `Assessment.Moonbloom` | Skin: shimmering; night sensation: tingling; location: moon garden | Proven |
| `Assessment.Moonbloom` | Skin: ordinary | Not proven; no further questions |
| `Assessment.Moonbloom` | Skin: shimmering; night sensation: calm | Not proven; location is not requested |
| `Assessment.Moonbloom` | Skin: shimmering; night sensation: tingling; location: living quarters | Not proven |
| `Assessment.Echo.fever` | Voice: echoing; ears: chiming; location: resonator workshop | Proven |
| `Assessment.Echo.fever` | Voice: echoing; ears: quiet | Not proven; location is not requested |
| `Assessment.Starlight.depletion` | Energy: low; sleep: fragmented; shift hours: 10 | Proven |
| `Assessment.Starlight.depletion` | Energy: low; sleep: fragmented; shift hours: 6 | Not proven |
| `Assessment.Starlight.depletion` | Energy: steady | Not proven; no further questions |

The GUI offers dropdowns for the string observations and a numeric field for shift hours. Cancelling a required answer leaves it unknown and stops this assessment with a missing-value error; it is not a negative finding.

## What backward chaining does

For `Assessment.Moonbloom`, the engine starts at the conclusion of `assess.moonbloom`. It needs `Syndrome.Glow.status`, so it searches for rules that assign that status. Those rules require skin appearance and possibly night sensation, which have no deriving rules, so only those observations are requested from the user.

If that pattern matches, the engine then derives `Exposure.Moonpollen.status` by asking about the recent location. If both requirements match, it asserts the Boolean `Assessment.Moonbloom` and supplies an explanation. The successful trace is:

1. `glow.pattern`
2. `moonpollen.exposure`
3. `assess.moonbloom`

It never asks about voice, ears, energy, sleep, or shift duration for that query. An ordinary skin appearance ends this branch after one question. This selective questioning is the main difference from a forward run through the entire file.

## Explicit negative evidence versus unknown

Intermediate facts use `"matched"` and `"unmatched"` statuses. An observed nonmatching pattern is therefore a known result that lets the caller's condition evaluate to false. Simply failing to assert a Boolean intermediate fact would leave it unknown and could interrupt deduction instead.

The three `Assessment.*` goals use positive Boolean conclusions only. A “Not proven” query result means the selected hypothesis was not established by these rules; it does not assert that its Boolean fact is false, establish that the person is healthy, or rule out another hypothesis. Inspect the intermediate statuses in Fact context to see why a branch did not match.

Rules are listed with the assessment goals first and their prerequisites later. Deduction follows dependencies rather than requiring a preliminary initialization pass. `Assessment.Finding` and `Assessment.Explanation` are provided only when a goal is proven. The evaluation trace contains fired rules, not a full record of every rejected candidate.
