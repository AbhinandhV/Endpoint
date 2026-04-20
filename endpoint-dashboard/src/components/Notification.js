import React, { useEffect } from "react";
import Icon from "./Icon";

export default function Notification({ notifications, removeNotification }) {
    return (
        <div className="notification-container">
            {notifications.map((n) => (
                <NotificationItem
                    key={n.id}
                    notification={n}
                    onDismiss={() => removeNotification(n.id)}
                />
            ))}
        </div>
    );
}

function NotificationItem({ notification, onDismiss }) {
    useEffect(() => {
        const timer = setTimeout(onDismiss, 5000);
        return () => clearTimeout(timer);
    }, [onDismiss]);

    const icon = notification.type === "success" ? "check" : "x";

    return (
        <div className={`notification notification-${notification.type}`}>
            <Icon name={icon} />
            <div className="notification-content">
                <strong>{notification.title}</strong>
                <span>{notification.message}</span>
            </div>
            <button className="notification-close" onClick={onDismiss}>&times;</button>
        </div>
    );
}
