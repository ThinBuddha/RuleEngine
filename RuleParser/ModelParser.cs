using System;
using Lexical;

namespace Rules.Parser
{
    /// <summary>
    /// Parses model declarations into a syntax tree without (yet) constructing model objects.
    /// Currently supports unit and type declarations.
    /// </summary>
    public class ModelParser : Parser
    {
        public ModelParser()
        {
            _parse = Declaration;
            _lookAheads = Token.Keyword;
        }

        protected override Builder CreateBuilder() => null;

        protected override void SetTokens()
        {
            foreach (var keyword in new[]
            {
                "unit", "type", "of", "with", "short", "integer", "real", "string",
                "min", "max", "values"
            })
            {
                _tokenizer.AddKeyword(keyword);
            }
        }

        private void Declaration()
        {
            if (NextTokenIsKeyword("unit", out _)) UnitDeclaration();
            else if (NextTokenIsKeyword("type", out _)) TypeDeclaration();
            else Error("unit or type");
        }

        private void UnitDeclaration()
        {
            _tokenizer.GetNextToken(); // unit
            _parseTree.Begin(ParsedItem.UnitDecl);
            try
            {
                if (!Name() || !Keyword("of")) return;
                if (!OneOfKeywords("integer", "real", out var primitive)) return;

                _parseTree.Add(ParsedItem.BaseType, primitive);

                if (!Keyword("with") || !Keyword("short") || !Comparison("=", out _)) return;
                if (!TokenValue(Token.String, "string literal", out var shortName)) return;

                _parseTree.Add(ParsedItem.Short, shortName);
            }
            finally
            { 
                _parseTree.End();
            }
        }

        private void TypeDeclaration()
        {
            _tokenizer.GetNextToken(); // type
            _parseTree.Begin(ParsedItem.TypeDecl);
            try
            {
                if (!Name() || !Keyword("of")) return;

                if (GetNextTokenIfIsKeyword("string", out _))
                {
                    _parseTree.Add(ParsedItem.BaseType, "string");
                    if (GetNextTokenIfIsKeyword("with", out _)) StringValues();
                }
                else if (TokenValue(Token.Identifier, "unit name or string", out var unit))
                {
                    _parseTree.Add(ParsedItem.BaseType, unit);
                    if (GetNextTokenIfIsKeyword("with", out _)) NumericBounds();
                }
            }
            finally 
            {
                _parseTree.End();
            }
        }

        private void NumericBounds()
        {
            _parseTree.Begin(ParsedItem.Bounds);
            try
            {
                if (GetNextTokenIfIsKeyword("min", out _))
                {
                    Bound(ParsedItem.Min, "=", ">");
                    if (LookAheadHasAnyOf(Token.And))
                    {
                        _tokenizer.GetNextToken();
                        if (Keyword("max")) Bound(ParsedItem.Max, "=", "<");
                    }
                }
                else if (GetNextTokenIfIsKeyword("max", out _))
                {
                    Bound(ParsedItem.Max, "=", "<");
                }
                else Error("min or max");
            }
            finally 
            { 
                _parseTree.End();
            }
        }

        private void Bound(ParsedItem kind, string inclusive, string exclusive)
        {
            _parseTree.Begin(kind);
            try
            {
                if (!Comparison(inclusive, exclusive, out var op)) return;
                _parseTree.Add(ParsedItem.Compare, op);
                if (Number(out var number, out var numberKind))
                    _parseTree.Add(numberKind, number);
            }
            finally
            { 
                _parseTree.End();
            }
        }

        private void StringValues()
        {
            _parseTree.Begin(ParsedItem.Values);
            try
            {
                if (!Keyword("values") || !Comparison("=", out _) ||
                    !TokenValue(Token.LeftSquare, "[", out _)) return;

                if (!StringValue()) return;
                while (LookAheadHasAnyOf(Token.Comma))
                {
                    _tokenizer.GetNextToken();
                    if (!StringValue()) return;
                }
                TokenValue(Token.RightSquare, "]", out _);
            }
            finally
            { 
                _parseTree.End();
            }
        }

        private bool StringValue()
        {
            if (!TokenValue(Token.String, "string literal", out var value)) return false;
            _parseTree.Add(ParsedItem.String, value);
            return true;
        }

        private bool Name()
        {
            if (!TokenValue(Token.Identifier, "identifier", out var value)) return false;
            _parseTree.Add(ParsedItem.Id, value);
            return true;
        }

        private bool Number(out string value, out ParsedItem kind)
        {
            var negative = LookAheadHasAnyOf(Token.Minus);
            if (negative) _tokenizer.GetNextToken();
            var (token, literal) = _tokenizer.GetLookAhead();
            if (token != Token.Integer && token != Token.Float)
            {
                Error("number literal");
                value = null;
                kind = default;
                return false;
            }
            _tokenizer.GetNextToken();
            value = negative ? "-" + literal : literal;
            kind = token == Token.Integer ? ParsedItem.Integer : ParsedItem.Float;
            return true;
        }

        private bool Keyword(string keyword)
        {
            if (GetNextTokenIfIsKeyword(keyword, out _)) return true;
            Error(keyword);
            return false;
        }

        private bool OneOfKeywords(string first, string second, out string value)
        {
            if (GetNextTokenIfIsKeyword(first, out value) ||
                GetNextTokenIfIsKeyword(second, out value)) return true;
            Error($"{first} or {second}");
            value = null;
            return false;
        }

        private bool Comparison(string expected, out string value) => Comparison(expected, null, out value);

        private bool Comparison(string first, string second, out string value)
        {
            var (token, literal) = _tokenizer.GetLookAhead();
            if (token == Token.Comparison && (literal == first || literal == second))
            {
                _tokenizer.GetNextToken();
                value = literal;
                return true;
            }
            Error(second == null ? first : $"{first} or {second}");
            value = null;
            return false;
        }

        private bool TokenValue(Token expectedToken, string expected, out string value)
        {
            var (token, literal) = _tokenizer.GetLookAhead();
            if (token == expectedToken)
            {
                _tokenizer.GetNextToken();
                value = literal;
                return true;
            }
            Error(expected);
            value = null;
            return false;
        }

        private void Error(string expected)
        {
            var (_, literal) = _tokenizer.GetLookAhead();
            SyntaxErrors.Add(Tuple.Create(
                $"Syntax error at {_tokenizer.CurrentPos}: expected {expected} but instead encountered \"{literal}\"",
                _parseTree.ToString()));

            // Keep a following declaration for the outer parse loop to process.
            if (!NextTokenIsKeyword("unit", out _) 
                && !NextTokenIsKeyword("type", out _) &&
                !LookAheadHasAnyOf(Token.EndOfFile))
            {
                _tokenizer.GetNextToken();
            }
        }
    }
}
