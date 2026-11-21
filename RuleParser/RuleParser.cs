using Lexical;
using System;
using System.Diagnostics;
using Rules.Model;

namespace Rules.Parser
{
    public enum RulePart { RuleContainer, Rule, Condition, Conclusion, Expression };

    /// <summary>
    /// Left associative parser for the rule language
    /// </summary>
    public class RuleParser : Parser
    {
        private readonly Enum _factorStart = Token.LeftHook | Token.Integer | Token.Float | Token.Identifier | Token.String | Token.Not; // Token.Not if Factor is an Id...
        private readonly Enum _exprStart = Token.LeftHook | Token.Integer | Token.Float | Token.Identifier | Token.Minus;
        private readonly Enum _conditionStart = Token.LeftHook | Token.Integer | Token.Float | Token.Identifier | Token.Not;
        private readonly Enum _conclusionStart = Token.LeftHook | Token.Integer | Token.Float | Token.Identifier;
        private readonly Enum _ruleStart = Token.Keyword;
        private readonly Enum _ruleContainerStart = Token.Keyword;
        private readonly Enum _mathOperator = Token.Minus | Token.Plus | Token.Multiply | Token.Divide;
        private readonly bool _isRuleFile;
        private bool _hasStartedContainers;

        public string ModelFileName { get; private set; }

        /// <summary>
        /// Create a parser for an entire rule of for just a part of it
        /// </summary>
        /// <param name="rulePart"></param>
        public RuleParser(RulePart rulePart = RulePart.Rule)
        {
            switch (rulePart)
            {
                case RulePart.Expression: _parse = Expression; _lookAheads = _exprStart; break;
                case RulePart.Condition: _parse = RuleCondition; _lookAheads = _conditionStart; break;
                case RulePart.Conclusion: _parse = Conclusion; _lookAheads = _conclusionStart;  break;
                case RulePart.Rule: _parse = Rule; _lookAheads = _ruleStart; break;
                case RulePart.RuleContainer:
                    _parse = RuleFileEntry;
                    _lookAheads = _ruleContainerStart;
                    _isRuleFile = true;
                    break;
            }
        }

        protected override void SetTokens()
        {
            ModelFileName = null;
            _hasStartedContainers = false;
            // Operators are set by Tokenizer by default
            // We add our keywords here
            _tokenizer.AddKeyword("rulecontainer");
            _tokenizer.AddKeyword("rule");
            _tokenizer.AddKeyword("priority");
            _tokenizer.AddKeyword("description");
            _tokenizer.AddKeyword("if");
            _tokenizer.AddKeyword("then");
            if (_isRuleFile)
            {
                _tokenizer.AddKeyword("enforce");
                _tokenizer.AddKeyword("model");
            }
        }

        protected override Builder CreateBuilder()
        {
            return new RulesBuilder(_parseTree);
        }

        private void RuleFileEntry()
        {
            if (GetNextTokenIfIsKeyword("enforce", out _))
            {
                if (_hasStartedContainers || ModelFileName != null)
                {
                    AddSyntaxError("enforce model must appear once before the rule containers");
                    return;
                }

                if (!GetNextTokenIfIsKeyword("model", out var literal))
                {
                    AddSyntaxError("model", literal);
                    return;
                }

                if (!LookAheadHasAnyOf(Token.String, out literal))
                {
                    AddSyntaxError(Token.String, literal);
                    return;
                }

                _tokenizer.GetNextToken();
                ModelFileName = literal;
                _parseTree.Add(ParsedItem.ModelFile, literal);
            }
            else if (NextTokenIsKeyword("rulecontainer", out _))
            {
                _hasStartedContainers = true;
                RuleContainer();
            }
            else
            {
                (_, var literal) = _tokenizer.GetLookAhead();
                AddSyntaxError("RuleContainer or enforce model", literal);
            }
        }

