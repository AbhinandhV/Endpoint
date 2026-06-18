import React from "react";
import Icon from "./Icon";

export default function Sidebar({ activeTab, setActiveTab, collapsed, setCollapsed, historyCount = 0 }) {
    const navItems = [
        {
            section: "Main",
            items: [
                { id: "dashboard", label: "Dashboard", icon: "dashboard" },
                { id: "actions", label: "Actions", icon: "play", badge: null },
                { id: "history", label: "History", icon: "history", badge: historyCount > 0 ? historyCount : null },
            ]
        },
        {
            section: "Resources",
            items: [
                { id: "knowledge", label: "Knowledge Base", icon: "book", external: "https://hrb-dwp.onbmc.com/dwp/app/#/search/1mfnekck" },
                { id: "support", label: "IT Support", icon: "help", external: "https://hrblock.sharepoint.com/sites/CorporateTechnologySupport" },
            ]
        }
    ];

    return (
        <aside className={`sidebar ${collapsed ? "collapsed" : ""}`}>
            <div className="sidebar-brand">
                <div className="sidebar-logo">SS</div>
                <span className="sidebar-brand-text">SnapSupport</span>
            </div>

            <nav className="sidebar-nav">
                {navItems.map((section) => (
                    <div key={section.section} className="nav-section">
                        <div className="nav-section-title">{section.section}</div>
                        {section.items.map((item) => (
                            item.external ? (
                                <a
                                    key={item.id}
                                    href={item.external}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="nav-item"
                                    title={collapsed ? item.label : undefined}
                                >
                                    <Icon name={item.icon} />
                                    <span className="nav-label">{item.label}</span>
                                    <Icon name="external" className="nav-external-icon" />
                                </a>
                            ) : (
                                <button
                                    key={item.id}
                                    className={`nav-item ${activeTab === item.id ? "active" : ""}`}
                                    onClick={() => setActiveTab(item.id)}
                                    title={collapsed ? item.label : undefined}
                                >
                                    <Icon name={item.icon} />
                                    <span className="nav-label">{item.label}</span>
                                    {item.badge && <span className="nav-badge">{item.badge}</span>}
                                </button>
                            )
                        ))}
                    </div>
                ))}
            </nav>

            <div className="sidebar-footer">
                <button 
                    className="sidebar-toggle"
                    onClick={() => setCollapsed(!collapsed)}
                    title={collapsed ? "Expand sidebar" : "Collapse sidebar"}
                >
                    <Icon name={collapsed ? "chevronRight" : "chevronLeft"} />
                </button>
            </div>
        </aside>
    );
}
