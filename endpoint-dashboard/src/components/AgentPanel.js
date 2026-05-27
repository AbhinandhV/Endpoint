import React, { useState, useEffect, useCallback } from "react";
import { api } from "../api";
import Icon from "./Icon";

function AgentPanel({ categories, addNotification }) {
    const [agents, setAgents] = useState([]);
    const [selectedAgent, setSelectedAgent] = useState(null);
    const [selectedAction, setSelectedAction] = useState(null);
    const [loading, setLoading] = useState(true);
    const [executing, setExecuting] = useState(false);
    const [commandHistory, setCommandHistory] = useState([]);

    const refreshAgents = useCallback(() => {
        api.getAgents()
            .then(setAgents)
            .catch(() => setAgents([]))
            .finally(() => setLoading(false));
    }, []);

    useEffect(() => {
        refreshAgents();
        const interval = setInterval(refreshAgents, 10000); // Refresh every 10s
        return () => clearInterval(interval);
    }, [refreshAgents]);

    // Load command history when agent selected
    useEffect(() => {
        if (selectedAgent) {
            api.getAgentCommands(selectedAgent.agentId)
                .then(setCommandHistory)
                .catch(() => setCommandHistory([]));
        }
    }, [selectedAgent]);

    const allActions = categories.flatMap(cat =>
        cat.actions.map(a => ({ ...a, categoryTitle: cat.title }))
    );

    const handleExecute = async () => {
        if (!selectedAgent || !selectedAction) return;

        setExecuting(true);
        try {
            await api.queueCommand(
                selectedAgent.agentId,
                selectedAction.id,
                selectedAction.name,
                selectedAction.script
            );
            addNotification("success", "Command Queued",
                `"${selectedAction.name}" queued for ${selectedAgent.machineName}`);
            
            // Refresh command history
            const cmds = await api.getAgentCommands(selectedAgent.agentId);
            setCommandHistory(cmds);
        } catch (err) {
            addNotification("error", "Queue Failed", err.message);
        } finally {
            setExecuting(false);
        }
    };

    if (loading) {
        return (
            <div className="panel">
                <div className="panel-header">
                    <Icon name="devices" /> Remote Agents
                </div>
                <div className="panel-body">
                    <p className="text-muted">Loading agents...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="agent-panel">
            <div className="panel">
                <div className="panel-header">
                    <Icon name="devices" /> Remote Agents
                    <button className="btn btn-sm btn-ghost" onClick={refreshAgents}>
                        <Icon name="refresh" />
                    </button>
                </div>
                <div className="panel-body">
                    {agents.length === 0 ? (
                        <div className="empty-state">
                            <p className="text-muted">No agents registered yet.</p>
                            <p className="text-sm text-muted">
                                Deploy EndpointAgent.exe to your endpoints to get started.
                            </p>
                        </div>
                    ) : (
                        <div className="agent-list">
                            {agents.map(agent => (
                                <div
                                    key={agent.agentId}
                                    className={`agent-item ${selectedAgent?.agentId === agent.agentId ? 'selected' : ''}`}
                                    onClick={() => setSelectedAgent(agent)}
                                >
                                    <div className="agent-status">
                                        <span className={`status-dot ${agent.isOnline ? 'online' : 'offline'}`} />
                                    </div>
                                    <div className="agent-info">
                                        <div className="agent-name">{agent.machineName}</div>
                                        <div className="agent-meta">
                                            {agent.ipAddress} • Last seen: {formatTime(agent.lastSeenAt)}
                                        </div>
                                    </div>
                                    {agent.pendingCommands > 0 && (
                                        <span className="badge badge-warning">{agent.pendingCommands} pending</span>
                                    )}
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>

            {selectedAgent && (
                <div className="panel">
                    <div className="panel-header">
                        <Icon name="play" /> Execute on {selectedAgent.machineName}
                    </div>
                    <div className="panel-body">
                        <div className="form-group">
                            <label>Select Action:</label>
                            <select
                                className="form-select"
                                value={selectedAction?.id || ""}
                                onChange={(e) => {
                                    const action = allActions.find(a => a.id === e.target.value);
                                    setSelectedAction(action);
                                }}
                            >
                                <option value="">-- Choose an action --</option>
                                {categories.map(cat => (
                                    <optgroup key={cat.id} label={cat.title}>
                                        {cat.actions.map(action => (
                                            <option key={action.id} value={action.id}>
                                                {action.name}
                                            </option>
                                        ))}
                                    </optgroup>
                                ))}
                            </select>
                        </div>

                        {selectedAction && (
                            <div className="action-preview">
                                <p className="text-sm text-muted">{selectedAction.description}</p>
                            </div>
                        )}

                        <button
                            className="btn btn-primary"
                            onClick={handleExecute}
                            disabled={!selectedAction || executing || !selectedAgent.isOnline}
                        >
                            {executing ? (
                                <><span className="spinner" /> Queuing...</>
                            ) : (
                                <><Icon name="play" /> Queue Command</>
                            )}
                        </button>

                        {!selectedAgent.isOnline && (
                            <p className="text-sm text-muted" style={{ marginTop: 8 }}>
                                ⚠️ Agent is offline. Command will execute when it comes back online.
                            </p>
                        )}
                    </div>
                </div>
            )}

            {selectedAgent && commandHistory.length > 0 && (
                <div className="panel">
                    <div className="panel-header">
                        <Icon name="history" /> Command History - {selectedAgent.machineName}
                    </div>
                    <div className="panel-body">
                        <table className="table">
                            <thead>
                                <tr>
                                    <th>Action</th>
                                    <th>Status</th>
                                    <th>Time</th>
                                    <th>Duration</th>
                                </tr>
                            </thead>
                            <tbody>
                                {commandHistory.map(cmd => (
                                    <tr key={cmd.id}>
                                        <td>{cmd.actionName}</td>
                                        <td>
                                            <span className={`badge badge-${getStatusColor(cmd.status)}`}>
                                                {cmd.status}
                                            </span>
                                        </td>
                                        <td>{formatTime(cmd.createdAt)}</td>
                                        <td>{cmd.durationMs ? `${cmd.durationMs}ms` : '-'}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
        </div>
    );
}

function formatTime(dateStr) {
    if (!dateStr) return "Never";
    // Handle UTC time from API (add Z if missing)
    const utcStr = dateStr.endsWith('Z') ? dateStr : dateStr + 'Z';
    const date = new Date(utcStr);
    const now = new Date();
    const diff = (now - date) / 1000;

    if (diff < 0) return "Just now"; // Future time (clock skew)
    if (diff < 60) return "Just now";
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return date.toLocaleDateString();
}

function getStatusColor(status) {
    switch (status?.toLowerCase()) {
        case "completed": case "success": return "success";
        case "failed": return "error";
        case "running": return "warning";
        case "pending": return "info";
        default: return "default";
    }
}

export default AgentPanel;
