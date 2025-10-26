<template>
    <div ref="searchContainer" class="relative w-full">
      <div class="flex items-center border rounded-2xl px-3 py-2 w-full">
        <button
          @click="onSearch"
          class="text-gray-500 hover:text-gray-700 transition mr-3"
        >
          <UIcon name="i-lucide-search" class="size-5" />
        </button>
        <input
          v-model="internalSearchWord"
          type="text"
          placeholder="Enter word..."
          class="flex-grow bg-transparent outline-none text-base"
          @keyup.enter="onSearch"
          @focus="showSuggestions = suggestions.length > 0"
          @keydown="handleKeydown"
        />
      </div>
  
      <div
        v-if="showSuggestions && suggestions.length > 0"
        class="absolute z-10 w-full mt-1 bg-gray-800 border border-gray-700 rounded-lg shadow-lg max-h-80 overflow-y-auto suggestions-list"
      >
        <ul ref="suggestionsListEl">
          <li
            v-for="(suggestion, index) in suggestions"
            :key="suggestion.word + suggestion.reading"
            class="px-4 py-3 border-b border-gray-700 last:border-b-0 cursor-pointer hover:bg-gray-700"
            :class="{ 'bg-gray-700': index === selectedIndex }"
            @click="onSelectSuggestion(suggestion)"
          >
            <div class="flex items-baseline gap-x-2">
              <span class="font-medium text-white">{{ suggestion.word }}</span>
              <span class="text-sm text-blue-400">{{ suggestion.reading }}</span>
            </div>
            <p class="text-sm text-gray-300 mt-1">{{ suggestion.meaning }}</p>
          </li>
        </ul>
      </div>
    </div>
  </template>
  
  <style scoped>
  .suggestions-list::-webkit-scrollbar {
    width: 8px; /* Width of the scrollbar */
  }
  
  .suggestions-list::-webkit-scrollbar-track {
    background: #1f2937; /* Track background, matching your bg-gray-800 */
    border-radius: 10px;
  }
  
  .suggestions-list::-webkit-scrollbar-thumb {
    background-color: #4b5563; /* A light gray (Tailwind's gray-300) */
    border-radius: 10px;
    border: 2px solid #1f2937; /* Creates a padding effect, matching bg-gray-800 */
    background-clip: padding-box;
  }
  
  .suggestions-list::-webkit-scrollbar-thumb:hover {
    background-color: #6b7280; /* A slightly darker gray on hover (Tailwind's gray-400) */
  }
  </style>
  
  <script setup lang="ts">
  import { ref, watch, onMounted, onBeforeUnmount, nextTick } from "vue";
  import { toKana } from "wanakana";
  
  // --- Props & Emits ---
  // We use v-model (modelValue prop and update:modelValue emit) to sync the search word
  // We emit a 'search' event to tell the parent page to run the search
  const props = defineProps({
    modelValue: {
      type: String,
      default: "",
    },
  });
  const emit = defineEmits(["update:modelValue", "search"]);

  const config = useRuntimeConfig();
  
  // --- Internal State ---
  // This internal ref tracks the input's value.
  const internalSearchWord = ref(props.modelValue);
  const suggestions = ref<any[]>([]);
  const showSuggestions = ref(false);
  const searchContainer = ref<HTMLDivElement | null>(null);
  let debounceTimer: any = null;
  const selectedIndex = ref(-1);
  const suggestionsListEl = ref<HTMLUListElement | null>(null);
  const isProgrammaticUpdate = ref(false); // To prevent watchers from firing loops
  
  // --- Watcher to sync prop to internal state ---
  // If the parent changes the searchWord (e.g., from URL), update our internal value
  watch(
    () => props.modelValue,
    (newValue) => {
      if (newValue !== internalSearchWord.value) {
        isProgrammaticUpdate.value = true;
        internalSearchWord.value = newValue;
      }
    }
  );
  
  // --- Watcher for Autocomplete ---
  // This is the main autocomplete logic, running when the user types
  watch(internalSearchWord, (newValue) => {
    // Sync internal value back up to the parent
    emit("update:modelValue", newValue);
  
    // If this update was programmatic (e.g., from suggestion click), don't run autocomplete
    if (isProgrammaticUpdate.value) {
      isProgrammaticUpdate.value = false;
      return;
    }
  
    if (debounceTimer) {
      clearTimeout(debounceTimer);
    }
    selectedIndex.value = -1;
  
    const trimmed = newValue.trim();
    if (!trimmed) {
      suggestions.value = [];
      showSuggestions.value = false;
      return;
    }
  
    // Set a timer to fetch autocomplete suggestions
    debounceTimer = setTimeout(async () => {
      try {
        const convertedWord = toKana(trimmed);
        const res = await fetch(
          `${config.public.apiBaseUrl}/api/Search/autocomplete/${encodeURIComponent(
            convertedWord
          )}`
        );
        if (!res.ok) throw new Error("Autocomplete fetch failed");
  
        const data = await res.json();
        suggestions.value = data || [];
        showSuggestions.value = suggestions.value.length > 0;
      } catch (e) {
        console.error("Autocomplete error:", e);
        suggestions.value = [];
        showSuggestions.value = false;
      }
    }, 350);
  });
  
  // --- Methods ---
  
  // Emits the 'search' event to the parent
  const onSearch = () => {
    showSuggestions.value = false;
    if (debounceTimer) {
      clearTimeout(debounceTimer);
    }
    // Tell the parent to run the search with the current word
    emit("search", internalSearchWord.value.trim());
  };
  
  // Handles clicking a suggestion
  const onSelectSuggestion = (suggestion: any) => {
    if (!suggestion) return;
    
    // Set flag to prevent autocomplete watcher from running
    isProgrammaticUpdate.value = true; 
    internalSearchWord.value = suggestion.word;
    
    showSuggestions.value = false;
    selectedIndex.value = -1;
    
    // Tell the parent to run the search with the selected word
    emit("search", suggestion.word);
  };
  
  // Hides suggestions when clicking outside the component
  const handleClickOutside = (event: MouseEvent) => {
    if (
      searchContainer.value &&
      !searchContainer.value.contains(event.target as Node)
    ) {
      showSuggestions.value = false;
      selectedIndex.value = -1;
    }
  };
  
  // Scrolls the suggestion list to the selected item
  const scrollToSelected = async () => {
    if (selectedIndex.value < 0 || !suggestionsListEl.value) return;
  
    await nextTick();
    const selectedEl = suggestionsListEl.value.children[
      selectedIndex.value
    ] as HTMLLIElement;
    if (selectedEl) {
      selectedEl.scrollIntoView({
        block: "nearest",
        behavior: "smooth",
      });
    }
  };
  
  // Handles keyboard navigation for suggestions
  const handleKeydown = (event: KeyboardEvent) => {
    if (showSuggestions.value && suggestions.value.length > 0) {
      if (event.key === "ArrowDown") {
        event.preventDefault();
        if (selectedIndex.value < suggestions.value.length - 1) {
          selectedIndex.value++;
          scrollToSelected();
        }
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        if (selectedIndex.value > 0) {
          selectedIndex.value--;
          scrollToSelected();
        }
      } else if (event.key === "Enter") {
        if (selectedIndex.value >= 0) {
          event.preventDefault(); // Stop input's @keyup.enter
          onSelectSuggestion(suggestions.value[selectedIndex.value]);
        }
        // Note: The 'onSearch' for enter is handled by @keyup.enter on the input itself
      } else if (event.key === 'Escape') {
        showSuggestions.value = false;
        selectedIndex.value = -1;
      }
    }
  };
  
  // --- Lifecycle Hooks ---
  // Add/remove global event listeners
  onMounted(() => {
    document.addEventListener("click", handleClickOutside);
    // We attach keydown to the input itself, so no global listener needed for that
  });
  
  onBeforeUnmount(() => {
    document.removeEventListener("click", handleClickOutside);
    if (debounceTimer) {
      clearTimeout(debounceTimer);
    }
  });
  </script>