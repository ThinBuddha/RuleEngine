using System;
using System.IO;
using Rules.Parser;
using Xunit;

namespace RuleParserTest
{
    public class TestRuleFileHeader
    {
        [Fact]
        public void ParsesParcelDispatcherWithAnEnforcedModelHeader()
        {
            var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Examples",
                "Demo-Parcel-Dispatcher.txt"));
            var parser = new RuleParser(RulePart.RuleContainer);

            parser.Parse(File.ReadAllText(path));

            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal("Demo-Parcel-Dispatcher-Types.txt", parser.ModelFileName);
            Assert.StartsWith("ModelFile(Demo-Parcel-Dispatcher-Types.txt)RuleCont(", parser.ParseTree);
            Assert.Equal(10, Assert.IsType<RulesBuilder>(parser.Builder).RuleContainers.Containers.Count);
        }

        [Fact]
        public void HeaderIsOptionalAndClearedWhenParserIsReused()
        {
            var parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse("ENFORCE MODEL \"types.txt\" RuleContainer First");
            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal("types.txt", parser.ModelFileName);

            parser.Parse("RuleContainer Second");
            Assert.Empty(parser.SyntaxErrors);
            Assert.Null(parser.ModelFileName);
            Assert.Equal("RuleCont(Id(Second))", parser.ParseTree);
        }

        [Theory]
        [InlineData("enforce \"types.txt\" RuleContainer C")]
        [InlineData("enforce model RuleContainer C")]
        [InlineData("enforce model \"a.txt\" enforce model \"b.txt\" RuleContainer C")]
        [InlineData("RuleContainer C enforce model \"types.txt\"")]
        public void RejectsMalformedOrMisplacedHeaders(string source)
        {
            var parser = new RuleParser(RulePart.RuleContainer);

            parser.Parse(source);

            Assert.NotEmpty(parser.SyntaxErrors);
        }
    }
}
