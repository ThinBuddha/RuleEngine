using System;
using Rules.Expression;
using Rules.Model;

namespace Rules
{
    public delegate TConstantValue NotifyUnknownValueEvent(TId subject, ValueBag context);

    /// <summary>
    /// Input callbacks owned by one evaluation context. Never shared between sessions.
    /// </summary>
    public sealed class MessageSink
    {
        public NotifyUnknownValueEvent NotifyUnknownValue { get; set; }
        public NotifyUnknownValueEvent GetUserInput { get; set; }

        internal IDisposable UseResolver(NotifyUnknownValueEvent resolver)
        {
            var scope = new ResolverScope(this, NotifyUnknownValue);
            NotifyUnknownValue = resolver;
            return scope;
        }

        private sealed class ResolverScope : IDisposable
        {
            private readonly MessageSink _sink;
            private readonly NotifyUnknownValueEvent _previous;

            public ResolverScope(MessageSink sink, NotifyUnknownValueEvent previous)
            {
                _sink = sink;
                _previous = previous;
            }

            public void Dispose() => _sink.NotifyUnknownValue = _previous;
        }
    }
}
