# Rule language reference

## Example

```text
RuleContainer Determine.shipping

Rule domestic.standard
Priority 10
If Order.Country = "Netherlands" and
   Order.Weight <= 20 and
   not Order.Is.Express
Then Shipping.Method := "standard" and
     Shipping.Is.Available
```

Keywords are case-insensitive. Whitespace and line breaks generally separate tokens but do not otherwise define structure.

## Containers and rules

A rule file consists of one or more containers:

```text
RuleContainer <container-name>

Rule <rule-name>
[Priority <integer>]
If <condition> [and <condition> ...]
Then <conclusion> [and <conclusion> ...]
```

Container and rule names are identifiers. Dots can be used to qualify names, for example `Pricing.Insurance` or `Customer.Is.Adult`.

`Priority` is optional and defaults to `1`. Lower numeric priorities run first in fire-once mode.

## Identifiers and literals

### Identifiers

Identifiers match this general shape:

```text
Customer
Customer.Age
order_line1.product_code
```

They start with a letter or underscore. Later characters may include letters, digits, underscores, and dot-separated segments.

Although keywords are case-insensitive, fact names are stored in a case-sensitive dictionary. Use consistent casing for identifiers throughout a rule set and its queries.

### Numeric literals

```text
42
-17
3.1415
1.5e+3
```

Integers and floating-point values are supported. Arithmetic between an integer and a floating-point value promotes the integer to floating point.

### String literals

```text
"Netherlands"
"standard delivery"
```

Strings are enclosed in double quotes. The tokenizer does not currently define an escape syntax for embedded quotes.

### Boolean values

There are no `true` or `false` literal keywords in rule syntax. A Boolean fact is expressed as a bare identifier, optionally prefixed with `not`:

```text
Customer.Is.Active
not Customer.Is.Blocked
```

## Conditions

A rule condition can be a Boolean identifier or a comparison:

```text
If Customer.Is.Active
If not Customer.Is.Blocked
If Customer.Age >= 18
If Customer.Country = "Netherlands"
```

Multiple conditions are joined with `and`:

```text
If Customer.Is.Active and
   Customer.Age >= 18 and
   not Customer.Is.Blocked
```

Every condition must be true for the rule to fire. `or` is not supported.

### Comparison operators

| Operator | Meaning |
| --- | --- |
| `=` | Equal |
| `<>` | Not equal |
| `>` | Greater than |
| `<` | Less than |
| `>=` | Greater than or equal |
| `<=` | Less than or equal |

## Conclusions

A bare identifier asserts a Boolean fact with value `true`:

```text
Then Application.Is.Approved
```

The `:=` operator assigns the value of an expression:

```text
Then Discount.Percentage := 10
Then Order.Total := Order.Subtotal + Order.Tax
Then Shipping.Method := "express"
```

Multiple conclusions can be joined with `and`:

```text
Then Application.Is.Approved and
     Application.RiskClass := "low"
```

`=` compares values in conditions; `:=` assigns values in conclusions.

## Expressions

Expressions support:

- addition and subtraction with `+` and `-`;
- multiplication and division with `*` and `/`;
- parentheses;
- unary minus;
- identifiers and numeric or string literals.

Multiplication and division bind more tightly than addition and subtraction:

```text
Order.Total := Order.Base + Order.Quantity * Order.UnitPrice
```

Parentheses can override precedence:

```text
Order.Total := (Order.Base + Order.Quantity) * Order.UnitPrice
```

Integer division produces an integer result. Include a floating-point operand when a fractional result is required:

```text
Ratio.Integer := 3 / 2
Ratio.Decimal := 3 / 2.0
```

String `+` concatenates strings. String subtraction returns the part before the first occurrence of the right-hand string, or the original value if there is no match. Multiplication and division are not supported for strings.

## Comments

Line comments begin with `//`:

```text
// Domestic orders are evaluated first.
Rule domestic
If Order.Is.Domestic
Then Shipping.Zone := 1
```

Block comments use `/* ... */`:

```text
/*
  Experimental rules retained for later work.
*/
```

## Complete deduction example

```text
RuleContainer Determine.how.to.travel

Rule gas.price.netherlands
If Travel.Country = "Netherlands"
Then Price.Of.Gas := "high"

Rule choose.train
If Distance.To.Travel = "long" and
   Price.Of.Gas = "high"
Then Travel.By.Train
```

With deduction selected, the query is:

```text
Travel.By.Train
```

The engine looks for a rule concluding `Travel.By.Train`. To evaluate that rule, it needs `Distance.To.Travel` and `Price.Of.Gas`. It can derive `Price.Of.Gas` from the first rule and asks the host application for values that no rule can derive.

## Features present in the object model but not in active syntax

The runtime model contains concepts that rule files cannot currently configure reliably:

- rule weights;
- rule stop flags;
- container stop conditions;
- container evaluation strategies;
- rule descriptions (a parser branch exists, but description tokens are not currently consumed and retained correctly).

Configure evaluation strategies in the embedding application or choose one when loading a file in `CommandShell`. Treat examples mentioning `Stop` as design material rather than supported syntax.
