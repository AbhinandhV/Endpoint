import React, { useState } from "react";
import Icon from "./Icon";

export default function HistoryPanel({ history, onRetry, onClose }) {
    const [expandedId, setExpandedId] = useState(null);

    if (!history || history.length === 0) {
        return (
            <div className="history-panel">
                <div className="card">
                    <div className="empty-state">
                        <div className="empty-state-icon">
                            <Icon name="history" />
                        </div>
                        <h4 className="empty-state-title">No history yet</h4>
                        <p className="empty-state-desc">Actions you run will appear here</p>
                    </div>
                </div>
            </div>
        );
    }

    const formatTime = (dateStr) => {
        const date = new Date(dateStr);
        const now = new Date();
        const diff = now - date;
        
        if (diff < 60000) return "Just now";
        if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`;
        if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`;
        return date.toLocaleDateString();
    };

    return (
        <div className="history-panel">
            <div className="history-list">
                {history.map((entry) => (
                    <div 
                        key={entry.id} 
                        className={`history-entry`}
                    >
                        <div className="history-entry-main">
                            <span className={`status-dot dot-${entry.status?.toLowerCase()}`} />
                            <span className="history-action">{entry.actionName}</span>
                            <span className={`status-badge badge-${entry.status?.toLowerCase()}`}>
                                <Icon name={entry.status === "Success" ? "check" : "x"} />
                                {entry.status}
                            </span>
                            <span className="history-duration">
                                <Icon name="clock" /> {entry.durationMs}ms
                            </span>
                            <span className="history-time">
                                {formatTime(entry.startedAt)}
                            </span>
                            {entry.output && (
                                <button
                                    className="btn btn-xs btn-ghost"
                                    onClick={() => setExpandedId(expandedId === entry.id ? null : entry.id)}
                                >
                                    {expandedId === entry.id ? "Hide" : "Details"}
                                </button>
                            )}
                            {entry.status === "Failed" && onRetry && (
                                <button
                                    className="btn btn-xs btn-ghost"
                                    onClick={() => onRetry(entry.id)}
                                    title="Retry this action"
                                >
                                    <Icon name="refresh" /> Retry
                                </button>
                            )}
                        </div>
                        {expandedId === entry.id && entry.output && (
                            <pre className="history-output">{entry.output}</pre>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}
