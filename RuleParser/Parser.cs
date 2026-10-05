using Lexical;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rules.Parser
{
    /// <summary>
    /// Base class for a left associative parser. 
    /// All generic logic goes here so that specific parsers can have only syntax rules as methods.
    /// </summary>
    abstract public class Parser
    {
        protected Tokenizer _tokenizer;
        protected readonly ParseTree _parseTree = new ParseTree();
        protected int _syntaxErrorCount;

        protected delegate void ParseDelegate();
        protected ParseDelegate _parse;

        protected Enum _lookAheads;

        public string ParseTree => _parseTree.ToString();
        public List<Tuple<string, string>> SyntaxErrors { get; } = new List<Tuple<string, string>>();
        public Builder Builder { get; internal set; }

        public void Parse(string input)
        {
            _syntaxErrorCount = 0;
            _parseTree.Clear();
            SyntaxErrors.Clear();

            Builder = CreateBuilder();

            _tokenizer = new Tokenizer(input);
            SetTokens();
            while (true)
            {
                (Token lookAhead, _) = _tokenizer.GetLookAhead();

                if (lookAhead == Token.EndOfFile) break;

                if (LookAheadHasAnyOf(_lookAheads))
                {
                    _parse();
                }
                else
                {
                    // Syntax error and recover, or else infinite loop
                    (_, string literal) = _tokenizer.GetLookAhead(); 
                    AddSyntaxError(_lookAheads.ToString(), literal);
                    _syntaxErrorCount++;
                    if (_syntaxErrorCount > 10)
                    {
                        SyntaxErrors.Add(new Tuple<string, string>("Too many errors", _parseTree.ToString()));
                        break;
                    }
                }
            }
        }

        protected abstract Builder CreateBuilder();

        protected abstract void SetTokens();

        #region ParseTree
        public string ExpandParseTree(string parseTree)
        {
            return _parseTree.Expand();
        }
        #endregion

        #region LookAhead convenience methods
        protected bool LookAheadHasAnyOf(Enum tokens)
        {
            (Token token, _) = _tokenizer.GetLookAhead();
            return tokens.HasFlag(token);
        }

        protected bool LookAheadHasAnyOf(Enum tokens, out string literal)
        {
            (Token token, string myLiteral) = _tokenizer.GetLookAhead();
            literal = myLiteral;
            return tokens.HasFlag(token);
        }

        protected bool NextTokenIsKeyword(string keyword, out string literal)
        {
            return LookAheadHasAnyOf(Token.Keyword, out literal) &&
                string.Compare(literal, keyword, StringComparison.OrdinalIgnoreCase) == 0;
        }

        protected bool GetNextTokenIfIsKeyword(string keyword, out string literal)
        {
            bool result = NextTokenIsKeyword(keyword, out literal);
            if (result)
            {
                _tokenizer.GetNextToken();
            }
            return result;
        }

        /// <summary>
        /// Check if the lookahead is any of the given tokens and filter on literals and if so get it as the next token. 
        /// Only works for tokens that refer to one character, e.g. '+' 
        /// </summary>
        /// <param name="tokens">Filter that contains the required token(s)</param>
        /// <param name="literals">Filter that contains one or more characters, e.g. "+-"</param>
        /// <param name="literal">The literal that was read as the next token</param>
        /// <returns>True if the next token is according to the filters</returns>
        protected bool GetNextTokenIfAnyOfChar(Enum tokens, string literals, out string literal)
        {
            (Token tryToken, string tryLiteral) = _tokenizer.GetLookAhead();
            if (tokens.HasFlag(tryToken) && literals.Contains(tryLiteral))
            {
                (_, literal) = _tokenizer.GetNextToken();
                return true;
            }
            literal = string.Empty;
            return false;
        }

        protected bool GetNextTokenIfAnyOfChar(Enum tokens, string literals)
        {
            return GetNextTokenIfAnyOfChar(tokens, literals, out string _);
        }
        #endregion

        #region SyntaxError
        protected void AddSyntaxError(string msg, string literal)
        {
            SyntaxErrors.Add(new Tuple<string, string>(
                $"Syntax error at {_tokenizer.CurrentPos}: expected {msg} but instead encountered \"{literal}\"",
                _parseTree.ToString()));

            _tokenizer.GetNextToken(); // try to recover, so we can carry on
        }

        protected void AddSyntaxError(Token token, string literal)
        {
            AddSyntaxError(token.ToString(), literal);
        }
        protected void AddSyntaxError(Enum tokens, string literal)
        {
            AddSyntaxError(tokens.ToString(), literal);
        }

        protected void AddSyntaxError(string msg)
        {
            SyntaxErrors.Add(new Tuple<string, string>(
                $"Syntax error at {_tokenizer.CurrentPos}: expected {msg}",
                _parseTree.ToString()));

            _tokenizer.GetNextToken(); // try to recover, so we can carry on
        }
        #endregion
    }
}
