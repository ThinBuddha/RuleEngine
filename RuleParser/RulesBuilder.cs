using System;
using System.Diagnostics;
using System.Globalization;
using Rules.Expression;
using Rules.Rules;

namespace Rules.Parser
{
    /// <summary>
    /// Build a (container of) rules as the parser builds up the parse tree
    /// </summary>
    /// <remarks>
    /// This builder exists as a separation of concern, so that the parser can focus on applying syntax rules only.
    /// The builder kind of mirrors what the parsetree does, but where the parsetree just wants to build a tree (in string format),
    /// it's the builder that really compiles the rules.
    /// The builder also allows for building just a part, e.g. just a TExpression.
    /// </remarks>
    /// <example>
    /// The builder uses a (short lived) stack to arrange all the elements in the right order, like so:
    ///   tree.Start(X)	    => builder.OnStart(X) 	=> stack.Push( Create(X) )
    ///   tree.Add(Y, Z)	=> builder.OnAdd(Y, Z) 	=> stack.Peek().AddY(Z)
    ///   tree.End()	    => builder.OnEnd() 	    => L l= stack.Pop(); stack.Peek().Add(L)
    /// 
    /// Not all syntax constructs imply a rule object (some imply nothing, some imply a property), in which case we need to do an extra peek
    ///   stack.Peek(1)
    /// 
    /// Some rule objects may be abstract/virtual (like TFactor) in which case they get replaced by their descendant:
    ///   stack.Replace()
    /// </example>
    public class RulesBuilder : Builder
    {
        public RuleContainers RuleContainers { get; internal set; }

        public RulesBuilder(ParseTree parseTree)
        {
            // Set delegates so we can react on parse events
            parseTree.OnStart = Start;
            parseTree.OnAdd = Add;
            parseTree.OnEnd = End;
        }

        /// <summary>
        /// Add the next (complex) part of the syntax tree to our stack together with the related "object to be compiled", if any 
        /// </summary>
        /// <param name="parsedItem"></param>
        /// <param name="_"></param>
        public void Start(ParsedItem parsedItem, string _)
        {
            _stack.Push(new Tuple<ParsedItem, object>(parsedItem, CreateRuleObject(parsedItem)));
        }

