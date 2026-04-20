import React, { useState } from "react";
import Icon from "./Icon";

export default function ActionCard({ category, onExecute, actionStates }) {
    const [expanded, setExpanded] = useState(true);
    const [activeLog, setActiveLog] = useState(null);

    return (
        <div className="action-card">
            <div className="action-card-header" onClick={() => setExpanded(!expanded)}>
                <div className="action-card-title">
                    <Icon name={category.icon} className="category-icon" />
                    <div>
                        <h2>{category.title}</h2>
                        <p>{category.description}</p>
                    </div>
                </div>
                <Icon name={expanded ? "chevronUp" : "chevronDown"} />
            </div>

            {expanded && (
                <div className="action-list">
                    {category.actions.map((action) => {
                        const state = actionStates[action.id] || {};
                        const isRunning = state.status === "running";
                        const hasResult = state.status === "success" || state.status === "failed";

                        return (
                            <div key={action.id} className="action-item">
                                <div className="action-item-info">
                                    <div className="action-item-name">
                                        {action.name}
                                        {action.requiresAdmin && (
                                            <span className="admin-badge" title="Requires admin">
                                                <Icon name="shield" /> Admin
                                            </span>
                                        )}
                                    </div>
                                    <div className="action-item-desc">{action.description}</div>
                                </div>
                                <div className="action-item-controls">
                                    {hasResult && (
                                        <span className={`status-badge badge-${state.status}`}>
                                            <Icon name={state.status === "success" ? "check" : "x"} />
                                            {state.status === "success" ? "Success" : "Failed"}
                                            {state.durationMs && (
                                                <span className="duration"> ({state.durationMs}ms)</span>
                                            )}
                                        </span>
                                    )}
                                    {hasResult && (
                                        <button
                                            className="btn btn-sm btn-ghost"
                                            onClick={() => setActiveLog(activeLog === action.id ? null : action.id)}
                                        >
                                            {activeLog === action.id ? "Hide Log" : "View Log"}
                                        </button>
                                    )}
                                    <button
                                        className={`btn btn-sm ${isRunning ? "btn-disabled" : "btn-primary"}`}
                                        onClick={() => onExecute(action.id)}
                                        disabled={isRunning}
                                    >
                                        {isRunning ? (
                                            <><span className="spinner" /> Running...</>
                                        ) : (
                                            <><Icon name="play" /> Run</>
                                        )}
                                    </button>
                                </div>
                                {activeLog === action.id && state.output && (
                                    <div className="action-log-inline">
                                        <pre>{state.output}</pre>
                                        {state.error && <pre className="log-error-mini">{state.error}</pre>}
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
}
