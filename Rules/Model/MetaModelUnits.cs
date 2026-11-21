using System;
using System.Collections.Generic;
using System.Linq;

namespace Rules.Model
{
    /// <summary>The standard units available in every explicit model. Symbols are case sensitive.</summary>
    public static class MetaModelUnits
    {
        public static IReadOnlyList<ModelUnit> All { get; } = Array.AsReadOnly(new ModelUnit[]
        {
            new("Millimeters", "real", "mm"),
            new("Centimeters", "real", "cm"),
            new("Meters", "real", "m"),
            new("Kilometers", "real", "km"),
            new("Inches", "real", "in"),
            new("Feet", "real", "ft"),
            new("Yards", "real", "yd"),
            new("Miles", "real", "mi"),
            new("SquareMillimeters", "real", "mm²"),
            new("SquareCentimeters", "real", "cm²"),
            new("SquareMeters", "real", "m²"),
            new("SquareKilometers", "real", "km²"),
            new("Hectares", "real", "ha"),
            new("Acres", "real", "ac"),
            new("Milliliters", "real", "mL"),
            new("Liters", "real", "L"),
            new("CubicCentimeters", "real", "cm³"),
            new("CubicMeters", "real", "m³"),
            new("Micrograms", "real", "µg"),
            new("Milligrams", "real", "mg"),
            new("Grams", "real", "g"),
            new("Kilograms", "real", "kg"),
            new("MetricTons", "real", "t"),
            new("Ounces", "real", "oz"),
            new("Pounds", "real", "lb"),
            new("Microseconds", "real", "µs"),
            new("Milliseconds", "real", "ms"),
            new("Seconds", "real", "s"),
            new("Minutes", "real", "min"),
            new("Hours", "real", "h"),
            new("Days", "real", "d"),
            new("Weeks", "real", "wk"),
            new("DegreesCelsius", "real", "°C"),
            new("DegreesFahrenheit", "real", "°F"),
            new("Kelvin", "real", "K"),
            new("MetersPerSecond", "real", "m/s"),
            new("KilometersPerHour", "real", "km/h"),
            new("MilesPerHour", "real", "mph"),
            new("Knots", "real", "kn"),
            new("MetersPerSecondSquared", "real", "m/s²"),
            new("Percentage", "real", "%"),
            new("PerMille", "real", "‰"),
            new("PartsPerMillion", "real", "ppm"),
            new("PartsPerBillion", "real", "ppb"),
            new("Degrees", "real", "°"),
            new("Radians", "real", "rad"),
            new("Hertz", "real", "Hz"),
            new("Kilohertz", "real", "kHz"),
            new("Megahertz", "real", "MHz"),
            new("Gigahertz", "real", "GHz"),
            new("Newtons", "real", "N"),
            new("Kilonewtons", "real", "kN"),
            new("Pascals", "real", "Pa"),
            new("Kilopascals", "real", "kPa"),
            new("Bars", "real", "bar"),
            new("Millibars", "real", "mbar"),
            new("PoundsPerSquareInch", "real", "psi"),
            new("Joules", "real", "J"),
            new("Kilojoules", "real", "kJ"),
            new("WattHours", "real", "Wh"),
            new("KilowattHours", "real", "kWh"),
            new("Watts", "real", "W"),
            new("Kilowatts", "real", "kW"),
            new("Megawatts", "real", "MW"),
            new("Amperes", "real", "A"),
            new("Milliamperes", "real", "mA"),
            new("Volts", "real", "V"),
            new("Millivolts", "real", "mV"),
            new("Ohms", "real", "Ω"),
            new("Bits", "integer", "bit"),
            new("Bytes", "integer", "B"),
            new("Kilobytes", "real", "kB"),
            new("Megabytes", "real", "MB"),
            new("Gigabytes", "real", "GB"),
            new("Terabytes", "real", "TB"),
            new("Kibibytes", "real", "KiB"),
            new("Mebibytes", "real", "MiB"),
            new("Gibibytes", "real", "GiB"),
            new("Tebibytes", "real", "TiB"),
            new("BitsPerSecond", "real", "bit/s"),
            new("KilobitsPerSecond", "real", "kbit/s"),
            new("MegabitsPerSecond", "real", "Mbit/s"),
            new("GigabitsPerSecond", "real", "Gbit/s"),
            new("BytesPerSecond", "real", "B/s")
        });

        public static IReadOnlyList<string> Symbols { get; } = Array.AsReadOnly(
            All.Select(unit => unit.ShortName).OrderByDescending(symbol => symbol.Length).ToArray());

        public static bool IsReservedName(string name) => All.Any(unit => unit.Name == name);
        public static bool IsReservedSymbol(string symbol) => All.Any(unit => unit.ShortName == symbol);
    }
}
