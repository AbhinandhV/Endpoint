import React from "react";
import Icon from "./Icon";

export default function LogPanel({ logs, output, onClose }) {
    if (!output && (!logs || logs.length === 0)) return null;

    return (
        <div className="log-panel">
            <div className="log-panel-header">
                <h3><Icon name="history" /> Output Log</h3>
                {onClose && (
                    <button className="btn btn-sm btn-ghost" onClick={onClose}>&times;</button>
                )}
            </div>
            {output && (
                <pre className="log-output">{output}</pre>
            )}
            {logs && logs.length > 0 && (
                <div className="log-history">
                    {logs.map((log, i) => (
                        <div key={i} className={`log-entry log-${log.status?.toLowerCase()}`}>
                            <div className="log-entry-header">
                                <span className={`status-badge badge-${log.status?.toLowerCase()}`}>
                                    {log.status}
                                </span>
                                <span className="log-action">{log.actionName}</span>
                                <span className="log-duration">{log.durationMs}ms</span>
                                <span className="log-time">
                                    {new Date(log.startedAt).toLocaleTimeString()}
                                </span>
                            </div>
                            {log.output && <pre className="log-output-mini">{log.output}</pre>}
                            {log.error && <pre className="log-error-mini">{log.error}</pre>}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
