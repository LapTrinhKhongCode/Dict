// types/graph.ts
export type NodeObject = {
  id: string
  x?: number
  y?: number
  data?: Record<string, unknown>
  [k: string]: unknown
}
export type LinkObject = {
  source: string | NodeObject
  target: string | NodeObject
  [k: string]: unknown
}
export type GraphData = { nodes: NodeObject[]; links: LinkObject[] }
export type BothGraphData = { withOutLinks: GraphData; noOutLinks: GraphData }
