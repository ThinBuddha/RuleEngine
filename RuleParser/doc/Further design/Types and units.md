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

Measurements, including durations, are real-valued so fractional values are allowed. `Bits` and `Bytes` require whole values. Percentage values use a percent scale (`25 %` means 25 percent), with bounds supplied by the consuming type. Days and weeks represent fixed durations; calendar months, years, and currencies are outside this catalog. The catalog supplies numeric kinds, symbols, and conversion relationships for units of the same quantity.

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

Comparisons, assignments, additions, and subtractions accept matching units or predefined units of the same quantity. A comparison converts its right operand to the left operand's unit; addition and subtraction convert subsequent terms to the first term's unit. Assignment converts the result to the destination property's declared unit. Context values and prompted inputs remain expressed in each property's declared unit. For example, `10 mm` equals `1 cm`, and assigning `1 cm + 5 mm` to a millimeter property stores `15`.

Multiplication by a unitless scalar and division by a unitless scalar preserve the unit. Real-valued units use floating-point arithmetic, including division of integer-written measurements. Multiplication of two unit-bearing values and division by a unit-bearing value are rejected. Compound units such as `MetersPerSecond` are predefined labels; the validator does not derive them from arithmetic on distance and time.

## Conversion relationships and warnings

`ModelUnit.Quantity`, `Scale`, and `Offset` describe a relationship to a shared reference: `reference = value * Scale + Offset`. `TryGetConversionTo` derives the conversion in either direction between units of the same quantity. Equivalently, the target value is `(sourceValue * source.Scale + source.Offset - target.Offset) / target.Scale`. This covers proportional conversions and temperature offsets. For millimeters and centimeters, `C = M / 10` and `M = C * 10`; dividing millimeters by 1000 converts them to meters.

Related families include length, area, volume, mass, duration, temperature, speed, acceleration, ratios, angle, frequency, force, pressure, energy, power, current, voltage, resistance, data size, and data rate. Decimal data prefixes use powers of 1000 and binary prefixes use powers of 1024. Imperial lengths use the international foot; `Acres` uses 43,560 international square feet. Conversion references are [NIST SP 811](https://www.nist.gov/pml/special-publication-811/nist-guide-si-appendix-b-conversion-factors/nist-guide-si-appendix-b8) and [NIST binary prefixes](https://physics.nist.gov/cuu/Units/binary.html).

When rules reference multiple predefined units for the same quantity, `RuleModelValidator.Warnings` reports one warning per quantity. Model declarations alone do not trigger a warning. Loading still succeeds: the API parse response includes `warnings`, the workbench displays them, and the command-line loader prints them. Unrelated quantities, missing suffixes, and unknown units remain validation errors. User-defined units have no conversion relationship unless supplied programmatically; the declaration syntax does not define conversions.

## Validation limits

Rule validation checks model paths, expression kinds, unit compatibility, literal values, and numeric bounds. Literal bounds are checked after conversion to the property's declared unit. Conversions to integer units must produce whole values: known fractional literals are rejected during validation, and fractional computed conversions fail during execution rather than rounding. Conversions use double precision and can incur floating-point rounding.

Percentage literals are stored at their written numeric value in a percentage property, so `25 %` is stored as `25`; conversion to another ratio unit applies that unit's scale. Multiplying another measurement by a percentage is multiplication of two unit-bearing values and is rejected; it is not interpreted as an automatic division by 100. Temperature conversion treats values as readings on an absolute scale; separate temperature-difference units are not modeled.

Bounds and permitted string values are checked for literals where the validator can determine their values, and the API validates prompted input against the property's type. These checks do not establish that every computed result or preloaded context value satisfies the model's bounds.
