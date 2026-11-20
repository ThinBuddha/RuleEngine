using System;
using System.Text;

namespace Rules.Parser
{
    public enum ParsedItem
    {
        RuleCont, Rule, Prio, Cond, Concl, Expr, CompExpr, AssignExpr, Assign, MathOp, LogicalOp, Term,
        Factor, Id, Lhook, Rhook, Integer, Float, String, If, Not, Compare, Minus, Then,
        UnitDecl, TypeDecl, BaseType, Short, Bounds, Min, Max, Values, ModelFile, UnitSuffix,
        ClassDecl, Property, TypeRef, ClassRef, AtomicType
    };

    public class ParseTree
    {
        protected readonly StringBuilder _parseTree = new StringBuilder();

        public delegate void NotifyParseEvent(ParsedItem item, string value = "");
        public delegate void NotifyParseEndEvent();
        public NotifyParseEvent OnStart { get; set; }
        public NotifyParseEvent OnAdd { get; set; }
        public NotifyParseEndEvent OnEnd { get; set; }

        public void Begin(ParsedItem item)
        {
            _parseTree.Append($"{item.ToString()}(");
            OnStart?.Invoke(item);
        }
        public void Add(ParsedItem item, string value = "")
        {
            _parseTree.Append($"{item.ToString()}({value})");
            OnAdd?.Invoke(item, value);
        }
        public void End()
        {
            _parseTree.Append(")");
            OnEnd?.Invoke();
        }

        public void Clear()
        {
            _parseTree.Clear();
        }

        public override string ToString()
        {
            return _parseTree.ToString();
        }

        public string Expand()
        {
            string parseTree = _parseTree.ToString();

            int level = 0;
            StringBuilder result = new StringBuilder();
            char c, priorC = '\0';
            for (int i = 0; i < parseTree.Length; i++)
            {
                c = parseTree[i];
                if (c == '(')
                {
                    level += 2;
                    result.Append(Environment.NewLine);
                    result.Append("".PadLeft(level, ' '));
                }
                else if (c == ')')
                {
                    level -= 2;
                    if (priorC != ')') result.Append(Environment.NewLine);
                    if (i < parseTree.Length - 1 && parseTree[i + 1] != ')') result.Append("".PadRight(level + 1, ' '));
                }
                else
                {
                    result.Append(c);
                }
                priorC = c;
            }
            return result.ToString();
        }
    }
}
