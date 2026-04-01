<template>
  <div class="p-5">
    <h2>Test Real-time</h2>
    <p>Trạng thái kết nối: <strong>{{ connectionStatus }}</strong></p>
    
    <!-- 🔔 Notification -->
    <div class="mt-4">
      <h3>🔔 Notifications</h3>
      <ul class="list-disc pl-5">
        <li v-for="(noti, index) in notifications" :key="index">
          {{ noti }}
        </li>
      </ul>
    </div>

    <!-- 💬 Comment -->
    <div class="mt-4">
      <h3>Live Comments (File ID: 123)</h3>
      <ul class="list-disc pl-5">
        <li v-for="cmt in comments" :key="cmt.id">
          <b>{{ cmt.userName }}</b>: {{ cmt.content }} 
          <span class="text-xs text-gray-500">({{ cmt.createdAt }})</span>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

const connectionStatus = ref('Đang kết nối...')
const comments = ref([])
const notifications = ref([]) // 🔥 thêm cái này
let connection = null

onMounted(async () => {
  const token = localStorage.getItem("jwt_token")

  connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7084/notificationHub", {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build()

  // =========================
  // 💬 COMMENT
  // =========================
  connection.on("ReceiveNewComment", (newComment) => {
    console.log("CÓ COMMENT:", newComment)
    comments.value.push(newComment)

    // 🔔 thêm thông báo
    notifications.value.unshift(
      `${newComment.userName} vừa comment: ${newComment.content}`
    )
  })

  connection.on("CommentDeleted", (commentId) => {
    const target = comments.value.find(c => c.id === commentId)
    if (target) {
      target.content = "Bình luận này đã bị xóa."
      target.isDeleted = true
    }

    notifications.value.unshift(`Một comment đã bị xóa (ID: ${commentId})`)
  })

  // =========================
  // 📩 INVITATION
  // =========================
  connection.on("ReceiveNewInvitation", (data) => {
    console.log("INVITE:", data)

    notifications.value.unshift(
      `📩 ${data.inviterName} mời bạn vào workspace "${data.workspaceName}"`
    )
  })

  connection.on("InvitationResponded", (data) => {
    console.log("RESPOND:", data)

    notifications.value.unshift(
      `📢 ${data.message}`
    )
  })

  try {
    await connection.start()
    connectionStatus.value = 'Đã kết nối thành công!'

    // join room comment
    await connection.invoke("JoinDocumentRoom", 123)

    // (optional) join workspace nếu cần
    // await connection.invoke("JoinWorkspaceRoom", 1)

  } catch (err) {
    console.error("Lỗi:", err)
    connectionStatus.value = 'Kết nối thất bại!'
  }
})

onUnmounted(async () => {
  if (connection) {
    await connection.invoke("LeaveDocumentRoom", 123)
    await connection.stop()
  }
})
</script>