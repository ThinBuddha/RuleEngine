# Rule Engine Workbench illustrated walkthrough

Use the Workbench to load rules, inspect or derive a model, answer questions during evaluation, and explore how the rules reached a result. This walkthrough starts with the travel demo and then shows features of the larger parcel-dispatch example.

For setup and startup commands, see [Run locally](../Rules.WebApi.GUI/README.md#run-locally). Start both the API and the GUI, then open the browser address printed by Vite. The screenshots below show the current Workbench; some are cropped to focus on a particular panel.

## Find your way around

The sidebar switches between five work areas:

| Work area | What you do there |
| --- | --- |
| **Model** | Import, inspect, and edit type and class declarations, or review a model derived from rules. |
| **Rule workspace** | Edit or import rules, parse them, choose an evaluation mode, and start a run. |
| **Evaluation trace** | Inspect fired rules and, for deduction, the reasoning tree or story. |
| **Fact context** | Inspect the answers and facts currently available to the next run, or clear them. |
| **Calculator** | Evaluate an expression using the current facts. |

## Load and inspect rules

The Workbench initially loads a small travel example. The rules for walking and taking the train are:

```text
RuleContainer Determine.how.to.travel

Rule walking
Priority 1
If not Outside.it.rains and Distance.to.travel = "small"
Then Travel.by.walking

Rule train
Priority 2
If Outside.it.rains and Distance.to.travel = "big"
Then Travel.by.train

Rule price
Priority 3
If Travel.country = "Netherlands"
Then Price.of.gas := "good"
```

To use your own rules, open **Rule workspace** and choose **Import file**, or paste text into the editor. Importing parses the file automatically. After editing, click **Parse source** to check the updated text and refresh the loaded-container summary. A parsing error appears above the workspace.

Each rule container has its own editor tab. **All source** shows the complete file, including any model header. **Auto format** aligns conditions and conclusions across the rule file; parse again after formatting or editing.

For a larger example, import [Parcel-Dispatcher.txt](../RuleParser/doc/Examples/Parcel%20dispatcher/Parcel-Dispatcher.txt) together with its companion [Parcel-Dispatcher-model.txt](../RuleParser/doc/Examples/Parcel%20dispatcher/Parcel-Dispatcher-model.txt). Select both files in the same import dialog. This rule file enforces the companion model.

![Rule workspace with ten parcel-dispatch container tabs and the Parcel Handling rules](../RuleParser/doc/Screendumps/Workbench%20GUI/Many%20rule%20containers.png)

*The parcel example separates handling, size, route, weather, vehicle selection, and quote calculation into container tabs.*

Loading another rule file does not clear your existing facts. Use **Start fresh** when you want the new example to ask its questions again.

## Derive a model from rules

For rules without an `enforce model` header, the Workbench can infer property types and a class structure from the identifiers used in the rules.

1. Load the travel demo, or paste the travel rules above and click **Parse source**.
2. Click **Derive model**, beside **Parse source**.
3. The Workbench opens **Model** and fills the **Types** and **Classes** tabs.

The button appears only after a successful parse of rules without an enforced model. It disappears when the source changes, parsing fails, or loaded rules are cleared. Parse the current source again to make it available.

![Derived Types tab showing Boolean declarations and known string values from the travel rules](../RuleParser/doc/Screendumps/Workbench%20GUI/Derived%20types.png)

*For example, comparisons with `"small"` and `"big"` supply the known values for `Distance.to.travel`. Bare Boolean conditions and conclusions supply Boolean types.*

Switch to **Classes** to inspect the structure inferred from dotted identifiers. For example, `Travel.by.train` becomes a `Travel` class with a `by` reference to a class containing `train`.

![Derived Classes tab showing nested travel classes and comments identifying a reserved word in Price of gas](../RuleParser/doc/Screendumps/Workbench%20GUI/Derived%20classes.png)

*The comments identify details that need a decision before using the draft as an enforced model. Here, the `of` segment in `Price.of.gas` is a reserved model keyword.*

**Derive model replaces any text already in the Model workspace.** The result is an editable draft: numeric properties use `real`, while units, bounds, and integer constraints require your review. Unknown types and facts without a class-qualified name are marked with comments. Known string values come from the rules and may need expanding for your intended inputs.

Deriving a model does not add `enforce model` to the rules. Review the declarations and resolve the comments before using them as an explicit model.

## Import an explicit model

If a rule file starts with a header such as:

```text
enforce model "Parcel-Dispatcher-model.txt"
```

select the rule file and that named model file together through **Import file**. Alternatively, open **Model**, choose **Import model file**, and then import the rule file. The model filename must match the header.

The Model workspace splits the imported text between **Types** and **Classes**. You can edit both tabs. Parsing and running enforced rules checks them against that model; **Derive model** is unavailable for these rules.

Explicit models can describe units as well as types. The parcel example uses `km` for distance and `credits` for fees:

![Route Classification rules comparing Delivery Distance with 3 km and 15 km and assigning fees in credits](../RuleParser/doc/Screendumps/Workbench%20GUI/Use%20of%20unit%20short%20names.png)

*Numeric literals carry the short names declared by the model. These labels participate in model validation; they do not imply automatic conversion between units.*

Rules can also calculate conclusions from existing facts. In the quote container, `:=` assigns calculated values, and `and` joins several conclusions:

![Quote Calculation rule adding fees, applying a discount, and calculating estimated delivery time](../RuleParser/doc/Screendumps/Workbench%20GUI/Expressions%20in%20rules.png)

## Choose how to run the rules

Return to **Rule workspace** and select a mode in **Run model**:

| Mode | What happens | When to try it |
| --- | --- | --- |
| **Fire once** | Visits each rule once in priority order within its container. | You want a forward pass through the rules. |
| **Iteration** | Retries rules that have not fired until a complete pass fires none. Each rule fires at most once per evaluation. | A later rule can establish a fact needed by an earlier rule in the same container. |
| **Deduction** | Works backwards from a query to find supporting rules and facts. | You want to answer a particular question. |

For Fire once and Iteration, containers run sequentially and share facts. Deduction can follow dependencies across the loaded containers to answer one query.

For the travel example, select **Deduction** and enter `Travel.by.train` in **Query**. Click **Start fresh** to begin with no saved answers. **Execute rules** uses facts already in the context.

## Answer questions and inspect why they are needed

When the rules need a fact that is not available, a **Value needed** dialog opens. Enter or select a value, then click **Continue**. With an explicit model, the input follows the declared type, choices, bounds, and unit. Without one, known string values inferred from the rules can still appear in a dropdown.

For the train query, answer `true` when asked for `Outside.it.rains`. When the next question asks for `Distance.to.travel`, expand **Why this question?** before answering.

![Distance to travel question with the Why tree expanded from the train query to the current input request](../RuleParser/doc/Screendumps/Workbench%20GUI/Simple%20ruleset%20-%20why%20trace.png)

*The highlighted node marks the current question. Its ancestors show that the train rule needs to establish whether the distance is `"big"`.*

Click **Read as story** to see the same reasoning in prose. **Show why tree** switches back. Inspecting either explanation leaves the question open.

![Why explanation as a story stating that rain is true and the engine is waiting for the distance](../RuleParser/doc/Screendumps/Workbench%20GUI/Simple%20ruleset%20-%20why%20trace%20-%20as%20story.png)

Choose `big` and click **Continue** to complete this example. **Cancel** declines the requested value; it does not supply `false`.

## Inspect how the result was reached

After execution, the Workbench opens **Evaluation trace**. For the successful train query, the fired-rule list contains `train`. Below it, **Deduction explanation** shows the supporting proof.

![Evaluation trace listing the train rule and a successful deduction tree for Travel by train](../RuleParser/doc/Screendumps/Workbench%20GUI/Simple%20ruleset%20-%20how%20trace.png)

Expand nodes to inspect conditions, values, and conclusions. **Show supporting evidence** links take you to the evidence used for a fact. The tree also retains failed alternatives and marks conditions that were skipped.

Use **Read as story** in the deduction explanation for a narrative of the same result:

![Successful deduction story explaining that rain and a big distance established travel by train](../RuleParser/doc/Screendumps/Workbench%20GUI/Simple%20ruleset%20-%20how%20trace%20-%20as%20story.png)

*Rain is true and the distance is `"big"`, so the train rule establishes `Travel.by.train`.*

For Fire once and Iteration, the trace also offers **Read as story**, describing the fired rules and their conclusions. These modes do not produce a deduction proof.

### Try a result that cannot be established

Click **Start fresh**, keep the query `Travel.by.train`, and answer `true` for rain but `small` for distance. The train rule's distance condition fails, so it cannot establish the query.

![Deduction story showing that the train rule could not apply because the distance was small](../RuleParser/doc/Screendumps/Workbench%20GUI/Simple%20ruleset%20-%20cannot%20prove.png)

An empty fired-rule list can still have a useful deduction explanation. Read the proof to distinguish a failed condition from a missing value: **Unknown** means a fact was not established; a Boolean value of `false` is an established value. A **Not proven** result alone does not establish the opposite conclusion.

## Inspect facts and repeat an evaluation

Open **Fact context** to inspect supplied answers and facts established by rules. After the successful train example, it includes `Outside.it.rains`, `Distance.to.travel`, and `Travel.by.train`.

Use the controls according to what you want to retain:

| Control | Effect |
| --- | --- |
| **Execute rules** or **Execute again** | Runs using the current facts, including earlier answers and conclusions. |
| **Start fresh** | Clears facts and runs again, asking for missing values. |
| **Clear context** in Fact context | Clears facts and the previous result without starting another run. |
| **Clear** in Loaded containers | Clears the loaded rule summary and results; leaves the editor text and facts available. |
| **Reset demo** in the top bar | Restores the travel demo, clears facts, and parses the demo again. |

Use **Start fresh** when changing answers or comparing outcomes. Reusing the context also reuses facts derived during earlier evaluations.

## Calculate expressions

Open **Calculator**, enter an expression, and click **Calculate**. Parentheses and arithmetic precedence apply. Expressions can reference current facts; missing values are requested through the same input dialog.

Try the expression shown below:

```text
-(2 + 3) * 4 / 18
```

![Calculator evaluating an expression with integer operands and returning minus one](../RuleParser/doc/Screendumps/Workbench%20GUI/Calculator%20-%20integer.png)

*With integer operands, this example uses integer division and returns `-1`.*

Change the final operand to `18.0` and calculate again:

![Calculator evaluating the same expression with 18 point zero and returning a fractional result](../RuleParser/doc/Screendumps/Workbench%20GUI/Calculator%20-%20float.png)

*The decimal operand gives a floating-point result, approximately `-1.1111111111111112`.*

For more syntax and examples, see the [rule language reference](rule-language.md). For local startup, tests, and API integration details, see the [Workbench README](../Rules.WebApi.GUI/README.md).
