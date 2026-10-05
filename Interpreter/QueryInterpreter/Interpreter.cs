using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.Rules;
using System;
using System.Collections.Generic;

namespace Rules.QueryInterpreter
{
    public class Interpreter
    {
        /// <summary>
        /// Translate a user query or task request into a related <see cref="TExpression"/>
        /// </summary>
        /// <remarks>
        /// A query is a question or task request on an abstract level, where the answer or task result is derived
        /// using the model (or value bag in case of no model), production rules and user answers.
        /// </remarks>
        /// <param name="query"></param>
        /// <returns></returns>
        public (TCompareExpression?, List<Tuple<string, string>>?) QueryToExpression(string query)
        {
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(query);
            if (parser.SyntaxErrors.Count == 0)
            {
                return (parser.Builder.Result as TCompareExpression, null);
            }
            else
            {
                return (null, parser.SyntaxErrors);
            }
        }


    }
}
