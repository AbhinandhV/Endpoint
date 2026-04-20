import React from "react";
import Icon from "./Icon";

export default function HistoryPanel({ history, onRetry, onClose }) {
    if (!history || history.length === 0) {
        return (
            <div className="history-panel">
                <div className="history-panel-header">
                    <h3><Icon name="history" /> Action History</h3>
                    {onClose && <button className="btn btn-sm btn-ghost" onClick={onClose}>&times;</button>}
                </div>
                <p className="text-muted">No actions have been executed yet.</p>
            </div>
        );
    }

    return (
        <div className="history-panel">
            <div className="history-panel-header">
                <h3><Icon name="history" /> Action History ({history.length})</h3>
                {onClose && <button className="btn btn-sm btn-ghost" onClick={onClose}>&times;</button>}
            </div>
            <div className="history-list">
                {history.map((entry) => (
                    <div key={entry.id} className={`history-entry history-${entry.status?.toLowerCase()}`}>
                        <div className="history-entry-main">
                            <span className={`status-dot dot-${entry.status?.toLowerCase()}`} />
                            <span className="history-action">{entry.actionName}</span>
                            <span className={`status-badge badge-${entry.status?.toLowerCase()}`}>
                                {entry.status}
                            </span>
                            <span className="history-duration">{entry.durationMs}ms</span>
                            <span className="history-time">
                                {new Date(entry.startedAt).toLocaleString()}
                            </span>
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
                        {entry.output && (
                            <pre className="history-output">{entry.output}</pre>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}
