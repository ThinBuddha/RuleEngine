using Lexical;
using Xunit;

namespace TokenizerTest
{
    public class TestTokenizer
    {
        [Fact]
        public void TestGetNextToken_EmptyInput()
        {
            string input = string.Empty;
            try
            {
                Tokenizer tokenizer = new Tokenizer(input);
                Assert.True(false, "Expected exception due to empty input");
            }
            catch
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void TestGetNextToken_Identifier()
        {
            string input = "_justOneIdentifier";
            (Token token, string literal) = new Tokenizer(input).GetNextToken();
            Assert.True(token == Token.Identifier, "token should be Identifier");
            Assert.True(literal == input, "literal should be equal to input");
        }

        [Fact]
        public void TestGetNextToken_Integer()
        {
            string input = "12345";
            (Token token, string literal) = new Tokenizer(input).GetNextToken();
            Assert.True(token == Token.Integer, "token should be Integer");
            Assert.True(literal == input, "literal should be equal to input");
        }

        [Fact]
        public void TestGetNextToken_Float()
        {
            string input = "1234.56";
            (Token token, string literal) = new Tokenizer(input).GetNextToken();
            Assert.True(token == Token.Float, "token should be Float");
            Assert.True(literal == input, "literal should be equal to input");
        }

        [Fact]
        public void TestGetNextToken_Float_Emin()
        {
            string input = "1234.5E-18";
            (Token token, string literal) = new Tokenizer(input).GetNextToken();
            Assert.True(token == Token.Float, "token should be Float");
            Assert.True(literal == input, "literal should be equal to input");
        }

        [Fact]
        public void TestGetNextToken_Float_Eplus()
        {
            string input = "1234.5E+18";
            (Token token, string literal) = new Tokenizer(input).GetNextToken();
            Assert.True(token == Token.Float, "token should be Float");
            Assert.True(literal == input, "literal should be equal to input");
        }

        [Theory]
        [InlineData("\"1234.5\"", "1234.5")]
        [InlineData("   \"1234.5\"  someMore Text", "1234.5")]
        [InlineData(" \"1234.5\" is a string", "1234.5")]
        [InlineData("\"123\"\"4.5\"", "123")]
        public void TestGetNextToken_String(string input, string literalCheck)
        {
            (Token token, string literal) = new Tokenizer(input).GetNextToken();
            Assert.True(token == Token.String, "token should be String");
            Assert.True(literal == literalCheck, "literal should be equal to input");
        }

        [Theory]
        [InlineData("This is a test", 1, "This", Token.Identifier)]
        [InlineData("12345 is a number", 1, "12345", Token.Integer)]
        [InlineData("12345.089 is a floating point number", 1, "12345.089", Token.Float)]
        [InlineData("This is a test", 2, "is", Token.Identifier)]
        [InlineData("12345 is a number", 2, "is", Token.Identifier)]
        [InlineData("12345.089 is a floating point number", 2, "is", Token.Identifier)]
        [InlineData("12345.089 is a floating point number", 6, "number", Token.Identifier)]
        public void TestGetToken(string input, int seqNr, string literalCheck, Token tokenCheck)
        {
            Tokenizer tokenizer = new Tokenizer(input);
            Token token = Token.None;
            string literal = string.Empty;
            for (int i = 0; i < seqNr; i++)
            {
                (token, literal) = tokenizer.GetNextToken();
            }
            Assert.True(token == tokenCheck, "unexpected token");
            Assert.True(literal == literalCheck, "unexpected literal");
        }

        [Theory]
        [InlineData("B = C * 5", 1, "B", Token.Identifier)]
        [InlineData("B = C * 5", 2, "=", Token.Comparison)]
        [InlineData("B = C * 5", 3, "C", Token.Identifier)]
        [InlineData("B = C * 5", 4, "*", Token.Multiply)]
        [InlineData("B = C * 5", 5, "5", Token.Integer)]
        public void TestGetTokenExt(string input, int seqNr, string literalCheck, Token tokenCheck)
        {
            Tokenizer tokenizer = new Tokenizer(input);
            Token token = Token.None;
            string literal = string.Empty;
            for (int i = 0; i < seqNr; i++)
            {
                (token, literal) = tokenizer.GetNextToken();
            }
            Assert.True(token == tokenCheck, "unexpected token");
            Assert.True(literal == literalCheck, "unexpected literal");
        }

        // You can use [ClassData(typeof(MyTestDataGenerator))] to get rid of InlineData clutter
        [Theory]
        [InlineData("Surface = Length * Height", 1, "Surface", Token.Identifier)]
        [InlineData("Surface = Length * Height", 2, "=", Token.Comparison)]
        [InlineData("Surface = Length * Height", 3, "Length", Token.Identifier)]
        [InlineData("Surface = Length * Height", 4, "*", Token.Multiply)]
        [InlineData("Surface = Length * Height", 5, "Height", Token.Identifier)]
        public void TestGetTokenExt2(string input, int seqNr, string literalCheck, Token tokenCheck)
        {
            Tokenizer tokenizer = new Tokenizer(input);

            Token token = Token.None;
            string literal = string.Empty;
            for (int i = 0; i < seqNr; i++)
            {
                (token, literal) = tokenizer.GetNextToken();
            }
            Assert.True(token == tokenCheck, "unexpected token");
            Assert.True(literal == literalCheck, "unexpected literal");
        }

        // TODO: rewrite to [Theory] using [ClassData]
        [Fact]
        public void TestGetTokenExt3()
        {
            Tokenizer tokenizer = new Tokenizer("if this is true then that becomes false");
            tokenizer.SetLiteralToken("if", Token.Keyword);
            tokenizer.SetLiteralToken("this", Token.Ordinal, "this");
            tokenizer.SetLiteralToken("is", Token.Comparison); // Override default equals symbol
            tokenizer.SetLiteralToken("true", Token.Ordinal, "true");
            tokenizer.SetLiteralToken("then", Token.Keyword);
            tokenizer.SetLiteralToken("that", Token.Ordinal, "that");
            tokenizer.SetLiteralToken("becomes", Token.Assign); // Override default assignment symbol
            tokenizer.SetLiteralToken("false", Token.Ordinal, "false");

            Token token;
            string literal;
            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Keyword, "if");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Ordinal, "this");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Comparison, "is");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Ordinal, "true");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Keyword, "then");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Ordinal, "that");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Assign, "becomes");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Ordinal, "false");
        }

        // TODO: rewrite to [Theory] using [ClassData]
        [Fact]
        public void TestGetTokenExt4()
        {
            Tokenizer tokenizer = new Tokenizer("(3 + 5) * 8 - 12");

            Token token;
            string literal;
            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.LeftHook, "(");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Integer, "3");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Plus, "+");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Integer, "5");

            (token, literal) = tokenizer.GetNextToken();
            CheckTokenData(token, literal, Token.RightHook, ")");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Multiply, "*");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Integer, "8");

            (token, literal) = tokenizer.GetNextToken();
            CheckTokenData(token, literal, Token.Minus, "-");

            (token, literal) = tokenizer.GetNextToken(); ;
            CheckTokenData(token, literal, Token.Integer, "12");

            (token, literal) = tokenizer.GetNextToken();
            CheckTokenData(token, literal, Token.EndOfFile, string.Empty);
        }

        [Fact]
        public void TestGetNextToken_NegativeInteger1()
        {
            string input = "-39";
            Tokenizer tokenizer = new Tokenizer(input);
            (Token token, string literal) = tokenizer.GetNextToken();
            Assert.True(token == Token.Minus, "token should be MathOperator");
            Assert.True(literal == "-", "literal should be equal to -");

            (token, literal) = tokenizer.GetNextToken();
            Assert.True(token == Token.Integer, "token should be Integer");
            Assert.True(literal == "39", "literal should be equal to 39");
        }

        [Fact]
        public void TestGetNextToken_NegativeInteger2()
        {
            string input = "lenght * -39";
            Tokenizer tokenizer = new Tokenizer(input);
            tokenizer.GetNextToken();
            tokenizer.GetNextToken();
            tokenizer.GetNextToken();
            (Token token, string literal) = tokenizer.GetNextToken();
            Assert.True(token == Token.Integer, "token should be Integer");
            Assert.True(literal == "39", "literal should be equal to 39");
        }

        private static void CheckTokenData(Token token, string literal, Token tokenCheck, string literalCheck)
        {
            Assert.True(token == tokenCheck, "unexpected token");
            Assert.True(literal == literalCheck, "unexpected literal");
        }
    }
}
