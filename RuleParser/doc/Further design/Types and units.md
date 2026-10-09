# Types and units: proposed syntax

This describes the declarations in [Types and units.txt](Types%20and%20units.txt). `ModelParser` recognizes this syntax separately from `RuleParser`. It produces a parse tree and syntax errors; it does not construct model objects or validate references and value domains yet.

The [class syntax](Classes.md) lets properties refer to these types or directly to atomic types. In a combined model file, unit and type declarations must come before class declarations.

## Grammar shown by the example

Keywords are written below in lowercase. As in the current rule language, they should be matched without regard to case, while declared names retain their spelling.

```ebnf
document          = { unitDeclaration | numericType | stringType | booleanType } ;

unitDeclaration   = "unit", name, "of", numericPrimitive,
                    "with", "short", "=", stringLiteral ;
numericPrimitive  = "integer" | "real" ;

numericType       = "type", name, "of", unitName,
                    ["with", numericBounds] ;
numericBounds     = minimum, ["and", maximum] | maximum ;
minimum           = "min", ("=" | ">"), numberLiteral ;
maximum           = "max", ("=" | "<"), numberLiteral ;

stringType        = "type", name, "of", "string",
                    ["with", "values", "=", "[", stringLiteral,
                     {",", stringLiteral}, "]"] ;
booleanType       = "type", name, "of", "boolean" ;
```

`name` and `unitName` are identifiers. `unitName` must resolve to a `unit` declaration. `numberLiteral` is an integer or real literal; a bound on an integer unit must be an integer. Whitespace, including line breaks, separates tokens but does not change their meaning. The keywords `unit` and `type` begin new declarations.

`with` may introduce a minimum, a maximum, or both (in that order). Omitting `with` leaves a numeric type unbounded. `and` is used only when both bounds are present.

`boolean` is available for facts that a rule asserts as a bare conclusion, such as `Assessment.Moonbloom`. Boolean types do not have range or value-list clauses.

## Meaning

- `unit Kilogram of real with short = "kg"` declares a named numeric unit backed by real values. `short` is its display label; it does not define a conversion factor.
- `unit Points of integer with short = "points"` declares an integer-backed unit. Multiple types can refer to the same unit.
- `type Weight of Kilogram with min = 0 and max = 100` accepts values from 0 through 100, inclusive.
- `type SuperWeight of Kilogram with min > 100 and max = 1000` excludes 100 and includes 1000.
- `type Lightweight of Kilogram with min = 0 and max < 10` includes 0 and excludes 10.
- `type HeavyWeight of Kilogram with min > 1000` has an exclusive minimum and no maximum.
- `type LowScore of Points with max = 2` has an inclusive maximum and no minimum.
- `type AnyWeight of Kilogram` is unbounded, but still uses the `Kilogram` unit and its `real` numeric kind.
- `type Urgency of string with values = ["relaxed", "today", "express"]` has a finite set of permitted strings. `type Name of string` has no listed value restriction.

`Priority` and `Score` demonstrate that types can share the `Points` unit while imposing different ranges. A type name and a unit name have different roles: the unit supplies the underlying numeric kind and label; the type supplies the admissible domain.

`min = n` means `value >= n`; `min > n` means `value > n`. `max = n` means `value <= n`; `max < n` means `value < n`. A type with both bounds must admit at least one value of its unit's numeric kind.

## Decisions still needed

- It does not say whether declarations can refer to units defined later in the file. A rules file can name a types file with `enforce model "filename"`, but loading and enforcing that file are not implemented yet.
- It does not define how typed facts are declared, how unit symbols appear in expressions, or whether values in different units can be converted.
- Semantic validation is still needed for unit references, numeric kinds, duplicate declarations, value lists, and bound satisfiability.

Until those choices are made, this grammar deliberately covers the forms present in the example rather than assigning behavior to additional forms.
