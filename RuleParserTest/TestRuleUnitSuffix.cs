using System.Linq;
using Rules.Model;
using Rules.Parser;
using Xunit;

namespace RuleParserTest
{
    public class TestRuleUnitSuffix
    {
        private static ExplicitModel Model()
        {
            var parser = new ModelParser();
            parser.Parse("unit Kilogram of real with short = \"kg\" unit Seconds of integer with short = \"sec\" " +
                "type Weight of Kilogram type Duration of Seconds class Item has Weight of Weight Other of Weight Duration of Duration Count of integer Ready of boolean");
            Assert.Empty(parser.SyntaxErrors);
            Assert.Empty(parser.Model.Validate());
            return parser.Model;
        }

        [Theory]
        [InlineData("Item.Weight > 5 kg", "Item.Weight := 2.5 kg")]
        [InlineData("5 kg < Item.Weight", "Item.Weight := -2.5 kg")]
        [InlineData("Item.Ready", "Item.Weight := 0 kg")]
        [InlineData("Item.Ready", "Item.Weight := Item.Other + 1 kg")]
        [InlineData("Item.Ready", "Item.Weight := (Item.Other - 1 kg) * 2")]
        [InlineData("Item.Ready", "Item.Weight := Item.Other / 2")]
        [InlineData("Item.Ready", "Item.Weight := 2 * Item.Other")]
        [InlineData("Item.Count = 2", "Item.Count := 3")]
        public void AcceptsCorrectUnits(string condition, string conclusion)
        {
            Assert.Empty(Validate(condition, conclusion));
        }

        [Theory]
        [InlineData("Item.Weight > 5", "Item.Weight := 2 kg", "kg")]
        [InlineData("5 < Item.Weight", "Item.Weight := 2 kg", "kg")]
        [InlineData("Item.Ready", "Item.Weight := 0", "kg")]
        [InlineData("Item.Ready", "Item.Weight := 2 sec", "Kilogram")]
        [InlineData("Item.Ready", "Item.Weight := 2 KG", "KG")]
        [InlineData("Item.Ready", "Item.Weight := 2 Kilogram", "Kilogram")]
        [InlineData("Item.Ready", "Item.Weight := Item.Other + 1", "units")]
        [InlineData("Item.Ready", "Item.Count := 2 kg", "no unit")]
        [InlineData("Item.Ready", "Item.Duration := 2.5 sec", "integer")]
        public void RejectsMissingOrIncorrectUnits(string condition, string conclusion, string error)
        {
            Assert.Contains(Validate(condition, conclusion), message => message.Contains(error));
        }

        private static System.Collections.Generic.IReadOnlyList<string> Validate(string condition, string conclusion)
        {
            var parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse($"enforce model \"model.txt\" RuleContainer C Rule R If {condition} Then {conclusion}");
            Assert.Empty(parser.SyntaxErrors);
            return new RuleModelValidator(Model()).Validate(((RulesBuilder)parser.Builder).RuleContainers);
        }

        [Theory]
        [InlineData("Item.Weight > 5 kg", "Item.Weight := 2")]
        [InlineData("Item.Ready", "Item.Weight := 2 kg")]
        public void RejectsSuffixesWithoutEnforceModel(string condition, string conclusion)
        {
            var parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse($"RuleContainer C Rule R If {condition} Then {conclusion}");
            Assert.NotEmpty(parser.SyntaxErrors);
        }

        [Fact]
        public void UnitsDoNotChangeNumericExecution()
        {
            var parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse("enforce model \"model.txt\" RuleContainer C Rule R If Item.Ready Then Item.Weight := 2 kg + 3 kg");
            Assert.Empty(parser.SyntaxErrors);
            var expression = ((RulesBuilder)parser.Builder).RuleContainers.Containers.Single().Rules.Single().Conclusions.Single().RightExpr;
            var result = Assert.IsType<Rules.Expression.TConstantNumValue>(expression.Evaluate(new ValueBag()));
            Assert.Equal(5, result.Value.IntValue);
        }
    }
}
