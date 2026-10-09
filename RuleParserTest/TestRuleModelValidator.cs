using System;
using System.IO;
using Rules.Parser;
using Xunit;

namespace RuleParserTest
{
    public class TestRuleModelValidator
    {
        [Theory]
        [InlineData("Demo-Parcel-Dispatcher")]
        [InlineData("Demo-Computer-Assembly")]
        [InlineData("Demo-Moonbase-Clinic")]
        public void DemoRulesMapToTheirModels(string name)
        {
            var examples = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RuleParser", "doc", "Examples"));
            var modelParser = new ModelParser();
            modelParser.Parse(File.ReadAllText(Path.Combine(examples, name + "-model.txt")));
            Assert.Empty(modelParser.SyntaxErrors);
            var model = ((ModelBuilder)modelParser.Builder).Model;
            Assert.Empty(model.Validate());
            var ruleParser = new RuleParser(RulePart.RuleContainer);
            ruleParser.Parse(File.ReadAllText(Path.Combine(examples, name + ".txt")));
            Assert.Empty(ruleParser.SyntaxErrors);
            Assert.Empty(new RuleModelValidator(model).Validate(((RulesBuilder)ruleParser.Builder).RuleContainers));
        }

        [Fact]
        public void ReportsUnknownIdsAndInvalidExpressions()
        {
            var modelParser = new ModelParser();
            modelParser.Parse("unit Points of integer with short = \"pt\" type Score of Points with min = 0 and max < 10 type Status of string with values = [\"ok\"] class Sample has Score of Score Status of Status Enabled of boolean");
            var model = ((ModelBuilder)modelParser.Builder).Model;
            Assert.Empty(modelParser.SyntaxErrors);
            Assert.Empty(model.Validate());
            var ruleParser = new RuleParser(RulePart.RuleContainer);
            ruleParser.Parse("RuleContainer C Rule R If Sample.Unknown = 1 and Sample.Score = 10 and Sample.Enabled then Sample.Status := \"bad\" and Sample.Score := \"wrong\"");
            Assert.Empty(ruleParser.SyntaxErrors);
            var errors = new RuleModelValidator(model).Validate(((RulesBuilder)ruleParser.Builder).RuleContainers);
            Assert.Contains(errors, error => error.Contains("Sample.Unknown"));
            Assert.Contains(errors, error => error.Contains("outside type 'Score'"));
            Assert.Contains(errors, error => error.Contains("outside type 'Status'"));
            Assert.Contains(errors, error => error.Contains("expected numeric"));
        }

        [Fact]
        public void ReportsInvalidModelReferencesAndBounds()
        {
            var parser = new ModelParser();
            parser.Parse("unit Points of integer with short = \"pt\" type Empty of Points with min > 1 and max < 2 type Missing of Unknown class Sample has Score of Missing Child as UnknownClass");
            Assert.Empty(parser.SyntaxErrors);
            var errors = parser.Model.Validate();
            Assert.Contains(errors, error => error.Contains("no permitted integer values"));
            Assert.Contains(errors, error => error.Contains("unknown unit 'Unknown'"));
            Assert.Contains(errors, error => error.Contains("unknown class 'UnknownClass'"));
        }

        [Fact]
        public void KeywordsAreCaseInsensitiveInCompiledModel()
        {
            var parser = new ModelParser();
            parser.Parse("UNIT Points OF INTEGER WITH SHORT = \"pt\" TYPE Score OF Points CLASS Sample HAS Amount OF Score Flag OF BOOLEAN");
            Assert.Empty(parser.SyntaxErrors);
            Assert.Empty(parser.Model.Validate());
            Assert.Equal("integer", parser.Model.Units["Points"].Kind);
            Assert.Equal("boolean", parser.Model.Resolve("Sample.Flag").Kind);
        }
    }
}
