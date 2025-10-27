<template>
  <div
    class="bg-gray-800 rounded-lg p-5 flex flex-col justify-between h-48 cursor-pointer transition-transform transform hover:-translate-y-1 hover:shadow-lg hover:shadow-sky-500/10 group relative"
    @click="emit('select-set', deck.id)"
  >
    <!-- Card Content -->
    <div>
      <h3 class="text-xl font-bold text-gray-100 truncate mb-1">{{ deck.name }}</h3>
      <p class="text-lg text-gray-400">{{ deck.cardCount }} từ</p>
      <p v-if="deck.description" class="text-bs text-gray-500 mt-1 truncate">{{ deck.description }}</p>
    </div>

    <!-- Author Info -->
    <div class="flex items-center justify-between mt-4">
      <div class="flex items-center overflow-hidden mr-2">
        <img v-if="deck.nowAuthorImageUrl || deck.authorImageUrl" 
             :src="deck.nowAuthorImageUrl || deck.authorImageUrl" 
             alt="Avatar" 
             class="w-6 h-6 rounded-full mr-2 bg-gray-700 flex-shrink-0 object-cover"
             onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';"
        />
        <!-- Fallback Initials/Icon -->
         <div v-if="deck.authorName!=currentUserName" class="w-6 h-6 rounded-full mr-2 bg-gray-700 flex-shrink-0 items-center justify-center text-xs font-bold" 
              :style="{ display: (deck.nowAuthorImageUrl || deck.authorImageUrl) ? 'none' : 'flex' }">
              {{ getInitials(deck.nowAuthorName || deck.authorName) }}
         </div>

       <span v-if="deck.authorName!=currentUserName" class="text-sm text-gray-300 truncate">{{ deck.nowAuthorName || deck.authorName }}</span>
      </div>
    </div>

    <!-- Save Button -->
    <button
      v-if="variant === 'explore' && deck.authorName !== currentUserName"
      @click.stop="handleSave"
      :disabled="isSaving"
      class="absolute top-2 right-2 p-2 bg-sky-600 hover:bg-sky-700 rounded-full text-white opacity-0 group-hover:opacity-100 focus:opacity-100 transition-opacity duration-200 disabled:bg-gray-500 disabled:cursor-not-allowed z-10"
      title="Lưu vào Sổ tay"
    >
      <svg v-if="isSaving" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path d="M5 4a2 2 0 012-2h6a2 2 0 012 2v14l-5-2.5L5 18V4z" />
      </svg>
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { DeckSummaryDto } from '~/types';
import { useJwt } from '~/composables/useJwt'; // Import composable

// Get authentication status
const { isAuthenticated } = useJwt();

const props = defineProps<{
  deck: DeckSummaryDto;
  variant: 'my' | 'explore';
  currentUserId: number;
  currentUserName: string;
}>();

// Emit the new event
const emit = defineEmits(['select-set', 'save-deck', 'show-login-notice']);

const isSaving = ref(false);

function handleSave() {
  if (isSaving.value) return;

  // Check authentication
  if (!isAuthenticated.value) {
    emit('show-login-notice'); // Emit notice event if not logged in
    return; // Stop execution
  }

  // Proceed if authenticated
  isSaving.value = true;
  emit('save-deck', props.deck.id);
  // Reset loading state - Parent should ideally control this based on API response
  // Keeping timeout here for simplicity as per previous code
  setTimeout(() => { isSaving.value = false; }, 1500);
}

// Helper to get initials
function getInitials(name: string | null | undefined): string {
    if (!name) return '?';
    const parts = name.split(' ').filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].substring(0, 1).toUpperCase();
    return (parts[0].substring(0, 1) + parts[parts.length - 1].substring(0, 1)).toUpperCase();
}

// Image error handler (more robust)
function onImageError(event: Event) {
    const imgElement = event.target as HTMLImageElement;
    imgElement.style.display = 'none'; // Hide broken image
    // Find the next sibling (the fallback div) and display it
    const fallback = imgElement.nextElementSibling;
    if (fallback instanceof HTMLElement) {
        fallback.style.display = 'flex';
    }
}
</script>

