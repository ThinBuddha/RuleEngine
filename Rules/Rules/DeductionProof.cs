using System;
using System.Collections.Generic;
using System.Linq;
using Rules.Expression;

namespace Rules.Rules
{
    public enum ProofNodeKind { Query, Goal, Rule, Condition, Operand, Conclusion, Evidence }
    public enum ProofState { Evaluating, WaitingForInput, Succeeded, Failed, Unknown, Skipped, Error }
    public enum EvidenceSource { SuppliedFact, UserAnswer, RuleConclusion }

    /// <summary>An immutable snapshot; references point to node IDs within the same proof.</summary>
    public sealed record ProofNode(int Id, ProofNodeKind Kind, string Text, ProofState State,
        object Value, EvidenceSource? Source, int? EvidenceId, IReadOnlyList<ProofNode> Children);
    public sealed record DeductionProof(ProofNode Root, int? ActiveNodeId)
    {
        public string Container { get; init; }
    }

    /// <summary>Owned by one deduction. Consumers receive immutable snapshots only.</summary>
    internal sealed class ProofRecorder
    {
        private sealed class Node
        {
            internal int Id;
            internal ProofNodeKind Kind;
            internal string Text;
            internal ProofState State = ProofState.Evaluating;
            internal object Value;
            internal EvidenceSource? Source;
            internal int? EvidenceId;
            internal readonly List<Node> Children = new();
            internal ProofNode Snapshot() => new(Id, Kind, Text, State, Value, Source, EvidenceId,
                Array.AsReadOnly(Children.Select(child => child.Snapshot()).ToArray()));
        }
        private readonly Stack<Node> _path = new();
        private readonly Dictionary<string, int> _evidence = new(StringComparer.Ordinal);
        private Node _root;
        private int _nextId;
        private readonly string _container;
        internal ProofRecorder(string container) => _container = container;
        internal DeductionProof Snapshot() => new(_root.Snapshot(), _path.Count == 0 ? null : _path.Peek().Id) { Container = _container };
        internal Scope Begin(ProofNodeKind kind, string text)
        {
            var node = new Node { Id = ++_nextId, Kind = kind, Text = text };
            if (_path.Count == 0) _root = node;
            else _path.Peek().Children.Add(node);
            _path.Push(node);
            return new Scope(this);
        }
        internal void Read(string name, TConstantValue value)
        {
            using var scope = Begin(ProofNodeKind.Evidence, name);
            if (_evidence.TryGetValue(name, out var id)) _path.Peek().EvidenceId = id;
            else _path.Peek().Source = EvidenceSource.SuppliedFact;
            scope.Complete(value);
        }
        internal void Remember(string name, EvidenceSource source)
        {
            _path.Peek().Source = source;
            _evidence[name] = _path.Peek().Id;
        }
        internal sealed class Scope : IDisposable
        {
            private readonly ProofRecorder _owner;
            internal Scope(ProofRecorder owner) => _owner = owner;
            internal void State(ProofState state) => _owner._path.Peek().State = state;
            internal void Source(EvidenceSource source) => _owner._path.Peek().Source = source;
            internal void Complete(bool result)
            {
                State(result ? ProofState.Succeeded : ProofState.Failed);
                _owner._path.Peek().Value = result;
            }
            internal void Complete(TConstantValue value)
            {
                State(value == null ? ProofState.Unknown : ProofState.Succeeded);
                _owner._path.Peek().Value = value switch
                {
                    TConstantStringValue s => s.Value,
                    TConstantNumValue n when n.Value.ValueType == TConstantNumValue.BoolType => n.Value.BoolValue,
                    TConstantNumValue n when n.Value.ValueType == TConstantNumValue.DoubleType => n.Value.DoubleValue,
                    TConstantNumValue n => n.Value.IntValue,
                    _ => null
                };
            }
            public void Dispose()
            {
                var node = _owner._path.Pop();
                if (node.State is ProofState.Evaluating or ProofState.WaitingForInput) node.State = ProofState.Unknown;
            }
        }
    }
}
