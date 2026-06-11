/**
 * Shared SignalR connection for document room features.
 * ONE connection per active document — shared between CollabCursorOverlay and FileCommentTab.
 * This avoids the "too many connections" problem that causes silent failures.
 */
import { ref, type Ref } from 'vue'
import * as signalR from '@microsoft/signalr'

type CursorPayload = {
  userId: string; userName: string; xPct: number; yPct: number; page: number
}
type ViewerPayload = { userId: string; userName: string; avatarUrl?: string }

// Singleton — one connection per app instance (same tab)
let hubConnection: signalR.HubConnection | null = null
let currentDocId: number | null = null

// Reactive state accessible by all consumers
const viewers = ref<Record<string, ViewerPayload & { color: string }>>({})
const cursors = ref<Record<string, CursorPayload & { color: string; lastSeen: number }>>({})

// Comment event callbacks registered by FileCommentTab
const onNewComment: Array<(cmt: any) => void> = []
const onCommentDeleted: Array<(id: number) => void> = []

// Annotation stroke callbacks registered by PdfViewer
type StrokePayload = { userId: string; pageNumber: number; strokeJson: string }
type ErasePayload  = { userId: string; pageNumber: number; strokesJson: string }
const onAnnotStroke: Array<(p: StrokePayload) => void> = []
const onAnnotErase:  Array<(p: ErasePayload) => void>  = []

function getColor(str: string): string {
  const colors = ['#e74c3c', '#3498db', '#2ecc71', '#f39c12', '#9b59b6', '#1abc9c', '#e67e22', '#e91e63']
  let hash = 0
  for (let i = 0; i < str.length; i++) hash = str.charCodeAt(i) + ((hash << 5) - hash)
  return colors[Math.abs(hash) % colors.length]
}

