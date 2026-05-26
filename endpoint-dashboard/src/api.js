// Use env variable if set (for Netlify deployment), otherwise same-origin or localhost
const API_BASE = process.env.REACT_APP_API_BASE
    ? process.env.REACT_APP_API_BASE
    : window.location.hostname === "localhost" && window.location.port === "3000"
        ? "http://localhost:5252/api"
        : "/api";

// API Key for cloud authentication (stored in sessionStorage after login)
const getApiKey = () => sessionStorage.getItem("apiKey");
const setApiKey = (key) => sessionStorage.setItem("apiKey", key);
const clearApiKey = () => sessionStorage.removeItem("apiKey");

// Check if we're using cloud API (not localhost or same-origin)
const isCloudApi = () => !!process.env.REACT_APP_API_BASE;

// Helper to include credentials (Windows Auth) or API Key header
const authFetch = (url, options = {}) => {
    const headers = { ...options.headers };
    
    // Use API Key for cloud deployment
    if (isCloudApi()) {
        const apiKey = getApiKey();
        if (apiKey) {
            headers["X-Api-Key"] = apiKey;
        }
    }
    
    return fetch(url, {
        credentials: isCloudApi() ? "omit" : "include",
        ...options,
        headers
    }).then(r => {
        if (r.status === 401) throw new Error("Authentication required. Please log in.");
        return r.json();
    });
};

export const api = {
    // Auth helpers
    isCloudMode: isCloudApi,
    hasApiKey: () => !!getApiKey(),
    setApiKey,
    clearApiKey,
    
    // Auth
    getCurrentUser: () => authFetch(`${API_BASE}/actions/me`),
    getAuditLogs: (limit = 100) => authFetch(`${API_BASE}/actions/audit?limit=${limit}`),

    // Health endpoints
    getDevices: () => authFetch(`${API_BASE}/health`),
    postHealth: (data) => authFetch(`${API_BASE}/health`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
    }),

    // Action endpoints
    getCategories: () => authFetch(`${API_BASE}/actions/categories`),
    executeAction: (actionType) => authFetch(`${API_BASE}/actions/execute`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ actionType })
    }),
    retryAction: (historyId) => authFetch(`${API_BASE}/actions/retry/${historyId}`, {
        method: "POST"
    }),
    getHistory: (limit = 50, categoryId = null) => {
        const params = new URLSearchParams({ limit });
        if (categoryId) params.set("categoryId", categoryId);
        return authFetch(`${API_BASE}/actions/history?${params}`);
    },

    // Multi-machine endpoints
    executeMulti: (actionType, deviceNames) => authFetch(`${API_BASE}/actions/execute-multi`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ actionType, deviceNames })
    }),
    uploadDevices: (file) => {
        const formData = new FormData();
        formData.append("file", file);
        const headers = {};
        if (isCloudApi()) {
            const apiKey = getApiKey();
            if (apiKey) headers["X-Api-Key"] = apiKey;
        }
        return fetch(`${API_BASE}/actions/upload-devices`, {
            method: "POST",
            credentials: isCloudApi() ? "omit" : "include",
            headers,
            body: formData
        }).then(r => r.json());
    }
};
