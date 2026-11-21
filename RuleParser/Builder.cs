using System;
using System.Collections.Generic;

namespace Rules.Parser
{
    public class Builder
    {
        protected readonly Stack<Tuple<ParsedItem, object>> _stack = new();

        /// <summary>
        /// Last compiled object
        /// </summary>
        public object Result { get; internal set; }
    }
}
