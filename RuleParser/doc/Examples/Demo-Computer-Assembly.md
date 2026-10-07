# Computer assembly: one pass versus forward chaining

Import `Demo-Computer-Assembly.txt`. There are 21 rules in two containers, and one input: `Build.Profile`, with choices **office**, **gaming**, and **creator**. Component names, compatibility, and prices are fictional demo data.

Run once in **Fire once**, inspect Fact context and Evaluation trace, then choose **Start fresh** and run with the same profile in **Iteration**.

| Result | Fire once | Iteration |
|---|---|---|
| Component list and count | Seven selected components | The same seven components, without duplicates |
| Platform / system | pending / pending | ready / assembled |
| Tests / software | pending / pending | passed / installed |
| Build.Status | collecting components | ready for delivery |
| Fired rules in trace | 8 | 13 |

| Profile | Fire once: components only | Iteration: components plus software |
|---|---:|---:|
| office | 420 credits | 500 credits |
| gaming | 1040 credits | 1120 credits |
| creator | 1630 credits | 1710 credits |

## Why the results differ

The first container initializes all internal statuses to `"pending"`, the component list to an empty string, and both counters to zero. Pending is an explicit state; it does not treat an unknown Boolean as false. Initialization also prevents questions about internal assembly facts.

The second container deliberately lists dependent rules before their prerequisites. Its priorities match the file order, so both modes start in the same order:

1. Release the computer, if software and tests are complete.
2. Install software, if tests have passed.
3. Test the computer, if assembly and cooling are complete.
4. Assemble the system, if the platform, graphics, storage, and power are installed.
5. Assemble the platform, if CPU, memory, and chassis are installed.
6. Select and install individual components according to the profile.

Fire once skips the first five rules because their conditions are initially false. It installs the components at the bottom, but never revisits the skipped rules.

Iteration builds the outcome over successive passes through the assembly container:

| Pass | Newly completed work |
|---|---|
| 1 | CPU, memory, graphics, storage, power supply, chassis, and cooling |
| 2 | Platform assembly |
| 3 | System assembly |
| 4 | Tests |
| 5 | Software installation; adds 80 credits |
| 6 | Release for delivery |
| 7 | No new rule fires; iteration finishes |

Each component rule appends its model to `Build.Component.list`, increments `Build.Component.count`, and adds its price to `Build.Total.credits`. Fired-rule tracking is essential here: iteration must not append components or charge for them again on subsequent passes.

Look at `Build.Component.list`, `Build.Component.count`, `Build.Total.credits`, and `Build.Status` in Fact context. The trace shows the order in which the assembly stages became possible. The GUI's current “passes” counter counts contributing containers, not these internal iteration passes; both modes report two contributing containers.

This example targets Fire once and Iteration. Its initialization and cumulative assignments are not designed for Deduction.
