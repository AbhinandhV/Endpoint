import React from "react";
import Icon from "./Icon";

export default function StatsCard({ icon, iconColor = "primary", value, label, change, changeType }) {
    return (
        <div className="stat-card">
            <div className={`stat-card-icon ${iconColor}`}>
                <Icon name={icon} />
            </div>
            <div className="stat-card-value">{value}</div>
            <div className="stat-card-label">{label}</div>
            {change && (
                <div className={`stat-card-change ${changeType}`}>
                    {changeType === "positive" ? "↑" : changeType === "negative" ? "↓" : ""} {change}
                </div>
            )}
        </div>
    );
}
