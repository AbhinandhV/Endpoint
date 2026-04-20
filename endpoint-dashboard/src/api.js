const API_BASE = "http://localhost:5252/api";

export const api = {
    // Health endpoints
    getDevices: () => fetch(`${API_BASE}/health`).then(r => r.json()),
    postHealth: (data) => fetch(`${API_BASE}/health`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
    }).then(r => r.json()),

    // Action endpoints
    getCategories: () => fetch(`${API_BASE}/actions/categories`).then(r => r.json()),
    executeAction: (actionType) => fetch(`${API_BASE}/actions/execute`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ actionType })
    }).then(r => r.json()),
    retryAction: (historyId) => fetch(`${API_BASE}/actions/retry/${historyId}`, {
        method: "POST"
    }).then(r => r.json()),
    getHistory: (limit = 50, categoryId = null) => {
        const params = new URLSearchParams({ limit });
        if (categoryId) params.set("categoryId", categoryId);
        return fetch(`${API_BASE}/actions/history?${params}`).then(r => r.json());
    },
};
