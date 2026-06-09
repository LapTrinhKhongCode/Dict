<template>
  <div class="space-y-4 border-t border-gray-200 dark:border-gray-700 pt-6 mt-6">
    <h3 class="font-semibold text-gray-800 dark:text-gray-200 flex items-center gap-2">
      <UIcon name="i-lucide-message-square" class="size-4" />
      <span>Bình luận</span>
      <span
        v-if="comments.length"
        class="text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 px-2 py-0.5 rounded-full"
      >{{ comments.length }}</span>
    </h3>

    <!-- Input box (chỉ khi đăng nhập) -->
    <div v-if="jwt" class="flex gap-2">
      <textarea
        v-model="newContent"
        :maxlength="1000"
        rows="2"
        placeholder="Viết bình luận của bạn..."
        class="flex-1 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-primary-500"
        @keydown.ctrl.enter="submitComment"
      />
      <button
        @click="submitComment"
        :disabled="!newContent.trim() || submitting"
        class="self-end px-4 py-2 text-sm font-medium bg-primary-600 hover:bg-primary-700 disabled:opacity-50 text-white rounded-lg transition-colors"
      >
        <UIcon v-if="submitting" name="i-lucide-loader-circle" class="size-4 animate-spin" />
        <span v-else>Gửi</span>
      </button>
    </div>
    <p v-else class="text-sm text-gray-500 dark:text-gray-400 italic">
      <NuxtLink to="/login" class="text-primary-600 hover:underline">Đăng nhập</NuxtLink> để bình luận.
    </p>

    <!-- Comment list -->
    <div v-if="loading" class="flex justify-center py-4">
      <UIcon name="i-lucide-loader-circle" class="size-5 animate-spin text-gray-400" />
    </div>

    <div v-else-if="comments.length === 0" class="text-sm text-gray-400 dark:text-gray-500 text-center py-4">
      Chưa có bình luận nào. Hãy là người đầu tiên!
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="c in comments"
        :key="c.id"
        class="group flex gap-3"
      >
        <div class="size-8 rounded-full bg-primary-100 dark:bg-primary-900 text-primary-700 dark:text-primary-300 flex items-center justify-center text-xs font-bold shrink-0">
          {{ c.userName?.charAt(0)?.toUpperCase() ?? '?' }}
        </div>
        <div class="flex-1 min-w-0">
          <div class="flex items-baseline gap-2">
            <span class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ c.userName }}</span>
            <span class="text-xs text-gray-400">{{ formatDate(c.createdAt) }}</span>
          </div>
          <p class="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap break-words mt-0.5">{{ c.content }}</p>
        </div>
        <button
          v-if="c.userId === currentUserId"
          @click="deleteComment(c.id)"
          class="opacity-0 group-hover:opacity-100 self-start text-gray-400 hover:text-red-500 transition-all"
          title="Xóa"
        >
          <UIcon name="i-lucide-trash-2" class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useJwt } from '~/composables/useJwt'
import { useToast } from '@/composables/useToast'

const props = defineProps<{ wordLabel: string }>()

interface CommentItem {
  id: number
  userId: number
  userName: string
  content: string
  createdAt: string
}

const { jwt } = useJwt()
const { showToast } = useToast()
const config = useRuntimeConfig()

const comments = ref<CommentItem[]>([])
const loading = ref(false)
const newContent = ref('')
const submitting = ref(false)

// Parse userId from JWT payload
const currentUserId = computed<number | null>(() => {
  if (!jwt.value) return null
  try {
    const payload = JSON.parse(atob(jwt.value.split('.')[1]))
    return parseInt(payload.userId ?? payload.sub ?? '0')
  } catch {
    return null
  }
})

async function loadComments() {
  if (!props.wordLabel) return
  loading.value = true
  try {
    const data: any = await $fetch(`${config.public.apiBase}/api/word-comments`, {
      params: { word: props.wordLabel }
    })
    comments.value = data?.result ?? []
  } catch {
    comments.value = []
  } finally {
    loading.value = false
  }
}

async function submitComment() {
  const content = newContent.value.trim()
  if (!content || submitting.value) return
  submitting.value = true
  try {
    const data: any = await $fetch(`${config.public.apiBase}/api/word-comments`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${jwt.value}` },
      body: { wordLabel: props.wordLabel, content }
    })
    if (data?.isSuccess) {
      comments.value.push(data.result)
      newContent.value = ''
    } else {
      showToast(data?.message ?? 'Lỗi gửi bình luận', 'error')
    }
  } catch {
    showToast('Lỗi gửi bình luận', 'error')
  } finally {
    submitting.value = false
  }
}

async function deleteComment(id: number) {
  try {
    const data: any = await $fetch(`${config.public.apiBase}/api/word-comments/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${jwt.value}` }
    })
    if (data?.isSuccess) {
      comments.value = comments.value.filter(c => c.id !== id)
    } else {
      showToast(data?.message ?? 'Không thể xóa', 'error')
    }
  } catch {
    showToast('Lỗi xóa bình luận', 'error')
  }
}

function formatDate(iso: string) {
  const d = new Date(iso)
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

watch(() => props.wordLabel, loadComments, { immediate: true })
</script>
