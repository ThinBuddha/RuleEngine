# Classes: proposed syntax

This describes the declarations in [Classes.txt](Classes.txt). It extends the [types and units syntax](Types%20and%20units.md). `ModelParser` also compiles declarations into explicit model objects.

## Grammar

Keywords are case-insensitive. Declared names retain their spelling.

If a model file contains units, types, and classes, all unit and type declarations must precede the first class declaration. Class declarations may follow one another.

```ebnf
classDeclaration = "class", className, "has", property, { property } ;

property         = propertyName, typeReference | propertyName, classReference ;
typeReference    = "of", ["type"], (typeName | atomicType) ;
classReference   = "as", ["class"], className ;
atomicType       = "integer" | "real" | "string" | "boolean" ;

className        = simpleId ;
propertyName     = simpleId ;
typeName         = simpleId, {".", simpleId} ;
simpleId         = (letter | "_"), {letter | digit | "_"} ;
```

A `class` keyword starts a new declaration and ends the preceding class. Each class has at least one property. Whitespace, including line breaks and indentation, separates tokens but does not delimit properties. The next property starts with its name after the preceding reference. No comma, semicolon, or closing keyword is used in the example.

Class and property names are single identifiers, so `Customer` and `Membership` are valid names, while `Customer.Member` is not valid for either. A type name may be a single identifier such as `Credits` or a dotted name such as `Customer.Membership`. The atomic names `integer`, `real`, `string`, and `boolean` are keywords rather than user-defined type names.

## Meaning and equivalent forms

- `credits of Credits` refers to the named type `Credits`.
- `Membership of Customer.Membership` refers to a dotted type name.
- `Parent as class Person` and `Child as Person` both refer to the class `Person`. A class may refer to itself.
- `Age of integer`, `Weight of real`, `Name of string`, and `Moonbloom of boolean` use atomic types directly.
- `Membership of type Customer.Membership` is equivalent to `Membership of Customer.Membership`; `Parent as class Person` is equivalent to `Parent as Person`.

The optional words `type` and `class` make a reference explicit; `of` always introduces a type or atomic type, and `as` always introduces a class. `of type integer` is therefore also valid.

## Further design

After parsing, model validation resolves referenced types and classes and reports duplicate declarations or properties. Rule validation walks dotted fact IDs from a root class through class properties to a typed leaf. The example does not define an empty class or property modifiers such as optionality or collections.