        private void RuleContainer()
        {
            /* RuleContainer [mynamespace.]myContainerName
             * [Description "yadiyadiya"]
             * 
             *   Rule [mynamespace.]myFirstRule
             *   [Description "bladibla and yadiya"]
             *   If Condition then Conclusion
             *   
             *   Rule [mynamespace.]mySecondRule
             *   [Description "bladibla and yadiya"]
             *   If Condition then Conclusion
             */
            if (!GetNextTokenIfIsKeyword("rulecontainer", out string literal))
            {
                AddSyntaxError("RuleContainer", literal);
                return;
            }
            _parseTree.Begin(ParsedItem.RuleCont);
            if (!LookAheadHasAnyOf(Token.Identifier, out literal))
            {
                AddSyntaxError(Token.Identifier, literal);
                return;
            }
            QuantifiedName();

            while (NextTokenIsKeyword("rule", out _))
            {
                Rule();
            }
            _parseTree.End();
        }

        private void Rule()
        {
            if (!GetNextTokenIfIsKeyword("rule", out string literal))
            {
                AddSyntaxError("Rule", literal);
            }
            _parseTree.Begin(ParsedItem.Rule);

            if (!LookAheadHasAnyOf(Token.Identifier, out literal))
            {
                AddSyntaxError(Token.Identifier, literal);
            }
            QuantifiedName();

            if (GetNextTokenIfIsKeyword("priority", out _))
            {
                if (!LookAheadHasAnyOf(Token.Integer, out literal))
                {
                    AddSyntaxError(Token.Integer, literal);
                }
                else
                {
                    _parseTree.Begin(ParsedItem.Prio);
                    (_, literal) = _tokenizer.GetNextToken();
                    _parseTree.Add(ParsedItem.Integer, literal);
                    _parseTree.End();
                }
            }

            if (GetNextTokenIfIsKeyword("description", out _))
            {
                if (!LookAheadHasAnyOf(Token.String, out literal))
                {
                    AddSyntaxError(Token.String, literal);
                }
                else
                {
                    _parseTree.Add(ParsedItem.String, literal);
                }
            }
            if (!GetNextTokenIfIsKeyword("if", out literal))
            {
                AddSyntaxError("if", literal);
            }
            else
            {
                _parseTree.Add(ParsedItem.If);
            }
            if (!LookAheadHasAnyOf(_conditionStart))
            {
                (_, literal) = _tokenizer.GetLookAhead();
                AddSyntaxError(_conditionStart, literal);
            }
            RuleCondition();

            if (!GetNextTokenIfIsKeyword("then", out literal))
            {
                AddSyntaxError("then", literal);
            }
            else
            {
                _parseTree.Add(ParsedItem.Then);
            }

            if (!LookAheadHasAnyOf(_conclusionStart))
            {
                (_, literal) = _tokenizer.GetLookAhead();
                AddSyntaxError(_conclusionStart, literal);
            }
            Conclusion();
            
            _parseTree.End(); 
        }

        private void QuantifiedName()
        {
            (Token token, string literal) = _tokenizer.GetNextToken();
            Debug.Assert(token == Token.Identifier);
            _parseTree.Add(ParsedItem.Id, literal);
        }

        private void Conclusion()
        {
            _parseTree.Begin(ParsedItem.Concl);

            if (LookAheadHasAnyOf(_conditionStart))
            {
                AssignExpression();

                while (LookAheadHasAnyOf(Token.And))
                {
                    (_, string literal) = _tokenizer.GetNextToken();
                    _parseTree.Add(ParsedItem.LogicalOp, literal);
                    AssignExpression();
                }
            }
            _parseTree.End();
        }

        private void AssignExpression()
        {
            Token token;
            string literal;

            _parseTree.Begin(ParsedItem.AssignExpr);

            // Left part
            (token, literal) = _tokenizer.GetNextToken();
            if (token == Token.Identifier)
            {
                _parseTree.Add(ParsedItem.Id, literal);

                // Right part is optional only if left part is a logical expression
                (token, literal) = _tokenizer.GetLookAhead();
                if (token == Token.Assign)
                {
                    _tokenizer.GetNextToken();
                    _parseTree.Add(ParsedItem.Assign, literal);

                    // Right part
                    Expression();
                }
            }
            else
            {
                AddSyntaxError(Token.Identifier, literal);
            }
            _parseTree.End();
        }

