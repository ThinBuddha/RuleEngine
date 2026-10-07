using System.IO;
using System.Text.Json;
using Rules.Rules;

namespace Rules.CommandShell
{
    internal static class ProofText
    {
        internal static void Write(DeductionProof proof, TextWriter output)
        {
            output.WriteLine($"Explanation: {proof.Container} ({proof.Root.State})");
            WriteNode(proof.Root, proof.ActiveNodeId, output, "");
        }

        private static void WriteNode(ProofNode node, int? activeId, TextWriter output, string indent)
        {
            var value = node.Value == null ? "" : $" = {JsonSerializer.Serialize(node.Value)}";
            var source = node.Source == null ? "" : $"; source: {node.Source}";
            var reference = node.EvidenceId == null ? "" : $"; evidence: #{node.EvidenceId}";
            var active = node.Id == activeId ? " <-- current question" : "";
            output.WriteLine($"{indent}#{node.Id} {node.Kind}: {node.Text} [{node.State}]{value}{source}{reference}{active}");
            foreach (var child in node.Children) WriteNode(child, activeId, output, indent + "  ");
        }
    }
}
