// utils/convert-radical-json.ts
import type { BothGraphData, GraphData, NodeObject, LinkObject } from '~/types/graph'

export type RadicalRaw = Record<string, { in?: string[]; out?: string[] }>

/**
 * convertRadicalJson(raw, opts)
 * - raw: your JSON mapping id -> { in: [], out: [] }
 * - opts.kanjiInfoMap: optional map id -> jishoData (attached to node.data.jishoData)
 * - opts.limitNodes: optional limit for number of nodes (slice after collect)
 */
export function convertRadicalJson(
  raw: RadicalRaw,
  opts?: {
    kanjiInfoMap?: Record<string, any>
    limitNodes?: number
  }
): BothGraphData {
  const kanjiInfoMap = opts?.kanjiInfoMap ?? {}
  // collect all ids from keys and in/out arrays
  const ids = new Set<string>()
  for (const [k, v] of Object.entries(raw)) {
    ids.add(String(k))
    if (Array.isArray(v?.in)) v.in.forEach((id) => ids.add(String(id)))
    if (Array.isArray(v?.out)) v.out.forEach((id) => ids.add(String(id)))
  }

  let idsArr = Array.from(ids)
  if (opts?.limitNodes && opts.limitNodes > 0) idsArr = idsArr.slice(0, opts.limitNodes)
  const allowed = new Set(idsArr)

  const nodes: NodeObject[] = idsArr.map((id) => ({
    id,
    data: { jishoData: kanjiInfoMap[id] ?? undefined },
  }))

  const linkSet = new Set<string>()
  const links: LinkObject[] = []
  for (const [src, v] of Object.entries(raw)) {
    if (!allowed.has(src)) continue
    const outs = Array.isArray(v?.out) ? v.out : []
    for (const t of outs) {
      const tgt = String(t)
      if (!allowed.has(tgt)) continue
      const key = `${src}=>${tgt}`
      if (!linkSet.has(key)) {
        linkSet.add(key)
        links.push({ source: src, target: tgt })
      }
    }
  }

  const withOutLinks: GraphData = { nodes, links }
  const noOutLinks: GraphData = { nodes: nodes.map((n) => ({ ...n })), links: [] }
  return { withOutLinks, noOutLinks }
}

/**
 * buildSubgraphAround(rootId, raw, depth, maxNodes)
 * BFS expand from root using both in/out, returns filtered raw suitable for convertRadicalJson
 */
export function buildSubgraphAround(rootId: string, raw: RadicalRaw, depth = 2, maxNodes = 500): RadicalRaw {
  if (!raw[rootId]) {
    // if root not present, just include root as an isolated node
    return { [rootId]: { in: [], out: [] } }
  }

  const seen = new Set<string>([rootId])
  let queue = [rootId]
  let d = 0

  while (queue.length > 0 && seen.size < maxNodes && d < depth) {
    const next: string[] = []
    for (const id of queue) {
      const node = raw[id]
      if (!node) continue
      for (const out of (node.out ?? [])) {
        if (!seen.has(out)) {
          seen.add(out)
          next.push(out)
        }
      }
      for (const inn of (node.in ?? [])) {
        if (!seen.has(inn)) {
          seen.add(inn)
          next.push(inn)
        }
      }
      if (seen.size >= maxNodes) break
    }
    queue = next
    d++
  }

  const filtered: RadicalRaw = {}
  for (const id of seen) {
    filtered[id] = raw[id] ?? { in: [], out: [] }
  }
  return filtered
}
