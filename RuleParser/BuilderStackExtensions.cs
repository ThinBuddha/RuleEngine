using System;
using System.Collections.Generic;
using System.Linq;

namespace Rules.Parser
{
    /// <summary>Operations needed by the rule builder in addition to the standard stack API.</summary>
    internal static class BuilderStackExtensions
    {
        // Stack enumeration starts at the top, matching the builder's nesting depth.
        public static T Peek<T>(this Stack<T> stack, int depth)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(depth);
            return stack.ElementAt(depth);
        }

        public static void Replace<T>(this Stack<T> stack, T item)
        {
            stack.Pop();
            stack.Push(item);
        }
    }
}
