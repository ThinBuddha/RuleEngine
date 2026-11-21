using System;
using System.Linq;
using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.Rules;
using Xunit;

namespace RuleParserTest
{
    public class TestUnitConversions
    {
        private static (RuleModelValidator Validator, RuleContainers Rules) Parse(string declarations, string condition, string conclusion)
        {
            var model = new ModelParser();
            model.Parse(declarations);
            Assert.Empty(model.SyntaxErrors);
            Assert.Empty(((ModelBuilder)model.Builder).Errors);
            Assert.Empty(model.Model.Validate());
            var parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse($"enforce model \"units.txt\" RuleContainer C Rule R If {condition} Then {conclusion}");
            Assert.Empty(parser.SyntaxErrors);
            return (new RuleModelValidator(model.Model), ((RulesBuilder)parser.Builder).RuleContainers);
        }

        private const string Lengths = "type Small of Millimeters type Large of Centimeters " +
            "class Sample has Small of Small Large of Large Other of Large Ready of boolean";

        private static double Number(TConstantValue value)
        {
            var number = Assert.IsType<TConstantNumValue>(value);
            return number.Value.ValueType == TConstantNumValue.IntType ? number.Value.IntValue : number.Value.DoubleValue;
        }

        [Theory]
        [InlineData("Millimeters", "Centimeters", 25, 2.5)]
        [InlineData("Centimeters", "Millimeters", 2.5, 25)]
        [InlineData("Millimeters", "Kilometers", 1000000, 1)]
        [InlineData("Inches", "Millimeters", 1, 25.4)]
        [InlineData("SquareCentimeters", "SquareMeters", 10000, 1)]
        [InlineData("Liters", "CubicCentimeters", 1, 1000)]
        [InlineData("Pounds", "Kilograms", 1, 0.45359237)]
        [InlineData("Minutes", "Seconds", 1.5, 90)]
        [InlineData("KilometersPerHour", "MetersPerSecond", 36, 10)]
        [InlineData("KilowattHours", "Joules", 1, 3600000)]
        [InlineData("Bars", "Pascals", 1, 100000)]
        [InlineData("Percentage", "PerMille", 25, 250)]
        [InlineData("Kibibytes", "Bytes", 1, 1024)]
        [InlineData("Kilobytes", "Bytes", 1, 1000)]
        [InlineData("BytesPerSecond", "BitsPerSecond", 1, 8)]
        [InlineData("DegreesFahrenheit", "DegreesCelsius", 32, 0)]
        [InlineData("DegreesCelsius", "DegreesFahrenheit", 100, 212)]
        [InlineData("Kelvin", "DegreesFahrenheit", 273.15, 32)]
        public void ConvertsCatalogRelationshipsInBothDirections(string source, string target, double value, double expected)
        {
            var units = new ExplicitModel().Units;
            Assert.True(units[source].TryGetConversionTo(units[target], out var forward));
            Assert.Equal(expected, forward.Convert(value), 9);
            Assert.True(units[target].TryGetConversionTo(units[source], out var reverse));
            Assert.Equal(value, reverse.Convert(expected), 9);
        }

        [Theory]
        [InlineData("Sample.Large = 10 mm", "Sample.Small := 2 cm", 20)]
        [InlineData("10 mm = Sample.Large", "Sample.Small := 2 cm", 20)]
        [InlineData("Sample.Small = Sample.Large", "Sample.Small := Sample.Large", 10)]
        [InlineData("Sample.Ready", "Sample.Small := (1 cm + 5 mm) * 2", 30)]
        [InlineData("Sample.Ready", "Sample.Small := 5 mm + 1 cm - 2 mm", 13)]
        [InlineData("Sample.Ready", "Sample.Small := (1 cm / 2) + 5 mm", 10)]
        [InlineData("Sample.Ready", "Sample.Small := 2 * (Sample.Large + 5 mm)", 30)]
        [InlineData("Sample.Ready", "Sample.Small := -Sample.Large", -10)]
        [InlineData("Sample.Ready", "Sample.Small := -(Sample.Large + 5 mm)", -15)]
        public void ExecutesConvertedComparisonsAssignmentsAndArithmetic(string condition, string conclusion, double expected)
        {
            var (validator, rules) = Parse(Lengths, condition, conclusion);
            Assert.Empty(validator.Validate(rules));
            Assert.Single(validator.Warnings);
            var context = new ValueBag();
            context.AddOrUpdateValue("Sample.Small", new TConstantNumValue(10));
            context.AddOrUpdateValue("Sample.Large", new TConstantNumValue(1));
            context.AddOrUpdateValue("Sample.Ready", new TConstantNumValue(true));
            var rule = rules.Containers.Single().Rules.Single();
            Assert.True(rule.Conditions.Single().Evaluate(context));
            rule.Conclusions.Single().Evaluate(context);
            Assert.Equal(expected, Number(context.GetValue("Sample.Small")), 10);
            Assert.Equal(1, Number(context.GetValue("Sample.Large")));
        }

        [Fact]
        public void ConvertsLargeValuesWithoutOverflowingTheReferenceValue()
        {
            var units = new ExplicitModel().Units;
            Assert.True(units["Terabytes"].TryGetConversionTo(units["Gigabytes"], out var conversion));
            var actual = conversion.Convert(1e300);
            Assert.True(double.IsFinite(actual));
            Assert.InRange(actual / 1e303, 0.999999999999, 1.000000000001);
        }

