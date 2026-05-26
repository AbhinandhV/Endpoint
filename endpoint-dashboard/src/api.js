// Use env variable if set (for Netlify deployment), otherwise same-origin or localhost
const API_BASE = process.env.REACT_APP_API_BASE
    ? process.env.REACT_APP_API_BASE
    : window.location.hostname === "localhost" && window.location.port === "3000"
        ? "http://localhost:5252/api"
        : "/api";

// Helper to include credentials (Windows Auth) with every request
const authFetch = (url, options = {}) =>
    fetch(url, { credentials: "include", ...options }).then(r => {
        if (r.status === 401) throw new Error("Authentication required. Please log in.");
        return r.json();
    });

export const api = {
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
        return fetch(`${API_BASE}/actions/upload-devices`, {
            method: "POST",
            credentials: "include",
            body: formData
        }).then(r => r.json());
    }
};