function buildConnection(apiBaseUrl: string, token: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/notificationHub`, {
      accessTokenFactory: () => localStorage.getItem('jwt_token') || token,
      transport: signalR.HttpTransportType.WebSockets
        | signalR.HttpTransportType.ServerSentEvents
        | signalR.HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.None)
    .build()
}

function registerHandlers(conn: signalR.HubConnection) {
  conn.on('RoomViewers', (list: ViewerPayload[]) => {
    viewers.value = {}
    for (const u of list)
      viewers.value[u.userId] = { ...u, color: getColor(u.userId) }
  })

  conn.on('UserJoined', (data: ViewerPayload) => {
    viewers.value[data.userId] = { ...data, color: getColor(data.userId) }
  })

  conn.on('UserLeft', (userId: string) => {
    delete viewers.value[userId]
    delete cursors.value[userId]
  })

  conn.on('CursorMoved', (data: CursorPayload) => {
    cursors.value[data.userId] = { ...data, color: getColor(data.userId), lastSeen: Date.now() }
  })

  conn.on('CursorLeft', (userId: string) => {
    delete cursors.value[userId]
  })

  conn.on('ReceiveNewComment', (cmt: any) => {
    onNewComment.forEach(fn => fn(cmt))
  })

  conn.on('CommentDeleted', (id: number) => {
    onCommentDeleted.forEach(fn => fn(id))
  })

  conn.on('AnnotationStroke', (data: StrokePayload) => {
    onAnnotStroke.forEach(fn => fn(data))
  })

  conn.on('AnnotationErased', (data: ErasePayload) => {
    onAnnotErase.forEach(fn => fn(data))
  })
}

export function useDocumentHub() {
  const config = useRuntimeConfig()

  async function connect(docId: number, avatarUrl?: string | null) {
    if (!process.client) return
    const token = localStorage.getItem('jwt_token')
    if (!token || !docId) return

    // Reuse connection if already in correct room
    if (hubConnection && currentDocId === docId &&
        hubConnection.state !== signalR.HubConnectionState.Disconnected) {
      return
    }

    // Switch room: leave old if needed
    if (hubConnection && currentDocId && currentDocId !== docId &&
        hubConnection.state === signalR.HubConnectionState.Connected) {
      await hubConnection.invoke('LeaveDocumentRoom', currentDocId).catch(() => {})
    }

    // Create new connection if needed
    if (!hubConnection || hubConnection.state === signalR.HubConnectionState.Disconnected) {
      viewers.value = {}
      cursors.value = {}

      hubConnection = buildConnection(config.public.apiBaseUrl as string, token)
      registerHandlers(hubConnection)

      hubConnection.onreconnected(() => {
        hubConnection?.invoke('JoinDocumentRoom', docId, avatarUrl ?? null).catch(() => {})
      })

      await hubConnection.start().catch(() => {})
    }

    currentDocId = docId
    await hubConnection.invoke('JoinDocumentRoom', docId, avatarUrl ?? null).catch(() => {})
  }

  async function disconnect(docId: number) {
    if (!hubConnection) return
    if (hubConnection.state === signalR.HubConnectionState.Connected) {
      await hubConnection.invoke('LeaveDocumentRoom', docId).catch(() => {})
    }
    hubConnection.stop()
    hubConnection = null
    currentDocId = null
    viewers.value = {}
    cursors.value = {}
  }

  function broadcastCursor(docId: number, xPct: number, yPct: number, page: number) {
    if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) return
    hubConnection.invoke('BroadcastCursor', docId, xPct, yPct, page).catch(() => {})
  }

  function leaveCursor(docId: number) {
    if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) return
    hubConnection.invoke('LeaveCursor', docId).catch(() => {})
  }

  function broadcastStroke(docId: number, pageNum: number, stroke: object) {
    if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) return
    hubConnection.invoke('BroadcastAnnotationStroke', docId, pageNum, JSON.stringify(stroke)).catch(() => {})
  }

  function broadcastErase(docId: number, pageNum: number, strokes: object[]) {
    if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) return
    hubConnection.invoke('BroadcastAnnotationErase', docId, pageNum, JSON.stringify(strokes)).catch(() => {})
  }

  function onComment(fn: (cmt: any) => void) {
    if (!onNewComment.includes(fn)) onNewComment.push(fn)
  }

  function onDelete(fn: (id: number) => void) {
    if (!onCommentDeleted.includes(fn)) onCommentDeleted.push(fn)
  }

  function offComment(fn: (cmt: any) => void) {
    const i = onNewComment.indexOf(fn)
    if (i !== -1) onNewComment.splice(i, 1)
  }

  function offDelete(fn: (id: number) => void) {
    const i = onCommentDeleted.indexOf(fn)
    if (i !== -1) onCommentDeleted.splice(i, 1)
  }

  function onStroke(fn: (p: StrokePayload) => void) {
    if (!onAnnotStroke.includes(fn)) onAnnotStroke.push(fn)
  }
  function offStroke(fn: (p: StrokePayload) => void) {
    const i = onAnnotStroke.indexOf(fn)
    if (i !== -1) onAnnotStroke.splice(i, 1)
  }
  function onErase(fn: (p: ErasePayload) => void) {
    if (!onAnnotErase.includes(fn)) onAnnotErase.push(fn)
  }
  function offErase(fn: (p: ErasePayload) => void) {
    const i = onAnnotErase.indexOf(fn)
    if (i !== -1) onAnnotErase.splice(i, 1)
  }

  return {
    viewers: viewers as Ref<Record<string, ViewerPayload & { color: string }>>,
    cursors: cursors as Ref<Record<string, CursorPayload & { color: string; lastSeen: number }>>,
    connect,
    disconnect,
    broadcastCursor,
    leaveCursor,
    broadcastStroke,
    broadcastErase,
    onComment,
    onDelete,
    offComment,
    offDelete,
    onStroke,
    offStroke,
    onErase,
    offErase,
    getColor,
  }
}