        /// <summary>
        /// Add the next (simple) part of the syntax tree and finish it as well
        /// </summary>
        /// <param name="parsedItem">syntax item parsed</param>
        /// <param name="value">literal</param>
        public void Add(ParsedItem parsedItem, string value)
        {
            Tuple<ParsedItem, object> item = _stack.Peek();

            Type itemType = item.Item2?.GetType();

            switch (parsedItem)
            {                   
                case ParsedItem.MathOp:
                    if ("+-".Contains(value))
                    {
                        ((TExpression)item.Item2).AddOperator(value);
                    }
                    else if ("*/".Contains(value))
                    {
                        ((TTerm)item.Item2).SetOperator(value);
                    }
                    break;

                case ParsedItem.LogicalOp:
                    // Should be "and", nothing to do here
                    break;

                case ParsedItem.Compare:
                    if (itemType == typeof(TCompareExpression))
                    {
                        ((TCompareExpression)item.Item2).AddOperator(value);
                    }
                    break;

                case ParsedItem.Id:
                    if (itemType == typeof(RuleContainer))
                    {
                        ((RuleContainer)item.Item2).Name = value;
                    }
                    else if (itemType == typeof(Rule))
                    {
                        ((Rule)item.Item2).SetName(value);
                    }
                    else if (itemType == typeof(TFactor))
                    {
                        TId id = new TId { Name = value };
                        _stack.Replace(new Tuple<ParsedItem, object>(parsedItem, id));
                    }
                    else if (itemType == typeof(TAssignExpression))
                    {
                        ((TAssignExpression)item.Item2).SetLeftExpr(value);
                    }
                    break;

                case ParsedItem.Integer:
                    if (itemType == typeof(TFactor))
                    {
                        TConstantNumValue numValue = new TConstantNumValue(int.Parse(value));
                        _stack.Replace(new Tuple<ParsedItem, object>(parsedItem, numValue));
                    }
                    else if (item.Item1 == ParsedItem.Prio)
                    {
                        (_stack.Peek(1).Item2 as Rule).Priority = int.Parse(value);
                    }
                    break;

                case ParsedItem.Float:
                    if (itemType == typeof(TFactor))
                    {
                        TConstantNumValue numValue = new TConstantNumValue(
                            double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
                        _stack.Replace(new Tuple<ParsedItem, object>(parsedItem, numValue));
                    }
                    break;

                case ParsedItem.String:
                    if (itemType == typeof(TFactor))
                    {
                        TConstantStringValue strValue = new TConstantStringValue(value);
                        _stack.Replace(new Tuple<ParsedItem, object>(parsedItem, strValue));
                    }
                    break;
              
                case ParsedItem.Minus:
                    break;

                default:
                    break;
            }
        }

        public void End()
        {
            if (_stack.Count > 0)
            {
                Tuple<ParsedItem, object> item = _stack.Pop();
                Tuple<ParsedItem, object> priorItem = _stack.Count == 0 ? null : _stack.Peek();

                Type itemType = item.Item2?.GetType();
                Type priorItemType = priorItem?.Item2?.GetType();

                if (itemType == typeof(RuleContainer) )
                {
                    // We're done, we've got a RuleContainer
                    Result = item.Item2;
                    if (RuleContainers == null) RuleContainers = new RuleContainers();
                    RuleContainers.AddRuleContainer((RuleContainer)Result);
                }
                else if (itemType == typeof(Rule))
                {
                    if (_stack.Count > 0)
                    {
                        if (priorItemType == typeof(RuleContainer))
                        {
                            ((RuleContainer)priorItem.Item2).Rules.Add((Rule)item.Item2);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else
                    {
                        // We're done, we've got a Rule
                        Result = item.Item2;
                    }
                }
                else if (itemType == typeof(TCompareExpression))
                {
                    if (_stack.Count > 1 && priorItem.Item1 == ParsedItem.Cond)
                    {
                        priorItem = _stack.Peek(1);
                        if (priorItem.Item2.GetType() == typeof(Rule))
                        {
                            ((Rule)priorItem.Item2).AddCondition((TCompareExpression)item.Item2);
                        }
                    }
                    else
                    {
                        // We're done, we've got a TCompareExpression
                        Result = item.Item2;
                    }
                }
                else if (itemType == typeof(TAssignExpression))
                {
                    if (_stack.Count > 1 && priorItem.Item1 == ParsedItem.Concl)
                    {
                        priorItem = _stack.Peek(1);
                        if (priorItem.Item2.GetType() == typeof(Rule))
                        {
                            ((Rule)priorItem.Item2).AddConclusion((TAssignExpression)item.Item2);
                        }
                    }
                    else
                    {
                      // We're done, we've got a TAssignExpression
                      Result = item.Item2;
                    }

                }
                else if (itemType == typeof(TExpression))
                {
                    if (priorItemType == typeof(TCompareExpression))
                    {
                        ((TCompareExpression)priorItem.Item2).AddExpr((TExpression)item.Item2);
                    }
                    else if (priorItemType == typeof(TAssignExpression))
                    {
                        ((TAssignExpression)priorItem.Item2).SetRightExpr((TExpression)item.Item2);
                    }
                    else if (priorItemType == typeof(TTerm))
                    {
                        ((TTerm)priorItem.Item2).AddFactor((TExpression)item.Item2);
                    }
                    else if (priorItemType == typeof(TFactor))
                    {
                        // Replace the abstract TFactor by this specialized instance
                        _stack.Replace(new Tuple<ParsedItem, object>(item.Item1, item.Item2));
                    }
                    else if (priorItem?.Item1 == ParsedItem.Minus)
                    {
                        // The enclosing Factor was replaced by this expression. Apply
                        // the unary minus before attaching it to the enclosing term.
                        ((TExpression)item.Item2).ApplyMinusSign();
                        var term = _stack.Peek(1).Item2 as TTerm;
                        Debug.Assert(term != null);
                        term.AddFactor((TExpression)item.Item2);
                    }
                    else
                    {
                        // We're done, we've got an expression
                        Result = item.Item2;
                    }
                }
                else if (itemType == typeof(TTerm))
                {
                    if (priorItemType == typeof(TExpression))
                    {
                        ((TExpression)priorItem.Item2).AddTerm((TTerm)item.Item2);
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
                else if (itemType == typeof(TFactor)) 
                {
                    Debug.Assert(false, "Expected all factors to be replaced by specialized classes");
                }
                else if (itemType == typeof(TId) || itemType == typeof(TConstantNumValue) || itemType == typeof(TConstantStringValue))
                {
                    // These already replaced all factors earlier on
                    if (priorItemType == typeof(TTerm))
                    {
                        ((TTerm)priorItem.Item2).AddFactor((TFactor)item.Item2);
                    }
                    else if (priorItem.Item1 == ParsedItem.Not)
                    {
                        ((TId)item.Item2).ShouldBeNegated = true;
                        priorItem = _stack.Peek(1);
                        if (priorItem.Item2.GetType() == typeof(TTerm))
                        {
                            ((TTerm)priorItem.Item2).AddFactor((TFactor)item.Item2);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else if (priorItem.Item1 == ParsedItem.Minus)
                    {
                        ((TFactor)item.Item2).ApplyMinusSign();
                        priorItem = _stack.Peek(1);
                        if (priorItem.Item2.GetType() == typeof(TTerm))
                        {
                            ((TTerm)priorItem.Item2).AddFactor((TFactor)item.Item2);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
            }
        }

        private object CreateRuleObject(ParsedItem parsedItem)
        {
            switch (parsedItem)
            {
                case ParsedItem.RuleCont:
                    return new RuleContainer();

                case ParsedItem.Rule:
                    return new Rule();

                case ParsedItem.Expr:
                    return new TExpression();

                case ParsedItem.CompExpr:
                    return new TCompareExpression();
                    
                case ParsedItem.AssignExpr:
                    return new TAssignExpression();

                case ParsedItem.Term:
                    return new TTerm();

                case ParsedItem.Factor:
                    return new TFactor();

                case ParsedItem.Id:
                    return new TId();

                default:
                    return null;
             
            }
        }
    }
}
