export function indexProof(root) {
  const nodes = new Map()
  const paths = new Map()
  function visit(node, ancestors) {
    nodes.set(node.id, node)
    const path = [...ancestors, node.id]
    paths.set(node.id, path)
    node.children.forEach(child => visit(child, path))
  }
  if (root) visit(root, [])
  return { nodes, paths }
}

export function proofValue(value) {
  return value === null || value === undefined ? '' : JSON.stringify(value)
}
