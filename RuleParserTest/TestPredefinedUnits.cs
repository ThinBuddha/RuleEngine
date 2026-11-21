using System.Collections.Generic;
using System.Linq;
using Rules.Model;
using Rules.Parser;
using Xunit;

namespace RuleParserTest
{
    public class TestPredefinedUnits
    {
        public static IEnumerable<object[]> StandardUnits => MetaModelUnits.All
            .Select(unit => new object[] { unit.Name, unit.ShortName, unit.Kind == "integer" ? "2" : "2.5" });

        [Theory]
        [MemberData(nameof(StandardUnits))]
        public void ParsesAndValidatesEveryStandardUnitWithoutADeclaration(string name, string symbol, string value)
        {
            var modelParser = new ModelParser();
            modelParser.Parse($"type Measurement of {name} class Sample has Value of Measurement Ready of boolean");
            Assert.Empty(modelParser.SyntaxErrors);
            Assert.Empty(((ModelBuilder)modelParser.Builder).Errors);
            Assert.Empty(modelParser.Model.Validate());

            var rules = new RuleParser(RulePart.RuleContainer);
            rules.Parse($"enforce model \"model.txt\" RuleContainer C Rule R If Sample.Value > 1 {symbol} Then Sample.Value := ({value} {symbol}+1 {symbol})/2");
            Assert.Empty(rules.SyntaxErrors);
            // Division yields real values; bits and bytes deliberately require whole values.
            if (modelParser.Model.Units[name].Kind == "integer")
                rules.Parse($"enforce model \"model.txt\" RuleContainer C Rule R If Sample.Ready Then Sample.Value := {value} {symbol}");
            Assert.Empty(rules.SyntaxErrors);
            Assert.Empty(new RuleModelValidator(modelParser.Model).Validate(((RulesBuilder)rules.Builder).RuleContainers));
        }

        [Theory]
        [InlineData("unit Kilometers of real with short = \"km\"", "predefined")]
        [InlineData("unit Minutes of integer with short = \"ticks\"", "predefined")]
        [InlineData("unit Percentage of real with short = \"pct\"", "predefined")]
        [InlineData("unit Kilometres of real with short = \"km\"", "reserved")]
        [InlineData("unit Other of real with short = \"%\"", "reserved")]
        public void RejectsRedefinitionsOfStandardNamesAndSymbols(string source, string error)
        {
            var parser = new ModelParser();
            parser.Parse(source);
            Assert.Empty(parser.SyntaxErrors);
            Assert.Contains(((ModelBuilder)parser.Builder).Errors, message => message.Contains(error));
            Assert.Equal(new ModelUnit("Minutes", "real", "min"), parser.Model.Units["Minutes"]);
        }

        [Fact]
        public void EachParseHasItsOwnUnitDictionaryAndKeepsCustomUnits()
        {
            var parser = new ModelParser();
            parser.Parse("unit Points of integer with short = \"points\" type Score of Points");
            var first = parser.Model;
            Assert.Empty(((ModelBuilder)parser.Builder).Errors);
            Assert.Empty(first.Validate());

            parser.Parse("type Distance of Kilometers");
            Assert.NotSame(first.Units, parser.Model.Units);
            Assert.DoesNotContain("Points", parser.Model.Units.Keys);
            Assert.Contains("Points", first.Units.Keys);
            Assert.Empty(parser.Model.Validate());
        }

        [Theory]
        [InlineData("Kilometres")]
        [InlineData("kilometers")]
        public void RequiresCanonicalUnitNames(string name)
        {
            var parser = new ModelParser();
            parser.Parse($"type Distance of {name}");
            Assert.Contains(parser.Model.Validate(), error => error.Contains("unknown unit"));
        }

        [Fact]
        public void SupportsCommentsBeforeCompoundAndSymbolSuffixes()
        {
            var model = new ModelParser();
            model.Parse("type Speed of MetersPerSecond class Sample has Speed of Speed Ready of boolean");
            var rules = new RuleParser(RulePart.RuleContainer);
            rules.Parse("enforce model \"model.txt\" RuleContainer C Rule R If Sample.Ready Then Sample.Speed := 2 /* measurement */ m/s + 3 // measurement\n m/s");
            Assert.Empty(rules.SyntaxErrors);
            Assert.Empty(new RuleModelValidator(model.Model).Validate(((RulesBuilder)rules.Builder).RuleContainers));
        }

        [Fact]
        public void DoesNotConvertDifferentUnitsImplicitly()
        {
            var model = new ModelParser();
            model.Parse("type Distance of Kilometers class Sample has Distance of Distance Ready of boolean");
            var rules = new RuleParser(RulePart.RuleContainer);
            rules.Parse("enforce model \"model.txt\" RuleContainer C Rule R If Sample.Ready Then Sample.Distance := 1000 m");
            Assert.Empty(rules.SyntaxErrors);
            Assert.Contains(new RuleModelValidator(model.Model).Validate(((RulesBuilder)rules.Builder).RuleContainers),
                error => error.Contains("expected 'Kilometers'"));
        }
    }
}
