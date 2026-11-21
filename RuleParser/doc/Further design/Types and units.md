# Types and units: proposed syntax

This describes the declarations in [Types and units.txt](Types%20and%20units.txt). `ModelParser` recognizes this syntax separately from `RuleParser`. It produces a parse tree and compiles unit, type, and class declarations into an explicit model. Model validation checks references, duplicates, and numeric bounds.

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

`name` and `unitName` are identifiers. `unitName` must resolve to a predefined unit or a user `unit` declaration. `numberLiteral` is an integer or real literal; a bound on an integer unit must be an integer. Whitespace, including line breaks, separates tokens but does not change their meaning. The keywords `unit` and `type` begin new declarations.

`with` may introduce a minimum, a maximum, or both (in that order). Omitting `with` leaves a numeric type unbounded. `and` is used only when both bounds are present.

`boolean` is available for facts that a rule asserts as a bare conclusion, such as `Assessment.Moonbloom`. Boolean types do not have range or value-list clauses.

## Predefined units

Every `ExplicitModel` starts with the immutable `ModelUnit` definitions in `Rules/Model/MetaModelUnits.cs`, in its own unit dictionary. Model parsing therefore makes standard units available automatically to API and command-line rule validation and input prompts. Names use American English (`Kilometers`, `Liters`) and are case sensitive, as are symbols. User declarations cannot redefine a predefined name or reuse a predefined symbol; domain units such as `Points` and `Credits` remain supported.

Measurements, including durations, are real-valued so fractional values are allowed. `Bits` and `Bytes` require whole values. Percentage values use a percent scale (`25 %` means 25 percent), with bounds supplied by the consuming type. Days and weeks represent fixed durations; calendar months, years, and currencies are outside this catalog. The catalog supplies numeric kinds and symbols, without conversion factors or dimensional arithmetic.

```text
type Distance of Kilometers with min = 0
type Duration of Minutes with min = 0
type Discount of Percentage with min = 0 and max = 100
```

## Meaning

- `Kilograms` is a predefined real-valued unit with symbol `kg`; no declaration is needed.
- `unit Points of integer with short = "points"` declares an integer-backed unit. Multiple types can refer to the same unit.
- `type Weight of Kilograms with min = 0 and max = 100` accepts values from 0 through 100, inclusive.
- `type SuperWeight of Kilograms with min > 100 and max = 1000` excludes 100 and includes 1000.
- `type Lightweight of Kilograms with min = 0 and max < 10` includes 0 and excludes 10.
- `type HeavyWeight of Kilograms with min > 1000` has an exclusive minimum and no maximum.
- `type LowScore of Points with max = 2` has an inclusive maximum and no minimum.
- `type AnyWeight of Kilograms` is unbounded, but still uses the `Kilograms` unit and its `real` numeric kind.
- `type Urgency of string with values = ["relaxed", "today", "express"]` has a finite set of permitted strings. `type Name of string` has no listed value restriction.

`Priority` and `Score` demonstrate that types can share the `Points` unit while imposing different ranges. A type name and a unit name have different roles: the unit supplies the underlying numeric kind and label; the type supplies the admissible domain.

`min = n` means `value >= n`; `min > n` means `value > n`. `max = n` means `value <= n`; `max < n` means `value < n`. A type with both bounds must admit at least one value of its unit's numeric kind.

## Decisions still needed

- Model references are resolved after parsing, so types may refer to units defined later among the declarations before classes. A rules file can name a model file with `enforce model "filename"`; the API request must supply that file's text and matching filename.
- Numeric literals in rules using `enforce model` require the unit symbol, such as `5 kg`, `1.5 min`, `25 %`, or `80 km/h`. Units are checked without implicit conversion.
- Rule validation checks model paths, expression kinds, units in comparisons and additions, literal values, and numeric bounds. It cannot prove that every computed numeric result remains inside a target range.

Until those choices are made, this grammar deliberately covers the forms present in the example rather than assigning behavior to additional forms.
