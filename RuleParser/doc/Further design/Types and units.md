# Types and units

This describes the implemented declarations in [Types and units.txt](Types%20and%20units.txt). `ModelParser` recognizes this syntax separately from `RuleParser`. It produces a parse tree and compiles unit, type, and class declarations into an explicit model. `ModelBuilder.Errors` reports duplicate declarations and attempts to redefine predefined units; `ExplicitModel.Validate()` checks references, duplicate string values, and numeric bounds. Both must be checked, along with syntax errors, before using a parsed model.

The [class syntax](Classes.md) lets properties refer to these types or directly to atomic types. In a combined model file, unit and type declarations must come before class declarations.

## Grammar shown by the example

This grammar covers unit and type declarations; class declarations are described in [Classes.md](Classes.md). Keywords are written below in lowercase. They are matched without regard to case, while declared names and their references are case sensitive.

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

Every `ExplicitModel` starts with the immutable `ModelUnit` definitions in [MetaModelUnits.cs](../../../Rules/Model/MetaModelUnits.cs), in its own unit dictionary. That file contains the complete predefined catalog. Model parsing therefore makes standard units available automatically to API and command-line rule validation; API input prompts receive the unit's numeric kind and symbol. Names use American English (`Kilometers`, `Liters`) and are case sensitive, as are symbols. User declarations cannot redefine a predefined name or reuse a predefined symbol; domain units such as `Points` and `Credits` remain supported.

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

A class property uses a declared type (`Weight of Weight`) or an atomic type (`Count of integer`); a unit name alone does not declare a property type. A custom unit's `short` value supplies its display label and literal suffix, without defining a conversion factor. Custom suffixes currently require identifier syntax, such as `points`; predefined symbols additionally support forms such as `%`, `°C`, and `km/h`.

`min = n` means `value >= n`; `min > n` means `value > n`. `max = n` means `value <= n`; `max < n` means `value < n`. A type with both bounds must admit at least one value of its unit's numeric kind.

## Loading and using a model

Model references are resolved after parsing, so types may refer to custom units defined later among the declarations before classes. Predefined units are available from the start.

A rules file enables model validation with `enforce model "filename"` before its rule containers. The API request must supply the model's text and matching filename. The command-line shell loads the referenced file relative to the rules file. Both loading paths check syntax, model-building errors, model validity, and rule validity.

In enforced rules, numeric literals representing unit-bearing values require the exact unit symbol, separated from the number by whitespace: `5 kg`, `1.5 min`, `25 %`, or `80 km/h`. Zero also requires a suffix. Unitless properties and scalar multipliers or divisors use plain numbers, for example `Parcel.Weight * 2`. Model declaration bounds and numeric answers to API input prompts remain plain numbers. Without `enforce model`, rule literals cannot have unit suffixes.

Comparisons, assignments, additions, and subtractions require matching units. Multiplication by a unitless scalar and division by a unitless scalar preserve the unit. Multiplication of two unit-bearing values and division by a unit-bearing value are rejected. Compound units such as `MetersPerSecond` are predefined labels; the validator does not derive them from arithmetic on distance and time.

## Validation limits

Rule validation checks model paths, expression kinds, unit compatibility, literal values, and numeric bounds. It does not convert between units: `1000 m` cannot be assigned to a type based on `Kilometers`. Percentage literals are stored at their written numeric value, so `25 %` is stored as `25`; percentage arithmetic has no automatic division by 100.

Bounds and permitted string values are checked for literals where the validator can determine their values, and the API validates prompted input against the property's type. These checks do not establish that every computed result or preloaded context value satisfies the model's bounds.
