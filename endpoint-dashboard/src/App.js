import React, { useEffect, useState, useCallback } from "react";
import "./App.css";
import { api } from "./api";
import SearchBar from "./components/SearchBar";
import ActionCard from "./components/ActionCard";
import DevicePanel from "./components/DevicePanel";
import HistoryPanel from "./components/HistoryPanel";
import Notification from "./components/Notification";
import MultiMachinePanel from "./components/MultiMachinePanel";
import Icon from "./components/Icon";

function App() {
    const [categories, setCategories] = useState([]);
    const [devices, setDevices] = useState([]);
    const [history, setHistory] = useState([]);
    const [actionStates, setActionStates] = useState({});
    const [notifications, setNotifications] = useState([]);
    const [search, setSearch] = useState("");
    const [activeTab, setActiveTab] = useState("actions");
    const [loading, setLoading] = useState(true);
    const [currentUser, setCurrentUser] = useState(null);

    // Load initial data
    useEffect(() => {
        Promise.all([
            api.getCategories().catch(() => []),
            api.getDevices().catch(() => []),
            api.getHistory(20).catch(() => []),
            api.getCurrentUser().catch(() => null)
        ]).then(([cats, devs, hist, user]) => {
            setCategories(cats);
            setDevices(devs);
            setHistory(hist);
            setCurrentUser(user);
            setLoading(false);
        });
    }, []);

    const refreshDevices = useCallback(() => {
        api.getDevices().then(setDevices).catch(() => {});
    }, []);

    const refreshHistory = useCallback(() => {
        api.getHistory(20).then(setHistory).catch(() => {});
    }, []);

    // Notification helpers
    const addNotification = useCallback((type, title, message) => {
        const id = Date.now();
        setNotifications((prev) => [...prev, { id, type, title, message }]);
    }, []);

    const refreshAll = useCallback(() => {
        setActionStates({});
        Promise.all([
            api.getCategories().catch(() => []),
            api.getDevices().catch(() => []),
            api.getHistory(20).catch(() => [])
        ]).then(([cats, devs, hist]) => {
            setCategories(cats);
            setDevices(devs);
            setHistory(hist);
            addNotification("success", "Refreshed", "Dashboard data updated");
        });
    }, [addNotification]);

    const removeNotification = useCallback((id) => {
        setNotifications((prev) => prev.filter((n) => n.id !== id));
    }, []);

    // Execute an action
    const executeAction = useCallback(async (actionId) => {
        setActionStates((prev) => ({
            ...prev,
            [actionId]: { status: "running" }
        }));

        try {
            const response = await api.executeAction(actionId);
            setActionStates((prev) => ({
                ...prev,
                [actionId]: {
                    status: response.status?.toLowerCase() === "success" ? "success" : "failed",
                    output: response.output,
                    error: response.error,
                    durationMs: response.durationMs
                }
            }));

            if (response.status === "Success") {
                addNotification("success", "Action Completed", response.output?.split("\n")[0] || actionId);
            } else {
                addNotification("error", "Action Failed", response.error || response.output || actionId);
            }

            // Refresh related data
            refreshDevices();
            refreshHistory();
        } catch (err) {
            setActionStates((prev) => ({
                ...prev,
                [actionId]: { status: "failed", output: "", error: err.message }
            }));
            addNotification("error", "Action Failed", err.message);
        }
    }, [addNotification, refreshDevices, refreshHistory]);

    // Retry a failed action
    const retryAction = useCallback(async (historyId) => {
        try {
            await api.retryAction(historyId);
            addNotification("success", "Retry Completed", "Action retried successfully");
            refreshHistory();
            refreshDevices();
        } catch {
            addNotification("error", "Retry Failed", "Could not retry the action");
        }
    }, [addNotification, refreshHistory, refreshDevices]);

    // Filter categories based on search
    const filteredCategories = categories.map((cat) => ({
        ...cat,
        actions: cat.actions.filter((a) => {
            if (!search) return true;
            const q = search.toLowerCase();
            return (
                a.name.toLowerCase().includes(q) ||
                a.description.toLowerCase().includes(q) ||
                cat.title.toLowerCase().includes(q)
            );
        })
    })).filter((cat) => cat.actions.length > 0);

    const totalActions = filteredCategories.reduce((sum, c) => sum + c.actions.length, 0);

    if (loading) {
        return (
            <div className="app" style={{ display: "flex", alignItems: "center", justifyContent: "center" }}>
                <div style={{ textAlign: "center" }}>
                    <div className="spinner" style={{ width: 32, height: 32, margin: "0 auto 16px" }} />
                    <p style={{ color: "var(--text-muted)" }}>Loading dashboard...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="app">
            <Notification
                notifications={notifications}
                removeNotification={removeNotification}
            />

            <header className="app-header">
                <div className="app-header-inner">
                    <div className="app-title">
                        <div className="logo">&#9881;</div>
                        <span>Endpoint Control Panel</span>
                    </div>
                    <SearchBar
                        value={search}
                        onChange={setSearch}
                        resultCount={totalActions}
                    />
                    <div className="header-actions">
                        <button className="btn btn-sm btn-ghost" onClick={refreshAll}>
                            <Icon name="refresh" /> Refresh
                        </button>
                        {currentUser && (
                            <span className="user-badge" title={currentUser.user}>
                                <Icon name="user" /> {currentUser.user?.split("\\").pop()}
                            </span>
                        )}
                    </div>
                </div>
            </header>

            <main className="main-content">
                <div className="tab-bar">
                    <button
                        className={`tab-btn ${activeTab === "actions" ? "active" : ""}`}
                        onClick={() => setActiveTab("actions")}
                    >
                        <Icon name="play" /> Actions
                    </button>
                    <button
                        className={`tab-btn ${activeTab === "devices" ? "active" : ""}`}
                        onClick={() => setActiveTab("devices")}
                    >
                        <Icon name="monitor" /> Devices ({devices.length})
                    </button>
                    <button
                        className={`tab-btn ${activeTab === "history" ? "active" : ""}`}
                        onClick={() => setActiveTab("history")}
                    >
                        <Icon name="history" /> History ({history.length})
                    </button>
                    <button
                        className={`tab-btn ${activeTab === "multi" ? "active" : ""}`}
                        onClick={() => setActiveTab("multi")}
                    >
                        <Icon name="devices" /> Multi-Machine
                    </button>
                </div>

                {activeTab === "actions" && (
                    <>
                        {filteredCategories.length === 0 ? (
                            <p className="text-muted">
                                {search ? `No actions matching "${search}"` : "No action categories configured."}
                            </p>
                        ) : (
                            filteredCategories.map((category) => (
                                <ActionCard
                                    key={category.id}
                                    category={category}
                                    onExecute={executeAction}
                                    actionStates={actionStates}
                                />
                            ))
                        )}
                    </>
                )}

                {activeTab === "devices" && (
                    <DevicePanel devices={devices} onRefresh={refreshDevices} />
                )}

                {activeTab === "history" && (
                    <HistoryPanel
                        history={history}
                        onRetry={retryAction}
                    />
                )}

                {activeTab === "multi" && (
                    <MultiMachinePanel
                        categories={categories}
                        addNotification={addNotification}
                    />
                )}
            </main>
        </div>
    );
}

export default App;