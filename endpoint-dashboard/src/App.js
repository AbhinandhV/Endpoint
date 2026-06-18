import React, { useEffect, useState, useCallback } from "react";
import "./App.modern.css";
import { api } from "./api";
import Sidebar from "./components/Sidebar";
import SearchBar from "./components/SearchBar";
import ActionCard from "./components/ActionCard";
import HistoryPanel from "./components/HistoryPanel";
import Notification from "./components/Notification";
import Icon from "./components/Icon";
import StatsCard from "./components/StatsCard";

function App() {
    const [categories, setCategories] = useState([]);
    const [history, setHistory] = useState([]);
    const [actionStates, setActionStates] = useState({});
    const [notifications, setNotifications] = useState([]);
    const [search, setSearch] = useState("");
    const [activeTab, setActiveTab] = useState("dashboard");
    const [loading, setLoading] = useState(true);
    const [currentUser, setCurrentUser] = useState(null);
    const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

    // Load initial data
    useEffect(() => {
        Promise.all([
            api.getCategories().catch(() => []),
            api.getHistory(20).catch(() => []),
            api.getCurrentUser().catch(() => null)
        ]).then(([cats, hist, user]) => {
            setCategories(cats);
            setHistory(hist);
            setCurrentUser(user);
            setLoading(false);
        });
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
            api.getHistory(20).catch(() => [])
        ]).then(([cats, hist]) => {
            setCategories(cats);
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
            refreshHistory();
        } catch (err) {
            setActionStates((prev) => ({
                ...prev,
                [actionId]: { status: "failed", output: "", error: err.message }
            }));
            addNotification("error", "Action Failed", err.message);
        }
    }, [addNotification, refreshHistory]);

    // Retry a failed action
    const retryAction = useCallback(async (historyId) => {
        try {
            await api.retryAction(historyId);
            addNotification("success", "Retry Completed", "Action retried successfully");
            refreshHistory();
        } catch {
            addNotification("error", "Retry Failed", "Could not retry the action");
        }
    }, [addNotification, refreshHistory]);

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
            <div className="app" style={{ display: "flex", alignItems: "center", justifyContent: "center", minHeight: "100vh" }}>
                <div style={{ textAlign: "center" }}>
                    <div className="spinner spinner-lg" style={{ margin: "0 auto 16px" }} />
                    <p className="text-muted">Loading dashboard...</p>
                </div>
            </div>
        );
    }

    // Calculate stats for dashboard
    const totalActionsCount = categories.reduce((sum, c) => sum + c.actions.length, 0);
    const successCount = history.filter(h => h.status === "Success").length;
    const failedCount = history.filter(h => h.status === "Failed").length;
    const recentCount = history.filter(h => {
        const date = new Date(h.startedAt);
        const now = new Date();
        return (now - date) < 24 * 60 * 60 * 1000; // Last 24 hours
    }).length;

    // Get page title based on active tab
    const getPageTitle = () => {
        switch (activeTab) {
            case "dashboard": return { title: "Dashboard", subtitle: "Quick IT support at your fingertips" };
            case "actions": return { title: "Actions", subtitle: "Execute system actions and scripts" };
            case "history": return { title: "History", subtitle: "View past action executions" };
            default: return { title: "Dashboard", subtitle: "" };
        }
    };

    const pageInfo = getPageTitle();

    return (
        <div className="app">
            <Notification
                notifications={notifications}
                removeNotification={removeNotification}
            />

            <Sidebar
                activeTab={activeTab}
                setActiveTab={setActiveTab}
                collapsed={sidebarCollapsed}
                setCollapsed={setSidebarCollapsed}
                historyCount={history.length}
            />

            <div className="main-wrapper">
                <header className="app-header">
                    <div className="app-header-inner">
                        <div className="header-left">
                            <div>
                                <h1 className="page-title">{pageInfo.title}</h1>
                                <p className="page-title-sub">{pageInfo.subtitle}</p>
                            </div>
                        </div>

                        <SearchBar
                            value={search}
                            onChange={setSearch}
                            resultCount={totalActions}
                        />

                        <div className="header-actions">
                            <button className="btn btn-sm btn-ghost btn-icon" title="Notifications">
                                <Icon name="bell" />
                            </button>
                            <button className="btn btn-sm btn-ghost" onClick={refreshAll}>
                                <Icon name="refresh" /> <span>Refresh</span>
                            </button>
                            {currentUser && (
                                <div className="user-menu">
                                    <div className="user-avatar">
                                        {currentUser.user?.split("\\").pop()?.charAt(0)?.toUpperCase() || "U"}
                                    </div>
                                    <div className="user-info">
                                        <span className="user-name">{currentUser.user?.split("\\").pop()}</span>
                                        <span className="user-role">Administrator</span>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </header>

                <main className="main-content">
                    {/* Dashboard View */}
                    {activeTab === "dashboard" && (
                        <>
                            <div className="stats-grid">
                                <StatsCard
                                    icon="play"
                                    iconColor="primary"
                                    value={totalActionsCount}
                                    label="Available Actions"
                                />
                                <StatsCard
                                    icon="check"
                                    iconColor="success"
                                    value={successCount}
                                    label="Successful Runs"
                                    change={successCount > 0 ? `${Math.round((successCount / (history.length || 1)) * 100)}%` : null}
                                    changeType="positive"
                                />
                                <StatsCard
                                    icon="x"
                                    iconColor="error"
                                    value={failedCount}
                                    label="Failed Runs"
                                    change={failedCount > 0 ? `${failedCount} issues` : null}
                                    changeType={failedCount > 0 ? "negative" : "positive"}
                                />
                                <StatsCard
                                    icon="clock"
                                    iconColor="warning"
                                    value={recentCount}
                                    label="Last 24 Hours"
                                />
                            </div>

                            <div className="quick-actions">
                                <button 
                                    className="quick-action-btn"
                                    onClick={() => setActiveTab("actions")}
                                >
                                    <Icon name="play" /> Run Action
                                </button>
                                <button 
                                    className="quick-action-btn"
                                    onClick={() => setActiveTab("history")}
                                >
                                    <Icon name="history" /> View History
                                </button>
                                <a
                                    href="https://hrblock.sharepoint.com/sites/CorporateTechnologySupport"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="quick-action-btn"
                                >
                                    <Icon name="help" /> IT Support
                                </a>
                            </div>

                            <h3 className="mb-3" style={{ fontSize: "16px", fontWeight: 600 }}>Recent Activity</h3>
                            {history.length > 0 ? (
                                <HistoryPanel
                                    history={history.slice(0, 5)}
                                    onRetry={retryAction}
                                />
                            ) : (
                                <div className="card">
                                    <div className="empty-state">
                                        <div className="empty-state-icon">
                                            <Icon name="history" />
                                        </div>
                                        <h4 className="empty-state-title">No recent activity</h4>
                                        <p className="empty-state-desc">Run your first action to see it here</p>
                                        <button className="btn btn-primary" onClick={() => setActiveTab("actions")}>
                                            <Icon name="play" /> Browse Actions
                                        </button>
                                    </div>
                                </div>
                            )}
                        </>
                    )}

                    {/* Actions View */}
                    {activeTab === "actions" && (
                        <>
                            <div className="tab-bar">
                                <button className="tab-btn active">
                                    <Icon name="play" /> All Actions
                                </button>
                            </div>

                            {filteredCategories.length === 0 ? (
                                <div className="card">
                                    <div className="empty-state">
                                        <div className="empty-state-icon">
                                            <Icon name="search" />
                                        </div>
                                        <h4 className="empty-state-title">
                                            {search ? `No actions matching "${search}"` : "No actions available"}
                                        </h4>
                                        <p className="empty-state-desc">
                                            {search ? "Try a different search term" : "Configure actions in the backend"}
                                        </p>
                                    </div>
                                </div>
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

                    {/* History View */}
                    {activeTab === "history" && (
                        <>
                            <div className="tab-bar">
                                <button className="tab-btn active">
                                    <Icon name="history" /> All History
                                </button>
                            </div>

                            <HistoryPanel
                                history={history}
                                onRetry={retryAction}
                            />
                        </>
                    )}

                </main>
            </div>
        </div>
    );
}

export default App;