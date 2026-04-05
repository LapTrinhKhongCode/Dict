// Composable to manage recent word searches in localStorage
import { ref } from 'vue';

export interface RecentSearchEntry {
    word: string;
    phonetic: string;
    short_mean: string;
    searchedAt: number; // timestamp
}

const MAX_ENTRIES = 10;

// Shared state across components
const recentSearches = ref<RecentSearchEntry[]>([]);
let currentUserId = ''; // Theo dõi dữ liệu của user nào đang được nạp

// Hàm tạo key động dựa trên userId
const getStorageKey = (userId: string) => `miyo_recent_searches_${userId}`;

// Load from localStorage
const loadFromStorage = (userId: string) => {
    if (typeof window === 'undefined') return;

    try {
        const stored = localStorage.getItem(getStorageKey(userId));
        if (stored) {
            recentSearches.value = JSON.parse(stored);
        } else {
            recentSearches.value = []; // Xóa trắng nếu user chưa có dữ liệu
        }
        currentUserId = userId; // Cập nhật id user hiện tại
    } catch (e) {
        console.error('Failed to load recent searches:', e);
        recentSearches.value = [];
        currentUserId = userId;
    }
};

// Nhận thêm tham số userId (mặc định là 'guest' nếu chưa đăng nhập)
export function useRecentSearches(userId: string = 'guest') {
    // Nếu chưa khởi tạo hoặc người dùng đã đổi tài khoản -> Nạp lại dữ liệu mới
    if (typeof window !== 'undefined' && currentUserId !== userId) {
        loadFromStorage(userId);
    }

    // Save to localStorage
    const saveToStorage = () => {
        if (typeof window === 'undefined') return;

        try {
            localStorage.setItem(getStorageKey(userId), JSON.stringify(recentSearches.value));
        } catch (e) {
            console.error('Failed to save recent searches:', e);
        }
    };

    // Add a new search entry
    const addRecentSearch = (entry: Omit<RecentSearchEntry, 'searchedAt'>) => {
        // Skip if no word
        if (!entry.word) return;

        // Đảm bảo đang thao tác trên đúng dữ liệu của user
        if (currentUserId !== userId) {
            loadFromStorage(userId);
        }

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

    return {
        recentSearches,
        addRecentSearch,
        clearRecentSearches,
        removeRecentSearch,
        loadFromStorage: () => loadFromStorage(userId),
    };
}