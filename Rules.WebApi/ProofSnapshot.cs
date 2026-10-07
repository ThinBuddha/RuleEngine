using Rules.Rules;

namespace Rules.WebApi;

/// <summary>Transport-only snapshot; no engine, expression or mutable context references.</summary>
public sealed record ProofSnapshot(string Container, ProofNodeSnapshot Root, int? ActiveNodeId)
{
    public static ProofSnapshot? From(DeductionProof? proof) => proof is null ? null :
        new(proof.Container, ProofNodeSnapshot.From(proof.Root), proof.ActiveNodeId);
}

public sealed record ProofNodeSnapshot(int Id, string Kind, string Text, string State,
    object? Value, string? Source, int? EvidenceId, IReadOnlyList<ProofNodeSnapshot> Children)
{
    internal static ProofNodeSnapshot From(ProofNode node) => new(node.Id, node.Kind.ToString(),
        node.Text, node.State.ToString(), node.Value, node.Source?.ToString(), node.EvidenceId,
        Array.AsReadOnly(node.Children.Select(From).ToArray()));
}
