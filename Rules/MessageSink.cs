using System;
using System.Collections.Generic;
using System.Text;
using Rules.Expression;
using Rules.Model;

namespace Rules
{
    public delegate TConstantValue NotifyUnknownValueEvent(TId subject, ValueBag context);

    /// <summary>
    /// Simple notifications between objects by means of delegates
    /// </summary>
    public static class MessageSink
    {
        public static NotifyUnknownValueEvent NotifyUnknownValue;
        public static NotifyUnknownValueEvent GetUserInput;
    }
}
