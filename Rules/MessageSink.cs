using System;
using System.Collections.Generic;
using System.Text;
using Rules.Expression;
using Rules.Model;
using System.Threading;

namespace Rules
{
    public delegate TConstantValue NotifyUnknownValueEvent(TId subject, ValueBag context);

    /// <summary>
    /// Simple notifications between objects by means of delegates
    /// </summary>
    public static class MessageSink
    {
        // Keep callbacks local to the execution, including while a web user answers.
        private static readonly AsyncLocal<NotifyUnknownValueEvent> _notifyUnknownValue = new AsyncLocal<NotifyUnknownValueEvent>();
        private static readonly AsyncLocal<NotifyUnknownValueEvent> _getUserInput = new AsyncLocal<NotifyUnknownValueEvent>();

        public static NotifyUnknownValueEvent NotifyUnknownValue
        {
            get => _notifyUnknownValue.Value;
            set => _notifyUnknownValue.Value = value;
        }

        public static NotifyUnknownValueEvent GetUserInput
        {
            get => _getUserInput.Value;
            set => _getUserInput.Value = value;
        }
    }
}
