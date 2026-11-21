using System;
using System.Collections.Generic;
using System.Linq;

namespace Rules.Model
{
    /// <summary>The standard units available in every explicit model. Symbols are case sensitive.</summary>
    public static class MetaModelUnits
    {
        // Conversion references: NIST SP 811 Appendix B.8 and physics.nist.gov/cuu/Units/binary.html.
        // Lengths use the international foot; Acres use 43560 international square feet.
        public static IReadOnlyList<ModelUnit> All { get; } = Array.AsReadOnly(new ModelUnit[]
        {
            new("Millimeters", "real", "mm", "length", 0.001),
            new("Centimeters", "real", "cm", "length", 0.01),
            new("Meters", "real", "m", "length", 1),
            new("Kilometers", "real", "km", "length", 1000),
            new("Inches", "real", "in", "length", 0.0254),
            new("Feet", "real", "ft", "length", 0.3048),
            new("Yards", "real", "yd", "length", 0.9144),
            new("Miles", "real", "mi", "length", 1609.344),
            new("SquareMillimeters", "real", "mm²", "area", 1e-6),
            new("SquareCentimeters", "real", "cm²", "area", 1e-4),
            new("SquareMeters", "real", "m²", "area", 1),
            new("SquareKilometers", "real", "km²", "area", 1e6),
            new("Hectares", "real", "ha", "area", 10000),
            new("Acres", "real", "ac", "area", 43560 * 0.3048 * 0.3048),
            new("Milliliters", "real", "mL", "volume", 1e-6),
            new("Liters", "real", "L", "volume", 0.001),
            new("CubicCentimeters", "real", "cm³", "volume", 1e-6),
            new("CubicMeters", "real", "m³", "volume", 1),
            new("Micrograms", "real", "µg", "mass", 1e-9),
            new("Milligrams", "real", "mg", "mass", 1e-6),
            new("Grams", "real", "g", "mass", 0.001),
            new("Kilograms", "real", "kg", "mass", 1),
            new("MetricTons", "real", "t", "mass", 1000),
            new("Ounces", "real", "oz", "mass", 0.45359237 / 16),
            new("Pounds", "real", "lb", "mass", 0.45359237),
            new("Microseconds", "real", "µs", "duration", 1e-6),
            new("Milliseconds", "real", "ms", "duration", 0.001),
            new("Seconds", "real", "s", "duration", 1),
            new("Minutes", "real", "min", "duration", 60),
            new("Hours", "real", "h", "duration", 3600),
            new("Days", "real", "d", "duration", 86400),
            new("Weeks", "real", "wk", "duration", 604800),
            new("DegreesCelsius", "real", "°C", "temperature", 1),
            new("DegreesFahrenheit", "real", "°F", "temperature", 5.0 / 9, -32 * 5.0 / 9),
            new("Kelvin", "real", "K", "temperature", 1, -273.15),
            new("MetersPerSecond", "real", "m/s", "speed", 1),
            new("KilometersPerHour", "real", "km/h", "speed", 1 / 3.6),
            new("MilesPerHour", "real", "mph", "speed", 1609.344 / 3600),
            new("Knots", "real", "kn", "speed", 1852.0 / 3600),
            new("MetersPerSecondSquared", "real", "m/s²", "acceleration", 1),
            new("Percentage", "real", "%", "ratio", 0.01),
            new("PerMille", "real", "‰", "ratio", 0.001),
            new("PartsPerMillion", "real", "ppm", "ratio", 1e-6),
            new("PartsPerBillion", "real", "ppb", "ratio", 1e-9),
            new("Degrees", "real", "°", "angle", Math.PI / 180),
            new("Radians", "real", "rad", "angle", 1),
            new("Hertz", "real", "Hz", "frequency", 1),
            new("Kilohertz", "real", "kHz", "frequency", 1000),
            new("Megahertz", "real", "MHz", "frequency", 1e6),
            new("Gigahertz", "real", "GHz", "frequency", 1e9),
            new("Newtons", "real", "N", "force", 1),
            new("Kilonewtons", "real", "kN", "force", 1000),
            new("Pascals", "real", "Pa", "pressure", 1),
            new("Kilopascals", "real", "kPa", "pressure", 1000),
            new("Bars", "real", "bar", "pressure", 100000),
            new("Millibars", "real", "mbar", "pressure", 100),
            new("PoundsPerSquareInch", "real", "psi", "pressure", 0.45359237 * 9.80665 / (0.0254 * 0.0254)),
            new("Joules", "real", "J", "energy", 1),
            new("Kilojoules", "real", "kJ", "energy", 1000),
            new("WattHours", "real", "Wh", "energy", 3600),
            new("KilowattHours", "real", "kWh", "energy", 3600000),
            new("Watts", "real", "W", "power", 1),
            new("Kilowatts", "real", "kW", "power", 1000),
            new("Megawatts", "real", "MW", "power", 1e6),
            new("Amperes", "real", "A", "current", 1),
            new("Milliamperes", "real", "mA", "current", 0.001),
            new("Volts", "real", "V", "voltage", 1),
            new("Millivolts", "real", "mV", "voltage", 0.001),
            new("Ohms", "real", "Ω", "resistance", 1),
            new("Bits", "integer", "bit", "data size", 1),
            new("Bytes", "integer", "B", "data size", 8),
            new("Kilobytes", "real", "kB", "data size", 8000),
            new("Megabytes", "real", "MB", "data size", 8e6),
            new("Gigabytes", "real", "GB", "data size", 8e9),
            new("Terabytes", "real", "TB", "data size", 8e12),
            new("Kibibytes", "real", "KiB", "data size", 8 * 1024),
            new("Mebibytes", "real", "MiB", "data size", 8 * 1048576),
            new("Gibibytes", "real", "GiB", "data size", 8.0 * 1073741824),
            new("Tebibytes", "real", "TiB", "data size", 8.0 * 1099511627776),
            new("BitsPerSecond", "real", "bit/s", "data rate", 1),
            new("KilobitsPerSecond", "real", "kbit/s", "data rate", 1000),
            new("MegabitsPerSecond", "real", "Mbit/s", "data rate", 1e6),
            new("GigabitsPerSecond", "real", "Gbit/s", "data rate", 1e9),
            new("BytesPerSecond", "real", "B/s", "data rate", 8)
        });

        public static IReadOnlyList<string> Symbols { get; } = Array.AsReadOnly(
            All.Select(unit => unit.ShortName).OrderByDescending(symbol => symbol.Length).ToArray());

        public static bool IsReservedName(string name) => All.Any(unit => unit.Name == name);
        public static bool IsReservedSymbol(string symbol) => All.Any(unit => unit.ShortName == symbol);
    }
}
