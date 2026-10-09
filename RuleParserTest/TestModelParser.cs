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
        public void ParsesTheParcelDispatcherTypesAndClassesTogether()
        {
            var examples = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Examples"));
            var source = File.ReadAllText(Path.Combine(examples, "Demo-Parcel-Dispatcher-Types.txt")) +
                System.Environment.NewLine +
                File.ReadAllText(Path.Combine(examples, "Demo-Parcel-Dispatcher-Classes.txt"));
            var parser = new ModelParser();

            parser.Parse(source);

            Assert.Empty(parser.SyntaxErrors);
            Assert.Null(parser.Builder);
            Assert.Equal(4, Count(parser.ParseTree, "UnitDecl("));
            Assert.Equal(33, Count(parser.ParseTree, "TypeDecl("));
            Assert.Equal(25, Count(parser.ParseTree, "ClassDecl("));
            Assert.Equal(45, Count(parser.ParseTree, "Property("));
        }

        [Fact]
        public void ParsesTheComputerAssemblyTypesAndClassesTogether()
        {
            var examples = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Examples"));
            var source = File.ReadAllText(Path.Combine(examples, "Demo-Computer-Assembly-Types.txt")) +
                System.Environment.NewLine +
                File.ReadAllText(Path.Combine(examples, "Demo-Computer-Assembly-Classes.txt"));
            var parser = new ModelParser();

            parser.Parse(source);

            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal(2, Count(parser.ParseTree, "UnitDecl("));
            Assert.Equal(24, Count(parser.ParseTree, "TypeDecl("));
            Assert.Equal(14, Count(parser.ParseTree, "ClassDecl("));
            Assert.Equal(26, Count(parser.ParseTree, "Property("));
        }

        [Fact]
        public void ParsesTheMoonbaseClinicTypesAndClassesTogether()
        {
            var examples = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Examples"));
            var source = File.ReadAllText(Path.Combine(examples, "Demo-Moonbase-Clinic-Types.txt")) +
                System.Environment.NewLine +
                File.ReadAllText(Path.Combine(examples, "Demo-Moonbase-Clinic-Classes.txt"));
            var parser = new ModelParser();

            parser.Parse(source);

            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal(1, Count(parser.ParseTree, "UnitDecl("));
            Assert.Equal(19, Count(parser.ParseTree, "TypeDecl("));
            Assert.Equal(19, Count(parser.ParseTree, "ClassDecl("));
            Assert.Equal(26, Count(parser.ParseTree, "Property("));
            Assert.Contains("TypeDecl(Id(Assessment.Moonbloom)BaseType(boolean))", parser.ParseTree);
        }

        [Fact]
        public void ParsesBooleanTypesAndAtomicClassProperties()
        {
            var parser = new ModelParser();

            parser.Parse("type Enabled of boolean class Feature has Named of Enabled Inline of boolean");

            Assert.Empty(parser.SyntaxErrors);
            Assert.Equal("TypeDecl(Id(Enabled)BaseType(boolean))ClassDecl(Id(Feature)Property(Id(Named)TypeRef(Enabled))Property(Id(Inline)AtomicType(boolean)))", parser.ParseTree);
        }

        [Fact]
        public void ParsesTheClassesExampleWithoutBuildingObjects()
        {
            var path = Path.GetFullPath(Path.Combine(System.AppContext.BaseDirectory,
                "..", "..", "..", "..", "RuleParser", "doc", "Further design", "Classes.txt"));
            var parser = new ModelParser();

            parser.Parse(File.ReadAllText(path));

            Assert.Empty(parser.SyntaxErrors);
            Assert.Null(parser.Builder);
            Assert.Equal(3, Count(parser.ParseTree, "ClassDecl("));
            Assert.Equal(5, Count(parser.ParseTree, "Property("));
            Assert.Contains("ClassDecl(Id(Customer)Property(Id(Membership)TypeRef(Customer.Membership))Property(Id(Discount)ClassRef(Discount)))", parser.ParseTree);
            Assert.Contains("ClassDecl(Id(Person)Property(Id(Parent)ClassRef(Person))Property(Id(Child)ClassRef(Person)))", parser.ParseTree);
        }

        [Fact]
        public void ParsesUnitsAndTypesBeforeClassesAndBothReferenceForms()
        {
            var parser = new ModelParser();

            parser.Parse("unit Credits of real with short = \"cr\" " +
                "type Customer.Membership of string " +
                "CLASS Customer HAS Balance of type Credits " +
                "Membership of type Customer.Membership " +
                "Age of integer Weight of type real Name of string " +
                "Parent as class Customer Child as Customer");

            Assert.Empty(parser.SyntaxErrors);
            Assert.StartsWith("UnitDecl(", parser.ParseTree);
            Assert.Contains("TypeDecl(Id(Customer.Membership)BaseType(string))ClassDecl(Id(Customer)", parser.ParseTree);
            Assert.Contains("Property(Id(Balance)TypeRef(Credits))", parser.ParseTree);
            Assert.Contains("Property(Id(Age)AtomicType(integer))", parser.ParseTree);
            Assert.Contains("Property(Id(Weight)AtomicType(real))", parser.ParseTree);
            Assert.Contains("Property(Id(Name)AtomicType(string))", parser.ParseTree);
            Assert.Contains("Property(Id(Parent)ClassRef(Customer))", parser.ParseTree);
        }

        [Theory]
        [InlineData("class Customer has")]
        [InlineData("class Customer.Name has Age of integer")]
        [InlineData("class Customer has Full.Name of string")]
        [InlineData("class Customer has Parent as Other.Person")]
        [InlineData("class Customer has Age of")]
        [InlineData("class Customer has Parent as")]
        [InlineData("class Customer has Age as integer")]
        [InlineData("class Customer has Age of class Person")]
        [InlineData("class Customer has Age of integer unit Credits of real with short = \"cr\"")]
        [InlineData("class Customer has Age of integer type Age of Credits")]
        public void RejectsInvalidClassSyntaxOrDeclarationOrder(string source)
        {
            var parser = new ModelParser();

            parser.Parse(source);

            Assert.NotEmpty(parser.SyntaxErrors);
        }

        [Fact]
        public void ResetsClassOrderingWhenParserIsReused()
        {
            var parser = new ModelParser();
            parser.Parse("class First has Value of integer");
            Assert.Empty(parser.SyntaxErrors);

            parser.Parse("unit Points of integer with short = \"pt\" class Second has Value of Points");

            Assert.Empty(parser.SyntaxErrors);
            Assert.StartsWith("UnitDecl(", parser.ParseTree);
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
        [InlineData("type Enabled of boolean with values = [\"true\"]")]
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
