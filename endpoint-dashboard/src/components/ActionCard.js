import React, { useState } from "react";
import Icon from "./Icon";

export default function ActionCard({ category, onExecute, actionStates }) {
    const [expanded, setExpanded] = useState(true);
    const [activeLog, setActiveLog] = useState(null);

    return (
        <div className="action-category">
            <div className="action-category-header" onClick={() => setExpanded(!expanded)}>
                <div className="action-category-title">
                    <div className="category-icon">
                        <Icon name={category.icon} />
                    </div>
                    <div>
                        <h2>{category.title}</h2>
                        <p>{category.description}</p>
                    </div>
                </div>
                <button className="btn btn-sm btn-ghost btn-icon">
                    <Icon name={expanded ? "chevronUp" : "chevronDown"} />
                </button>
            </div>

            {expanded && (
                <div className="action-cards-grid">
                    {category.actions.map((action) => {
                        const state = actionStates[action.id] || {};
                        const isRunning = state.status === "running";
                        const hasResult = state.status === "success" || state.status === "failed";

                        return (
                            <div key={action.id} className={`action-card-item ${hasResult ? `status-${state.status}` : ''}`}>
                                <div className="action-card-item-header">
                                    <div className="action-card-icon">
                                        <Icon name="play" />
                                    </div>
                                    {action.requiresAdmin && (
                                        <span className="admin-badge">
                                            <Icon name="shield" /> Admin
                                        </span>
                                    )}
                                </div>
                                <div className="action-card-item-body">
                                    <h3 className="action-card-item-name">{action.name}</h3>
                                    <p className="action-card-item-desc">{action.description}</p>
                                </div>
                                <div className="action-card-item-footer">
                                    {hasResult && (
                                        <div className="action-card-status">
                                            <span className={`status-badge badge-${state.status}`}>
                                                <Icon name={state.status === "success" ? "check" : "x"} />
                                                {state.status === "success" ? "Success" : "Failed"}
                                            </span>
                                            {state.durationMs && (
                                                <span className="duration-text">{state.durationMs}ms</span>
                                            )}
                                        </div>
                                    )}
                                    <div className="action-card-actions">
                                        {hasResult && (
                                            <button
                                                className="btn btn-sm btn-ghost"
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    setActiveLog(activeLog === action.id ? null : action.id);
                                                }}
                                            >
                                                {activeLog === action.id ? "Hide" : "Log"}
                                            </button>
                                        )}
                                        <button
                                            className={`btn btn-sm ${isRunning ? "btn-disabled" : "btn-primary"}`}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                onExecute(action.id);
                                            }}
                                            disabled={isRunning}
                                        >
                                            {isRunning ? (
                                                <><span className="spinner spinner-sm" /></>
                                            ) : (
                                                <><Icon name="play" /> Run</>
                                            )}
                                        </button>
                                    </div>
                                </div>
                                {activeLog === action.id && state.output && (
                                    <div className="action-card-log">
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
