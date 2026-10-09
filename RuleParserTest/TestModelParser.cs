using System.IO;
using Rules.Parser;
using Xunit;

namespace RuleParserTest
{
    public class TestModelParser
    {
        [Fact]
        public void ParsesTheTypesAndUnitsExampleWithoutBuildingObjects()
        {
            var path = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Further design", "Types and units.txt"));
            var source = File.ReadAllText(path);
            var parser = new ModelParser();

            parser.Parse(source);

            Assert.Empty(parser.SyntaxErrors);
            Assert.Null(parser.Builder);
            Assert.Equal(2, Count(parser.ParseTree, "UnitDecl("));
            Assert.Equal(10, Count(parser.ParseTree, "TypeDecl("));
            Assert.Contains("Short(kg)", parser.ParseTree);
            Assert.Contains("Min(Compare(>)Integer(100))", parser.ParseTree);
            Assert.Contains("Max(Compare(<)Integer(10))", parser.ParseTree);
            Assert.Contains("TypeDecl(Id(AnyWeight)BaseType(Kilogram))", parser.ParseTree);
            Assert.Contains("Values(String(relaxed)String(today)String(express))", parser.ParseTree);
        }

        [Fact]
        public void ParsesTheParcelDispatcherTypeDefinitions()
        {
            var path = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Examples",
                "Demo-Parcel-Dispatcher-Types.txt"));
            var parser = new ModelParser();

            parser.Parse(File.ReadAllText(path));

            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal(4, Count(parser.ParseTree, "UnitDecl("));
            Assert.Equal(33, Count(parser.ParseTree, "TypeDecl("));
        }

        [Fact]
        public void ParsesCompactListsNegativeBoundsAndCaseInsensitiveKeywords()
        {
            var parser = new ModelParser();

            parser.Parse("UNIT Celsius OF real WITH short = \"C\" " +
                "TYPE Temperature OF Celsius WITH min > -5.5 and max < 20 " +
                "TYPE Label OF string WITH values = [\"cold\",\"warm\"]");

            Assert.Empty(parser.SyntaxErrors);
            Assert.Contains("Min(Compare(>)Float(-5.5))", parser.ParseTree);
            Assert.Contains("Max(Compare(<)Integer(20))", parser.ParseTree);
            Assert.Contains("Values(String(cold)String(warm))", parser.ParseTree);
        }

        [Theory]
        [InlineData("unit Kilogram of string with short = \"kg\"")]
        [InlineData("unit Kilogram of real with short = 5")]
        [InlineData("type Weight of Kilogram with min < 0")]
        [InlineData("type Weight of Kilogram with max > 10")]
        [InlineData("type Weight of Kilogram with min = 0 max = 10")]
        [InlineData("type Urgency of string with values = []")]
        [InlineData("type Urgency of string with values = [\"today\",]")]
        [InlineData("type Urgency of string with min = 0")]
        public void ReportsSyntaxErrors(string source)
        {
            var parser = new ModelParser();

            parser.Parse(source);

            Assert.NotEmpty(parser.SyntaxErrors);
        }

        [Fact]
        public void CanParseAnotherSourceAfterAnError()
        {
            var parser = new ModelParser();
            parser.Parse("type Urgency of string with values = []");
            Assert.NotEmpty(parser.SyntaxErrors);

            parser.Parse("type Name of string");

            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal("TypeDecl(Id(Name)BaseType(string))", parser.ParseTree);
        }

        [Fact]
        public void ReportsAnErrorAndContinuesAtTheNextDeclaration()
        {
            var parser = new ModelParser();

            parser.Parse("unit Points of integer with short = type Score of Points with max = 5");

            Assert.NotEmpty(parser.SyntaxErrors);
            Assert.Contains("TypeDecl(Id(Score)BaseType(Points)Bounds(Max(Compare(=)Integer(5))))",
                parser.ParseTree);
        }

        private static int Count(string text, string fragment) =>
            (text.Length - text.Replace(fragment, string.Empty).Length) / fragment.Length;
    }
}
