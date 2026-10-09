using System;
using Utilities.LinkedList;

namespace Rules.Parser
{
    public class Builder
    {
        protected TLinkedStack<Tuple<ParsedItem, object>> _stack = new TLinkedStack<Tuple<ParsedItem, object>>();

        /// <summary>
        /// Last compiled object
        /// </summary>
        public object Result { get; internal set; }
    }
}