        [Fact]
        public void ConvertsNegatedFractionalPropertyValues()
        {
            var (validator, rules) = Parse(Lengths, "Sample.Ready", "Sample.Small := -Sample.Large");
            Assert.Empty(validator.Validate(rules));
            var context = new ValueBag();
            context.AddOrUpdateValue("Sample.Large", new TConstantNumValue(1.25));
            Assert.Equal(-12.5, Number(rules.Containers.Single().Rules.Single().Conclusions.Single().RightExpr.Evaluate(context)), 10);
            Assert.Equal(1.25, Number(context.GetValue("Sample.Large")));
        }

        [Fact]
        public void ValidationCanBeRepeatedWithoutApplyingConversionsTwice()
        {
            var (validator, rules) = Parse(Lengths, "Sample.Ready", "Sample.Small := 1 cm + 5 mm");
            Assert.Empty(validator.Validate(rules));
            var warning = Assert.Single(validator.Warnings);
            Assert.Contains("1 cm + 5 mm", rules.Containers.Single().Rules.Single().Conclusions.Single().ToString());
            Assert.Empty(validator.Validate(rules));
            Assert.Equal(warning, Assert.Single(validator.Warnings));
            Assert.Equal(15, Number(rules.Containers.Single().Rules.Single().Conclusions.Single().RightExpr.Evaluate(new ValueBag())), 10);
        }

        [Theory]
        [InlineData("Sample.Large > 60 mm", "Sample.Large := 1 cm")]
        [InlineData("60 mm < Sample.Large", "Sample.Large := 1 cm")]
        [InlineData("Sample.Ready", "Sample.Large := 60 mm")]
        public void ChecksLiteralBoundsInThePropertyUnit(string condition, string conclusion)
        {
            var (validator, rules) = Parse("type Length of Centimeters with min = 0 and max = 5 class Sample has Large of Length Ready of boolean", condition, conclusion);
            Assert.Contains(validator.Validate(rules), error => error.Contains("outside type 'Length'"));
        }

        [Theory]
        [InlineData("Sample.Ready", "Sample.Small := 1 min")]
        [InlineData("Sample.Small > 1 s", "Sample.Small := 1 mm")]
        [InlineData("Sample.Ready", "Sample.Small := 1 cm + 1 s")]
        [InlineData("Sample.Ready", "Sample.Small := 1 cm * 1 mm")]
        [InlineData("Sample.Ready", "Sample.Small := 1 cm / 1 mm")]
        [InlineData("Sample.Ready", "Sample.Small := 1")]
        public void RejectsUnrelatedUnitsAndUnsupportedDimensionalArithmetic(string condition, string conclusion)
        {
            var (validator, rules) = Parse(Lengths, condition, conclusion);
            Assert.NotEmpty(validator.Validate(rules));
        }

        [Fact]
        public void ReportsOneWarningForMixedUnitsAcrossRulesAndResetsItForAnotherModel()
        {
            var (validator, rules) = Parse(Lengths, "Sample.Ready", "Sample.Small := 1 mm and Sample.Large := 1 cm");
            Assert.Empty(validator.Validate(rules));
            Assert.Contains("cm, mm", Assert.Single(rules.ValidationWarnings));

            var (_, sameUnits) = Parse(Lengths, "Sample.Ready", "Sample.Small := 1 mm");
            Assert.Empty(validator.Validate(sameUnits));
            Assert.Empty(validator.Warnings);
            Assert.Empty(sameUnits.ValidationWarnings);
        }

        [Fact]
        public void CustomUnitsDoNotBecomeConvertibleBecauseTheyShareANumericKind()
        {
            var (validator, rules) = Parse("unit Points of real with short = \"points\" type Score of Points class Sample has Score of Score Ready of boolean",
                "Sample.Ready", "Sample.Score := 1 cm");
            Assert.NotEmpty(validator.Validate(rules));
        }

        [Fact]
        public void IntegerConversionsRejectFractionalLiteralsAndRuntimeValues()
        {
            const string model = "type Size of Bytes type Bits of Bits class Sample has Size of Size Bits of Bits Ready of boolean";
            var (validator, rules) = Parse(model, "Sample.Ready", "Sample.Size := 1 bit");
            Assert.Contains(validator.Validate(rules), error => error.Contains("integer"));
            (validator, rules) = Parse(model, "Sample.Ready", "Sample.Size := Sample.Bits");
            Assert.Empty(validator.Validate(rules));
            var context = new ValueBag();
            context.AddOrUpdateValue("Sample.Bits", new TConstantNumValue(1));
            var expression = rules.Containers.Single().Rules.Single().Conclusions.Single().RightExpr;
            Assert.Throws<TExprException>(() => expression.Evaluate(context));
            context.AddOrUpdateValue("Sample.Bits", new TConstantNumValue(16));
            Assert.Equal(2, Number(expression.Evaluate(context)));
        }

        [Theory]
        [InlineData("0 °C = 32 °F")]
        [InlineData("32 °F = 273.15 K")]
        [InlineData("100 °C = 212 °F")]
        public void ConvertsTemperatureOffsetsDuringComparison(string condition)
        {
            var (validator, rules) = Parse("class Sample has Ready of boolean", condition, "Sample.Ready");
            Assert.Empty(validator.Validate(rules));
            Assert.True(rules.Containers.Single().Rules.Single().Conditions.Single().Evaluate(new ValueBag()));
        }
    }
}
