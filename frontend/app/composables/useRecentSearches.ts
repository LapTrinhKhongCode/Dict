// Composable to manage recent word searches in localStorage
import { ref } from 'vue';

export interface RecentSearchEntry {
    word: string;
    phonetic: string;
    short_mean: string;
    searchedAt: number; // timestamp
}

const STORAGE_KEY = 'miyo_recent_searches';
const MAX_ENTRIES = 10;

// Shared state across components
const recentSearches = ref<RecentSearchEntry[]>([]);
let isInitialized = false;

// Load from localStorage (runs once)
const initializeFromStorage = () => {
    if (isInitialized) return;
    if (typeof window === 'undefined') return;

    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
            recentSearches.value = JSON.parse(stored);
        }
        isInitialized = true;
    } catch (e) {
        console.error('Failed to load recent searches:', e);
        recentSearches.value = [];
        isInitialized = true;
    }
};

export function useRecentSearches() {
    // Initialize on first use (client-side only)
    if (typeof window !== 'undefined') {
        initializeFromStorage();
    }

    // Save to localStorage
    const saveToStorage = () => {
        if (typeof window === 'undefined') return;

        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(recentSearches.value));
        } catch (e) {
            console.error('Failed to save recent searches:', e);
        }
    };

    // Add a new search entry
    const addRecentSearch = (entry: Omit<RecentSearchEntry, 'searchedAt'>) => {
        // Skip if no word
        if (!entry.word) return;

        // Ensure we have latest data
        initializeFromStorage();

        // Remove existing entry with same word (to avoid duplicates)
        recentSearches.value = recentSearches.value.filter(
            (item) => item.word !== entry.word
        );

        // Add new entry at the beginning
        recentSearches.value.unshift({
            ...entry,
            searchedAt: Date.now(),
        });

        // Keep only the last MAX_ENTRIES
        if (recentSearches.value.length > MAX_ENTRIES) {
            recentSearches.value = recentSearches.value.slice(0, MAX_ENTRIES);
        }

        saveToStorage();
    };

    // Clear all recent searches
    const clearRecentSearches = () => {
        recentSearches.value = [];
        saveToStorage();
    };

    // Remove a single entry
    const removeRecentSearch = (word: string) => {
        recentSearches.value = recentSearches.value.filter(
            (item) => item.word !== word
        );
        saveToStorage();
    };

    // Force reload from storage
    const loadFromStorage = () => {
        isInitialized = false;
        initializeFromStorage();
    };

    return {
        recentSearches,
        addRecentSearch,
        clearRecentSearches,
        removeRecentSearch,
        loadFromStorage,
    };
}
