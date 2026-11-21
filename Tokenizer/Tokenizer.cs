using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lexical
{
    [Flags]
    public enum Token
    {
        None = 0,
        Identifier = 1,
        Integer = 2,
        Float = 4,
        String = 8,
        And = 16,
        Comparison = 32,
        Assign = 64,
        Keyword = 128,
        Ordinal = 256,
        LeftHook = 1024,
        RightHook = 2048,
        EndOfFile = 4096, 
        IllegalChar = 8192, 
        Not = 16384, 
        Minus = 32768, 
        Plus = 65536, 
        Multiply = 131072, 
        Divide = 262144,
        LeftSquare = 524288,
        RightSquare = 1048576,
        Comma = 2097152
    }

    /// <summary>
    /// Recognizes operators, numbers, assignents, identifiers (including '.'), numbers (including E-notation), keywords and custom regExprs
    /// </summary>
    /// <remarks>
    /// This is not as fast as a dedicated state machine, but it is human readable code instead.
    /// It is also not fully generic but specifically build for the RuleEngine project.
    /// Literals that are recognized by regEx can only be tokenized if they are surrounded by white space or delimiters (like +, -, *).
    /// Literals with hardcoded recognition (like the math operators +,-,*,/,^ and √) don't need white space or delimiters.
    /// </remarks>
    public class Tokenizer
    {
        private enum ParseMode { None, InlineComment, BlockComment, Literal, String };

        private ParseMode _parseMode = ParseMode.None;
        private int _literalStartPos;
        private int _EOFIndex;
        private int _position;

        private readonly Regex _regExInteger = new Regex(@"^-?[0-9]+$", RegexOptions.Compiled);
        private readonly Regex _regExFloat = new Regex(@"^-?[0-9]+\.[0-9]+([eE][-+]?[0-9]+)?$", RegexOptions.Compiled); 
        private readonly Regex _regExIdentifier = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z0-9_]+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HashSet<string> _assignOps = new HashSet<string>();
        private readonly HashSet<string> _comparisonOps = new HashSet<string>();
        private readonly HashSet<string> _keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _overrideAssign = false;
        private bool _overrideComparison = false;

        private readonly List<Tuple<Regex, string>> _customRegExs = new List<Tuple<Regex, string>>();

        public int Line { get; private set; }

        public string CurrentPos { get { return GetCurrentPos();  } }

        public readonly string Input;

        public Tokenizer(string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input)) throw new Exception("input is empty");
            
            _position = 0;
            Line = 0;
            Input = input + ' '; // Add whitespace at the end to simplify GetLiteral()
            _EOFIndex = Input.Length - 1;

            SetLiteralToken("=", Token.Comparison);
            SetLiteralToken(">", Token.Comparison);
            SetLiteralToken("<", Token.Comparison);
            SetLiteralToken(">=", Token.Comparison);
            SetLiteralToken("<=", Token.Comparison);
            SetLiteralToken("<>", Token.Comparison);

            SetLiteralToken(":=", Token.Assign);
        }

        public (Token, string) GetNextToken()
        {
            Token token = Token.None;
            string literal = string.Empty;
            _parseMode = ParseMode.None;
            char c = '\x00';

            // Anything that is explicitly checked here can be recognized without surrounding whitespace
            // anything else (patterns in RegEx or HashSet) needs surrounding whitespace or delimiters
            while (!IsEndOfFile())
            {
                c = Input[_position];

                if (c == '\n' || c == '\r')
                {
                    if (c == '\n' || !IsEndOfFile() && Input[_position + 1] != '\n')
                    {
                        Line++;
                    }
                }
                else if (char.IsControl(c) && c != '\t')
                {
                    token = Token.IllegalChar;
                    return (token, string.Empty);
                }

                if (c == '(' && _parseMode == ParseMode.None)
                {
                    token = Token.LeftHook;
                    literal = "(";
                }
                else if (c == ')' && _parseMode == ParseMode.None)
                {
                    token = Token.RightHook;
                    literal = ")";
                }
                else if (c == '[' && _parseMode == ParseMode.None)
                {
                    token = Token.LeftSquare;
                    literal = "[";
                }
                else if (c == ']' && _parseMode == ParseMode.None)
                {
                    token = Token.RightSquare;
                    literal = "]";
                }
                else if (c == ',' && _parseMode == ParseMode.None)
                {
                    token = Token.Comma;
                    literal = ",";
                }
                if (c == '/' && PeekNextChar() == '*' && _parseMode == ParseMode.None)
                {
                    _parseMode = ParseMode.BlockComment;
                }
                else if (c == '/' && PeekNextChar() == '/' && _parseMode == ParseMode.None)
                {
                    _parseMode = ParseMode.InlineComment;
                }
                else if (c == '-' && _parseMode == ParseMode.None) 
                {
                    (token, literal) = TryExpandToFloat(ref _position, ref _literalStartPos);
                    if (token == Token.Float)
                    {
                        return (token, literal);
                    }
                    else
                    {
                        token = Token.Minus;
                        literal = c.ToString();
                    }
                }
                else if (c == '+' && _parseMode == ParseMode.None)
                {
                    (token, literal) = TryExpandToFloat(ref _position, ref _literalStartPos);
                    if (token == Token.Float)
                    {
                        return (token, literal);
                    }
                    else
                    {
                        token = Token.Plus;
                        literal = c.ToString();
                    }
                }
                else if (c == '*' && _parseMode == ParseMode.None)
                {
                    token = Token.Multiply;
                    literal = c.ToString();
                }
                else if (c == '/' && _parseMode == ParseMode.None)
                {
                    token = Token.Divide;
                    literal = c.ToString();
                }
                else
                {
                    SwitchParseMode(ref token, ref literal, c);
                }

                _position++;

                if (token != Token.None)
                {
                    return (token, literal);
                }
            }
            // In case last char in file leads to a token
            SwitchParseMode(ref token, ref literal, c);
            if (token == Token.None) token = Token.EndOfFile;
            return (token, literal);
        }

        /// <summary>
        /// With the characters '-' there can be a conflict between Token.Minus and Token.Float. Likewise for '+'. Solve it here.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="literalStartPos"></param>
        /// <returns></returns>
        private (Token, string) TryExpandToFloat(ref int position, ref int literalStartPos)
        {
            if ("Ee".Contains(PeekPriorChar()))
            {
                int startFrom = GetWhiteSpaceOrDelimPos(_position - 2, left: true);
                int length = GetWhiteSpaceOrDelimPos(_position + 1, left: false) - startFrom;
                if (_regExFloat.Match(Input, startFrom, length).Success)
                {
                    literalStartPos = startFrom;
                    position = startFrom + length;
                    return (Token.Float, GetLiteral());
                }
            }
            return (Token.None, string.Empty);
        }

        private int GetWhiteSpaceOrDelimPos(int pos, bool left)
        {
            int p = pos;
            if (left)
            {
                while (p > 0)
                {
                    if (IsWhiteSpace(Input[p]) || IsDelimiter(Input[p])) break;
                    p--;
                }
            }
            else
            {
                while (p <= _EOFIndex)
                {
                    if (IsWhiteSpace(Input[p]) || IsDelimiter(Input[p])) break;
                    p++;
                }
            }
            return p;
        }

        public (Token, string) GetLookAhead()
        {
            int savePosition = _position;
            (Token, string) result = GetNextToken();
            _position = savePosition;
            return result;
        }

        /// <summary>Consumes an exact literal at a token boundary, in caller-supplied priority order.</summary>
        public bool TryGetLiteral(IEnumerable<string> candidates, out string literal)
        {
            var start = _position;
            while (start <= _EOFIndex)
            {
                if (IsWhiteSpace(Input[start])) { start++; continue; }
                if (start < _EOFIndex && Input[start] == '/' && Input[start + 1] == '*')
                {
                    var close = Input.IndexOf("*/", start + 2, StringComparison.Ordinal);
                    if (close < 0) break;
                    start = close + 2;
                    continue;
                }
                if (start < _EOFIndex && Input[start] == '/' && Input[start + 1] == '/')
                {
                    while (start <= _EOFIndex && Input[start] != '\r' && Input[start] != '\n') start++;
                    continue;
                }
                break;
            }
            foreach (var candidate in candidates)
            {
                var end = start + candidate.Length;
                if (end > _EOFIndex ||
                    string.CompareOrdinal(Input, start, candidate, 0, candidate.Length) != 0 ||
                    !IsDelimiter(Input[end])) continue;

                for (var i = _position; i < start; i++)
                    if (Input[i] == '\n' || (Input[i] == '\r' && Input[i + 1] != '\n')) Line++;
                _position = end;
                _parseMode = ParseMode.None;
                literal = candidate;
                return true;
            }
            literal = null;
            return false;
        }

        /// <summary>
        /// Add custom token (keyword, ordinal) or override (a limited set of) predefined tokens for a literal
        /// </summary>
        /// <param name="literal"></param>
        /// <param name="token"></param>
        /// <param name="name"></param>
        public void SetLiteralToken(string literal, Token token, string name = "")
        {
            switch (token)
            {
                case Token.Keyword:
                    {
                        _keywords.Add(literal);
                        break;
                    }
                case Token.Ordinal:
                    {
                        SetRegExToken($"^{literal}$", name);
                        break;
                    }
                case Token.Assign:
                    {
                        if (!_overrideAssign) { DoOverrideAssignOp(); }
                        _assignOps.Add(literal);
                        break;
                    }
                case Token.Comparison:
                    {
                        if (!_overrideComparison) { DoOverrideComparisonOps(); }
                        _comparisonOps.Add(literal);
                        break;
                    }
                default:
                    {
                        throw new Exception($"Cannot add or override {literal} as token {token}");
                    }
            }
        }

        public void AddKeyword(string literal)
        {
            SetLiteralToken(literal, Token.Keyword);
        }

        /// <summary>
        /// Add custom token (by name) for a regular expression
        /// </summary>
        /// <param name="regEx"></param>
        /// <param name="name"></param>
        public void SetRegExToken(string regEx, string name)
        {
            if (!ValidateRegEx(regEx)) throw new Exception($"Cannot add regex {regEx} as custom token");
            _customRegExs.Add(new Tuple<Regex, string>(new Regex(regEx, RegexOptions.Compiled | RegexOptions.IgnoreCase), name));
        }

        private string GetCurrentPos()
        {
            return $"Row {Line} Column {_position}";
        }

        private void SwitchParseMode(ref Token token, ref string literal, char c)
        {
            switch (_parseMode)
            {
                case ParseMode.None: TryStartStringOrLiteralMode(c); break;
                case ParseMode.BlockComment: TryEndBlockComment(c); break;
                case ParseMode.InlineComment: TryEndLineComment(c); break;
                case ParseMode.Literal: TryEndLiteral(c, ref token, ref literal); break;
                case ParseMode.String: TryEndString(c, ref token, ref literal); break;
                default: Debug.Assert(false); break;
            }
        }
        private bool ValidateRegEx(string regEx)
        {
            return true; // TODO: check that it doesn't already exist anywhere
        }

        private void DoOverrideAssignOp()
        {
            Debug.Assert(!_overrideAssign);
            _overrideAssign = true;
            _assignOps.Clear();
        }

        private void DoOverrideComparisonOps()
        {
            Debug.Assert(!_overrideComparison);
            _overrideComparison = true;
            _comparisonOps.Clear();
        }

        private void TryStartStringOrLiteralMode(char c)
        {
            Debug.Assert(_parseMode == ParseMode.None);

            if (c == '"' && PeekPriorChar() != '"')
            {
                _parseMode = ParseMode.String;
                _literalStartPos = _position + 1;
            }
            else if (!IsWhiteSpace(c))
            {
                _parseMode = ParseMode.Literal;
                _literalStartPos = _position;
            }
        }

        private void TryEndString(char c, ref Token token, ref string literal)
        {
            Debug.Assert(_parseMode == ParseMode.String);
            if (c == '"')
            {
                literal = GetStringLiteral();
                token = Token.String;
                _parseMode = ParseMode.None;
            }
        }

        private void TryEndLiteral(char c, ref Token token, ref string literal)
        {
            Debug.Assert(_parseMode == ParseMode.Literal);
            literal = string.Empty;
            if (IsDelimiter(c))
            {
                (token, _) = LookupToken(out literal);
                _parseMode = ParseMode.None;
                if (!IsWhiteSpace(c)) _position--; // Don't forget this char in the next pass
            }
        }

        private bool IsWhiteSpace(char c) => char.IsWhiteSpace(c) || c == '\r' || c == '\n';

        private bool IsDelimiter(char c) => IsWhiteSpace(c) || "()[],+-*/^√".Contains(c);

        private void TryEndLineComment(char c)
        {
            Debug.Assert(_parseMode == ParseMode.InlineComment);
            if (IsEndOfLine())
            {
                _parseMode = ParseMode.None;
            }
        }

        private void TryEndBlockComment(char c)
        {
            Debug.Assert(_parseMode == ParseMode.BlockComment);
            Debug.Assert(_position > 0);
            if (c == '/' && PeekPriorChar() == '*')
            {
                _parseMode = ParseMode.None;
            }
        }

        private bool IsEndOfFile()
        {
            return _position > _EOFIndex;
        }

        private bool IsEndOfLine()
        {
            // A string containing "\r\n" for non-Unix platforms, or a string containing "\n" for Unix platforms.
            // So this can be improved upon...
            return Input[_position] == '\n' && PeekPriorChar() == '\r';
        }

        private char PeekPriorChar() => _position > 0 ? Input[_position - 1] : '\0';
        private char PeekNextChar() => _position < _EOFIndex ? Input[_position + 1] : '\0';

        private string GetLiteral()
        {
            Debug.Assert(_literalStartPos >= 0 && _literalStartPos <= _EOFIndex);
            return Input.Substring(_literalStartPos, _position - _literalStartPos);
        }      
        private string GetStringLiteral()
        {
            Debug.Assert(_literalStartPos >= 0 && _literalStartPos <= _EOFIndex);
            return Input.Substring(_literalStartPos, _position - _literalStartPos);
        }
        
        private (Token, string) LookupToken(out string literal)
        {
            literal = GetLiteral();

            if (IsCustomToken(literal, out string name)) return (Token.Ordinal, name);
            if (_keywords.Contains(literal)) return (Token.Keyword, "Keyword"); 
            if (string.Compare(literal, "not", StringComparison.OrdinalIgnoreCase) == 0) return (Token.Not, "Not");
            if (string.Compare(literal, "and", StringComparison.OrdinalIgnoreCase) == 0) return (Token.And, "And");
            if (_comparisonOps.Contains(literal)) return (Token.Comparison, "CompareOp");
            if (_assignOps.Contains(literal)) return (Token.Assign, "AssignOp");
            if (_regExInteger.IsMatch(literal)) return (Token.Integer, "Integer");
            if (_regExFloat.IsMatch(literal)) return (Token.Float, "Float");
            if (_regExIdentifier.IsMatch(literal)) return (Token.Identifier, "ID");

            return (Token.None, string.Empty); 
        }

        private bool IsCustomToken(string literal, out string name)
        {
            foreach (Tuple<Regex,string> tuple in _customRegExs)
            {
                if (tuple.Item1.IsMatch(literal))
                {
                    name = tuple.Item2;
                    return true;
                }
            }
            name = string.Empty;
            return false;
        }
    }
}