        private void RuleCondition()
        {
            _parseTree.Begin(ParsedItem.Cond);

            if (LookAheadHasAnyOf(_conditionStart))
            {
                (_, _) = _tokenizer.GetLookAhead();

                CompareExpression();

                while (LookAheadHasAnyOf(Token.And)) 
                {
                    (_, string literal) = _tokenizer.GetNextToken();
                    _parseTree.Add(ParsedItem.LogicalOp, literal);
                    CompareExpression();
                }
            }

            _parseTree.End();
        }

        private void CompareExpression()
        {
            _parseTree.Begin(ParsedItem.CompExpr);

            Expression(); // "left part"

            if (LookAheadHasAnyOf(Token.Comparison, out string literal))
            {
                (_, literal) = _tokenizer.GetNextToken();
                _parseTree.Add(ParsedItem.Compare, literal);

                Expression(); // "right part"
            }
            else
            {
                // To end up here is only allowed if CompareExpression is purely logical, which means that "left part" Expression 
                // should contain no math operators. The "left part" Expression may still be meant to be numerical, but that's 
                // outside the scope of the parser.
                // E.g. if "left part" Expression = "length" then it will be valid syntax, but probably not a valid expression if
                // the "model" behind it defines "length" as numerical...
                // TODO: if (_parseTree.ContainsMathOp()) AddSyntaxError("comparison operator", literal);
            }

            _parseTree.End();
        }

        private void Expression()
        {
            _parseTree.Begin(ParsedItem.Expr);

            Term(); 

            while (GetNextTokenIfAnyOfChar(_mathOperator, "+-", out string op))
            {
                _parseTree.Add(ParsedItem.MathOp, op);
                Term();
            }
            _parseTree.End();
        }

        private void Term()
        {
            _parseTree.Begin(ParsedItem.Term);

            MinOrNotFactor();

            while (GetNextTokenIfAnyOfChar(_mathOperator, "*/", out string op))
            {
                _parseTree.Add(ParsedItem.MathOp, op);
                MinOrNotFactor();
            }
            _parseTree.End();
        }

        private void MinOrNotFactor()
        {
            bool minus = false;
            bool negation = false;

            if (LookAheadHasAnyOf(Token.Not))
            {
                _parseTree.Begin(ParsedItem.Not);
                _tokenizer.GetNextToken();
                negation = true;
            }
            else if (GetNextTokenIfAnyOfChar(Token.Minus, "-"))
            {
                _parseTree.Begin(ParsedItem.Minus);
                minus = true;
            }
            if (!LookAheadHasAnyOf(_factorStart, out string literal)) AddSyntaxError(_factorStart.ToString(), literal);
            Factor();
            if (minus || negation)
            {
                _parseTree.End();
            }
        }

        private void UnitSuffix()
        {
            if (ModelFileName == null) return;
            if (_tokenizer.TryGetLiteral(MetaModelUnits.Symbols, out var standardSymbol))
            {
                _parseTree.Add(ParsedItem.UnitSuffix, standardSymbol);
            }
            else if (LookAheadHasAnyOf(Token.Identifier, out var suffix))
            {
                _tokenizer.GetNextToken();
                _parseTree.Add(ParsedItem.UnitSuffix, suffix);
            }
        }

        private void Factor()
        {
            _parseTree.Begin(ParsedItem.Factor);
            (Token token, string literal) = _tokenizer.GetNextToken();
            switch (token)
            {
                case Token.LeftHook:
                    {
                        _parseTree.Add(ParsedItem.Lhook);
                        Expression();
                        if (!GetNextTokenIfAnyOfChar(Token.RightHook, ")"))
                        { 
                            AddSyntaxError(")"); 
                        }
                        else
                        {
                            _parseTree.Add(ParsedItem.Rhook);
                        }
                        break;
                    }
                case Token.Integer:
                    {
                        _parseTree.Add(ParsedItem.Integer, literal);
                        UnitSuffix(); break;
                    }
                case Token.Float:
                    {
                        _parseTree.Add(ParsedItem.Float, literal);
                        UnitSuffix(); break;
                    }
                case Token.Identifier:
                    {
                        _parseTree.Add(ParsedItem.Id, literal); break;
                    }
                case Token.String:
                    {
                        _parseTree.Add(ParsedItem.String, literal); break;
                    }
                default: break;
            }
            _parseTree.End();
        }
      
    }
}
