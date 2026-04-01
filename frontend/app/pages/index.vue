<template>
  <div class="max-w-[1200px] mx-auto w-full font-sans text-gray-800 dark:text-[#c9d1d9] pb-10">
    
    <div class="flex flex-col sm:flex-row sm:items-center justify-between mb-8 pb-4 border-b border-gray-200 dark:border-[#30363d] gap-4">
      <div>
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white">Tổng quan</h2>
        <div class="flex items-center gap-2 mt-2">
          <span class="text-sm text-gray-500 dark:text-[#8b949e]">Workspace:</span>
          <select v-model="activeWsId" @change="onWorkspaceChange" 
            class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] text-sm rounded-lg px-2 py-1 outline-none text-gray-700 dark:text-[#c9d1d9] focus:ring-2 focus:ring-[#f0c040] transition-colors cursor-pointer">
            <option v-if="loadingWs" value="" disabled>Đang tải...</option>
            <option v-for="ws in workspaces" :key="ws.id" :value="ws.id">{{ ws.name }}</option>
          </select>
        </div>
      </div>

      <div class="flex gap-3">
        <button @click="openUploadModal" class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-[#21262d] border border-gray-200 dark:border-[#30363d] text-gray-700 dark:text-[#c9d1d9] rounded-lg text-sm font-medium hover:bg-gray-100 dark:hover:bg-[#30363d] shadow-sm transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" /></svg>
          Upload tài liệu
        </button>
        <button @click="openCreateProjectModal" class="flex items-center gap-2 px-4 py-2 bg-[#f0c040] text-black rounded-lg text-sm font-bold hover:bg-[#e3b330] shadow-sm transition-colors">
          <span>+</span> Dự án mới
        </button>
      </div>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-5 shadow-sm">
        <p class="text-3xl font-bold text-gray-900 dark:text-white mb-1">{{ projects.length }}</p>
        <p class="text-sm text-gray-500 dark:text-[#8b949e]">Dự án đang mở</p>
      </div>
      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-5 shadow-sm">
        <p class="text-3xl font-bold text-gray-900 dark:text-white mb-1">{{ recentSearches.length }}</p>
        <p class="text-sm text-gray-500 dark:text-[#8b949e]">Từ vựng đã tra</p>
      </div>
      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-5 shadow-sm">
        <p class="text-3xl font-bold text-gray-900 dark:text-white mb-1">{{ recentFiles.length }}</p>
        <p class="text-sm text-gray-500 dark:text-[#8b949e]">Tài liệu trong WS</p>
      </div>
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-3 lg:grid-cols-2 gap-6 mb-8">
      
      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-5 shadow-sm flex flex-col h-[400px]">
        <div class="flex justify-between items-center mb-4 flex-shrink-0">
          <h3 class="font-bold text-gray-900 dark:text-white">Dự án gần đây</h3>
        </div>
        
        <div v-if="loadingProjects" class="space-y-2 flex-1 overflow-hidden">
          <div v-for="i in 3" :key="i" class="h-12 bg-gray-100 dark:bg-[#0d1117] animate-pulse rounded-lg"></div>
        </div>
        <div v-else class="space-y-3 flex-1 overflow-y-auto pr-1">
          <div v-for="p in projects" :key="p.id" @click="goToProject(p.id)" class="flex items-center justify-between p-3 bg-gray-50 dark:bg-[#0d1117] border border-gray-100 dark:border-[#30363d] rounded-lg cursor-pointer hover:border-gray-300 dark:hover:border-[#8b949e] transition-colors group">
            <div class="flex items-center gap-3 flex-1 min-w-0">
              <div class="w-8 h-8 rounded bg-blue-100 dark:bg-[#1f6feb]/20 text-blue-600 dark:text-[#58a6ff] flex items-center justify-center flex-shrink-0">
                <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20"><path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" /></svg>
              </div>
              <span class="font-medium text-gray-800 dark:text-[#c9d1d9] truncate group-hover:text-blue-500 dark:group-hover:text-[#58a6ff] transition-colors">{{ p.name }}</span>
            </div>
            <span class="text-xs text-green-700 dark:text-[#3fb950] bg-green-100 dark:bg-[#238636]/20 px-2 py-1 rounded flex-shrink-0 ml-2">{{ p.mediaCount || 0 }} file</span>
          </div>
          <div v-if="projects.length === 0" class="text-center py-6 text-sm text-gray-500 dark:text-[#8b949e]">Chưa có dự án nào</div>
        </div>
      </div>

      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-5 shadow-sm flex flex-col h-[400px]">
        <div class="flex justify-between items-center mb-4 flex-shrink-0">
          <h3 class="font-bold text-gray-900 dark:text-white">Tất cả tài liệu WS</h3>
        </div>
        
        <div v-if="loadingRecentFiles" class="space-y-2 flex-1 overflow-hidden">
          <div v-for="i in 4" :key="i" class="h-14 bg-gray-100 dark:bg-[#0d1117] animate-pulse rounded-lg"></div>
        </div>
        <div v-else class="space-y-3 flex-1 overflow-y-auto pr-1">
           <div v-for="file in recentFiles" :key="file.id" @click="goToReader(file)" class="flex items-center justify-between p-3 bg-gray-50 dark:bg-[#0d1117] border border-gray-100 dark:border-[#30363d] rounded-lg cursor-pointer hover:border-gray-300 dark:hover:border-[#8b949e] transition-colors group">
            
            <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
              <div class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0" :class="file.type === 'pdf' ? 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400' : 'bg-gray-200 dark:bg-[#30363d] text-gray-700 dark:text-[#c9d1d9]'">
                <svg v-if="file.type === 'pdf'" class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24"><path d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z"/></svg>
                <span v-else class="text-[9px] font-black uppercase">{{ file.type || 'IMG' }}</span>
              </div>
              
              <div class="flex flex-col flex-1 min-w-0">
                <span class="font-medium text-sm text-gray-800 dark:text-[#c9d1d9] truncate block w-full group-hover:text-blue-500 dark:group-hover:text-[#58a6ff] transition-colors" :title="file.name">{{ file.name }}</span>
                <span class="text-[10px] text-gray-500 dark:text-[#8b949e] truncate block w-full mt-0.5">
                  Dự án: {{ file.projectName }}
                </span>
              </div>
            </div>
            
            <div class="flex flex-col items-end gap-1 flex-shrink-0 ml-2">
              <span class="text-[10px] text-gray-500 dark:text-[#8b949e]">{{ formatDate(file.createdAt) }}</span>
            </div>
          </div>

          <div v-if="recentFiles.length === 0" class="text-center py-8 text-sm text-gray-500 dark:text-[#8b949e]">
            Chưa có tài liệu nào trong WS.
          </div>
        </div>
      </div>

      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-5 shadow-sm flex flex-col h-[400px]">
        <div class="flex justify-between items-center mb-4 flex-shrink-0">
          <h3 class="font-bold text-gray-900 dark:text-white">Từ vựng tra gần đây</h3>
          <button @click="clearSearches" v-if="recentSearches.length > 0" class="text-xs text-red-500 hover:text-red-600 dark:text-red-400 hover:underline">Xóa lịch sử</button>
        </div>
        
        <div class="space-y-3 flex-1 overflow-y-auto pr-1">
          <div v-for="search in recentSearches" :key="search.word" @click="searchWord(search.word)" 
            class="flex items-center justify-between p-3 bg-gray-50 dark:bg-[#0d1117] border border-gray-100 dark:border-[#30363d] rounded-lg cursor-pointer hover:border-gray-300 dark:hover:border-[#8b949e] transition-colors group">
            <div class="flex flex-col flex-1 min-w-0 pr-2">
              <div class="flex items-center gap-2">
                <span class="text-base font-bold text-gray-900 dark:text-white">{{ search.word }}</span>
                <span class="text-xs text-gray-500 dark:text-gray-400">[{{ search.phonetic }}]</span>
              </div>
              <span class="text-[11px] text-gray-600 dark:text-gray-300 truncate w-full mt-0.5">{{ search.short_mean }}</span>
            </div>
            <button @click.stop="removeSearch(search.word)" class="p-1.5 text-gray-400 hover:text-red-500 dark:text-[#8b949e] dark:hover:text-red-400 rounded-full hover:bg-gray-200 dark:hover:bg-[#30363d] transition-colors flex-shrink-0">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <div v-if="recentSearches.length === 0" class="flex flex-col items-center justify-center py-10 text-gray-400 dark:text-[#8b949e]">
            <svg class="w-12 h-12 mb-3 opacity-20" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
            <p class="text-sm">Chưa có lịch sử tra từ</p>
          </div>
        </div>
      </div>

    </div>

    <div>
      <p class="text-xs font-bold text-gray-500 dark:text-[#8b949e] uppercase tracking-wider mb-3">Thao tác nhanh</p>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div @click="$router.push('/explore')" class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-4 flex justify-between items-start cursor-pointer shadow-sm hover:bg-gray-50 dark:hover:bg-[#21262d] transition-colors group">
          <div>
            <svg class="w-5 h-5 text-gray-400 dark:text-[#8b949e] mb-2 group-hover:text-blue-500 dark:group-hover:text-white transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" /></svg>
            <p class="font-bold text-gray-900 dark:text-white text-sm">Ôn flashcard</p>
            <p class="text-xs text-gray-500 dark:text-[#8b949e] mt-1">Khám phá & ôn tập từ vựng</p>
          </div>
          <div class="w-6 h-6 rounded-full bg-gray-100 dark:bg-[#30363d] flex items-center justify-center text-gray-600 dark:text-[#c9d1d9] group-hover:bg-blue-100 dark:group-hover:bg-blue-900/30 transition-colors">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
          </div>
        </div>

        <div @click="$router.push('/sensei')" class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl p-4 flex justify-between items-start cursor-pointer shadow-sm hover:bg-gray-50 dark:hover:bg-[#21262d] transition-colors group">
          <div>
            <svg class="w-5 h-5 text-gray-400 dark:text-[#8b949e] mb-2 group-hover:text-green-500 dark:group-hover:text-white transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
            <p class="font-bold text-gray-900 dark:text-white text-sm">Chat với Sensei</p>
            <p class="text-xs text-gray-500 dark:text-[#8b949e] mt-1">Hỏi đáp & luyện tập hội thoại</p>
          </div>
          <div class="w-6 h-6 rounded-full bg-gray-100 dark:bg-[#30363d] flex items-center justify-center text-gray-600 dark:text-[#c9d1d9] group-hover:bg-green-100 dark:group-hover:bg-green-900/30 transition-colors">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
          </div>
        </div>
      </div>
    </div>

    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showCreateProject" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 backdrop-blur-sm p-4" @click.self="showCreateProject = false">
          <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] p-6 rounded-2xl w-full max-w-md shadow-2xl">
            <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo dự án mới</h3>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Tên dự án <span class="text-red-500">*</span></label>
                <input v-model="projectForm.name" type="text" placeholder="VD: Tài liệu học thi N3..." 
                  class="w-full bg-gray-50 dark:bg-[#0d1117] border border-gray-300 dark:border-[#30363d] rounded-lg px-4 py-2.5 text-sm text-gray-900 dark:text-white outline-none focus:ring-2 focus:ring-[#f0c040] focus:border-transparent transition-all">
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
                <textarea v-model="projectForm.description" rows="2"
                  class="w-full bg-gray-50 dark:bg-[#0d1117] border border-gray-300 dark:border-[#30363d] rounded-lg px-4 py-2.5 text-sm text-gray-900 dark:text-white outline-none focus:ring-2 focus:ring-[#f0c040] transition-all resize-none"
                  placeholder="Mô tả ngắn..."></textarea>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Thuộc Workspace <span class="text-red-500">*</span></label>
                <select v-model="projectForm.workspaceId" 
                  class="w-full bg-gray-50 dark:bg-[#0d1117] border border-gray-300 dark:border-[#30363d] rounded-lg px-4 py-2.5 text-sm text-gray-900 dark:text-white outline-none focus:ring-2 focus:ring-[#f0c040] focus:border-transparent transition-all">
                  <option value="" disabled>-- Chọn Workspace --</option>
                  <option v-for="ws in workspaces" :key="ws.id" :value="ws.id">{{ ws.name }}</option>
                </select>
              </div>
            </div>
            <div class="flex justify-end gap-3 mt-8">
              <button @click="showCreateProject = false" class="px-5 py-2.5 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-[#21262d] transition-colors">Hủy</button>
              <button @click="handleCreateProject" :disabled="!projectForm.name || !projectForm.workspaceId || creatingProject" 
                class="px-5 py-2.5 rounded-lg bg-[#f0c040] text-black text-sm font-bold hover:bg-[#e3b330] shadow-sm disabled:opacity-50 disabled:cursor-not-allowed transition-all">
                {{ creatingProject ? 'Đang tạo...' : 'Xác nhận tạo' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showUploadModal" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 backdrop-blur-sm p-4" @click.self="showUploadModal = false">
          <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] p-6 rounded-2xl w-full max-w-md shadow-2xl">
            <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-6 flex items-center gap-2">
              <svg class="w-5 h-5 text-[#f0c040]" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" /></svg>
              Upload tài liệu mới
            </h3>
            
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">1. Chọn Workspace</label>
                <select v-model="uploadForm.workspaceId" @change="onUploadWsChange" 
                  class="w-full bg-gray-50 dark:bg-[#0d1117] border border-gray-300 dark:border-[#30363d] rounded-lg px-4 py-2.5 text-sm text-gray-900 dark:text-white outline-none focus:ring-2 focus:ring-[#f0c040] transition-all">
                  <option value="" disabled>-- Chọn Workspace --</option>
                  <option v-for="ws in workspaces" :key="ws.id" :value="ws.id">{{ ws.name }}</option>
                </select>
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">2. Chọn Dự án lưu trữ</label>
                <select v-model="uploadForm.projectId" :disabled="!uploadForm.workspaceId || isFetchingUploadProjects"
                  class="w-full bg-gray-50 dark:bg-[#0d1117] border border-gray-300 dark:border-[#30363d] rounded-lg px-4 py-2.5 text-sm text-gray-900 dark:text-white outline-none focus:ring-2 focus:ring-[#f0c040] transition-all disabled:opacity-50 disabled:cursor-not-allowed">
                  <option value="" disabled>-- Chọn Dự án --</option>
                  <option v-for="p in uploadProjects" :key="p.id" :value="p.id">{{ p.name }}</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">3. Tệp tài liệu</label>
                <div class="relative">
                  <input type="file" @change="onFileSelected" accept="image/*,application/pdf" class="absolute inset-0 w-full h-full opacity-0 cursor-pointer z-10" />
                  <div class="w-full border-2 border-dashed border-gray-300 dark:border-[#30363d] rounded-lg p-4 text-center hover:border-[#f0c040] transition-colors bg-gray-50 dark:bg-[#0d1117] flex flex-col items-center justify-center">
                    <p v-if="uploadForm.file" class="text-sm font-bold text-blue-600 dark:text-[#58a6ff] w-full truncate px-2">{{ uploadForm.file.name }}</p>
                    <div v-else class="text-sm text-gray-500 dark:text-[#8b949e]">
                      <span class="text-[#f0c040] font-semibold underline">Chọn tệp</span> hoặc kéo thả vào đây
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="flex justify-end gap-3 mt-8">
              <button @click="showUploadModal = false" class="px-5 py-2.5 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-[#21262d] transition-colors">Hủy</button>
              <button @click="handleConfirmUpload" :disabled="!uploadForm.projectId || !uploadForm.file || uploadingFile" 
                class="px-5 py-2.5 rounded-lg bg-[#f0c040] text-black text-sm font-bold hover:bg-[#e3b330] shadow-sm disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center gap-2">
                <svg v-if="uploadingFile" class="w-4 h-4 animate-spin text-black" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/></svg>
                {{ uploadingFile ? 'Đang Upload...' : 'Upload ngay' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <Transition name="toast-fade">
        <div v-if="toast.visible" class="fixed inset-0 z-[10000] flex items-center justify-center pointer-events-none px-4">
          <div class="bg-[#1c2128] border p-5 rounded-2xl shadow-2xl flex flex-col items-center max-w-sm text-center pointer-events-auto transition-colors"
            :class="toast.type === 'error' ? 'border-red-500/50' : 'border-blue-500/50'">
            <p class="text-white font-bold">{{ toast.message }}</p>
          </div>
        </div>
      </Transition>
    </Teleport>

  </div>
</template>

<script setup>
definePageMeta({ 
  layout: 'default', 
  ssr: false,
  middleware: 'auth-client' // Đảm bảo có middleware bảo vệ route
})

import { ref, reactive, onMounted, watch } from 'vue' // 1. Import thêm watch
import { useRouter } from 'vue-router'
import { useWorkspace } from '~/composables/useWorkspace'
import { useProject } from '~/composables/useProject'
import { useRecentSearches } from '~/composables/useRecentSearches'
import { useJwt } from '~/composables/useJwt' // 2. Import useJwt

const router = useRouter()
const config = useRuntimeConfig()
const { getMyWorkspaces } = useWorkspace()
const { getProjects, createProject } = useProject()
const { jwt, isAuthenticated } = useJwt() // 3. Lấy trạng thái Auth

// Lịch sử tra từ vựng
const { recentSearches, removeSearch, clearSearches } = useRecentSearches()

const getToken = () => localStorage.getItem('jwt_token') || jwt.value || ''

// 4. WATCHER: Bắt sự kiện đăng xuất (Token rỗng)
watch(isAuthenticated, (newVal) => {
  if (!newVal) {
    router.push('/login')
  }
}, { immediate: true })

// --- TOAST ---
const toast = reactive({ visible: false, message: '', type: 'success' })
function showToast(msg, type = 'success') {
  toast.message = msg; toast.type = type; toast.visible = true;
  setTimeout(() => toast.visible = false, 3000)
}

// --- STATES CHÍNH ---
const loadingWs = ref(true)
const workspaces = ref([])
const activeWsId = ref('') // Lưu id của workspace đang chọn từ dropdown

const loadingProjects = ref(false)
const projects = ref([])

const loadingRecentFiles = ref(false)
const recentFiles = ref([])

// --- HÀM THAY ĐỔI WORKSPACE TỪ DROPDOWN ---
async function onWorkspaceChange() {
  if (!activeWsId.value) return;
  
  loadingProjects.value = true;
  projects.value = await getProjects(activeWsId.value);
  loadingProjects.value = false;

  await fetchRecentFilesForWorkspace(projects.value);
}

// ==========================================
// --- LOGIC TẠO PROJECT ---
// ==========================================
const showCreateProject = ref(false)
const creatingProject = ref(false)
const projectForm = reactive({ name: '', description: '', workspaceId: '' })

function openCreateProjectModal() {
  projectForm.name = ''
  projectForm.description = ''
  projectForm.workspaceId = activeWsId.value || (workspaces.value.length > 0 ? workspaces.value[0].id : '')
  showCreateProject.value = true
}

async function handleCreateProject() {
  if (!projectForm.name.trim() || !projectForm.workspaceId) return
  
  creatingProject.value = true
  try {
    const wsId = Number(projectForm.workspaceId);
    const payload = {
      name: projectForm.name.trim(),
      description: projectForm.description || '' 
    };

    const p = await createProject(wsId, payload)
    
    if (activeWsId.value && activeWsId.value === projectForm.workspaceId) {
      projects.value.unshift(p)
    }
    
    showCreateProject.value = false
    showToast("Tạo dự án thành công", "success")
    goToProject(p.id)
    
  } catch (error) {
    console.error("CHI TIẾT LỖI TẠO DỰ ÁN:", error);
    showToast("Lỗi tạo dự án! Vui lòng kiểm tra Console (F12).", "error")
  } finally {
    creatingProject.value = false
  }
}
// ==========================================

// --- LOGIC UPLOAD MODAL ---
const showUploadModal = ref(false)
const uploadingFile = ref(false)
const isFetchingUploadProjects = ref(false)
const uploadProjects = ref([])
const uploadForm = reactive({ workspaceId: '', projectId: '', file: null })

function openUploadModal() {
  uploadForm.workspaceId = activeWsId.value || (workspaces.value.length > 0 ? workspaces.value[0].id : '')
  uploadForm.projectId = ''
  uploadForm.file = null
  onUploadWsChange() 
  showUploadModal.value = true
}

async function onUploadWsChange() {
  uploadForm.projectId = ''
  uploadProjects.value = []
  if (!uploadForm.workspaceId) return
  isFetchingUploadProjects.value = true
  try {
    uploadProjects.value = await getProjects(Number(uploadForm.workspaceId))
  } catch (error) {
    console.error(error)
  } finally {
    isFetchingUploadProjects.value = false
  }
}

function onFileSelected(e) {
  if (e.target.files && e.target.files.length > 0) uploadForm.file = e.target.files[0]
}

async function handleConfirmUpload() {
  if (!uploadForm.projectId || !uploadForm.file) return
  const token = getToken()
  uploadingFile.value = true

  const formData = new FormData()
  formData.append('image', uploadForm.file)
  formData.append('projectId', uploadForm.projectId.toString())

  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Infer/upload-and-infer?saveAnnotated=false`, {
      method: 'POST', body: formData, headers: { Authorization: `Bearer ${token}` }
    })
    
    if (!res.ok) throw new Error("Upload thất bại")
    const jobData = await res.json()
    
    sessionStorage.setItem(`ocr_view_meta_${jobData.jobId}`, JSON.stringify({ jobId: jobData.jobId, imageUrl: jobData.imageUrl }))
    
    showUploadModal.value = false
    showToast("Tải lên tài liệu thành công!", "success")

    if (activeWsId.value && activeWsId.value === uploadForm.workspaceId) {
      projects.value = await getProjects(activeWsId.value)
      await fetchRecentFilesForWorkspace(projects.value)
    }

  } catch (error) {
    showToast("Có lỗi xảy ra khi upload.", "error")
  } finally {
    uploadingFile.value = false
  }
}

// --- LOGIC LOAD DỮ LIỆU ---
async function loadInitialData() {
  // 5. Kiểm tra an toàn trước khi gọi API
  if (!isAuthenticated.value) return;

  loadingWs.value = true
  try {
    workspaces.value = await getMyWorkspaces()
    if (workspaces.value.length > 0) {
      activeWsId.value = workspaces.value[0].id
      await onWorkspaceChange()
    }
  } catch (e) {
    console.error(e)
    // Nếu token lỗi 401 từ Server, đá văng luôn
    if (e.response?.status === 401) {
      router.push('/login')
    }
  } finally {
    loadingWs.value = false
  }
}

async function fetchRecentFilesForWorkspace(projectList) {
  loadingRecentFiles.value = true
  recentFiles.value = []
  
  if (!projectList || projectList.length === 0) {
    loadingRecentFiles.value = false
    return
  }

  try {
    const token = getToken()
    const promises = projectList.map(async (p) => {
      try {
        const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${p.id}/files`, {
          headers: { Authorization: `Bearer ${token}` }
        })
        if (res.ok) {
          const filesData = await res.json()
          const arr = Array.isArray(filesData) ? filesData : (filesData?.data || filesData?.result || [])
          return arr.map(f => ({ ...f, projectName: p.name, projectId: p.id }))
        }
      } catch (err) {
        console.error(`Lỗi lấy file dự án ${p.id}:`, err)
      }
      return []
    })

    const allFilesArrays = await Promise.all(promises)
    let combinedFiles = allFilesArrays.flat()

    combinedFiles.sort((a, b) => {
      const timeA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
      const timeB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
      return timeB - timeA;
    })

    recentFiles.value = combinedFiles
  } catch (error) {
    console.error("Lỗi gom tài liệu:", error)
  } finally {
    loadingRecentFiles.value = false
  }
}

// --- ĐIỀU HƯỚNG VÀ FORMAT ---
function goToProject(id) { router.push(`/workspaces/project/${id}`) }

function goToReader(file) {
  const name = encodeURIComponent(file.name || '')
  router.push(`/reader?jobId=${file.id}&projectId=${file.projectId}&name=${name}`)
}

function searchWord(word) {
  router.push({ path: '/search', query: { q: word } })
}

function formatDate(d) {
  if (!d) return ''
  const date = new Date(d)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  if (diff < 1000 * 60 * 60) return `${Math.floor(diff / 60000)} phút trước`
  if (diff < 1000 * 60 * 60 * 24) return `${Math.floor(diff / 3600000)} giờ trước`
  return date.toLocaleDateString('vi-VN')
}

onMounted(loadInitialData)
</script>

<style scoped>
/* Style cho Scrollbar Custom */
.overflow-y-auto::-webkit-scrollbar { width: 4px; }
.overflow-y-auto::-webkit-scrollbar-track { background: transparent; }
.overflow-y-auto::-webkit-scrollbar-thumb { background: #d1d5db; border-radius: 4px; }
.dark .overflow-y-auto::-webkit-scrollbar-thumb { background: #30363d; }
.overflow-y-auto::-webkit-scrollbar-thumb:hover { background: #9ca3af; }
.dark .overflow-y-auto::-webkit-scrollbar-thumb:hover { background: #484f58; }

/* Các Transition CSS cơ bản */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }  
.toast-fade-enter-active { transition: all 0.3s ease; }
.toast-fade-leave-active { transition: all 0.2s ease; }
.toast-fade-enter-from { opacity: 0; transform: translateY(20px); }
.toast-fade-leave-to { opacity: 0; transform: scale(0.9); }
</style>