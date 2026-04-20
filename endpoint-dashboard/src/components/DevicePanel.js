import React from "react";
import Icon from "./Icon";

export default function DevicePanel({ devices, onRefresh }) {
    if (!devices || devices.length === 0) {
        return (
            <div className="device-panel">
                <div className="device-panel-header">
                    <h3><Icon name="monitor" /> Device Health</h3>
                    <button className="btn btn-sm btn-ghost" onClick={onRefresh}>
                        <Icon name="refresh" /> Refresh
                    </button>
                </div>
                <p className="text-muted">No devices reporting. Run a Health Check to see device status.</p>
            </div>
        );
    }

    return (
        <div className="device-panel">
            <div className="device-panel-header">
                <h3><Icon name="monitor" /> Device Health ({devices.length})</h3>
                <button className="btn btn-sm btn-ghost" onClick={onRefresh}>
                    <Icon name="refresh" /> Refresh
                </button>
            </div>
            <div className="device-grid">
                {devices.map((device, i) => (
                    <div key={i} className="device-card">
                        <div className="device-name">{device.deviceName}</div>
                        <div className="device-stats">
                            <DeviceStat
                                label="Service"
                                value={device.serviceStatus}
                                ok={device.serviceStatus === "Running"}
                            />
                            <DeviceStat
                                label="Disk"
                                value={device.diskStatus}
                                ok={device.diskStatus === "OK"}
                            />
                            <DeviceStat
                                label="Network"
                                value={device.networkStatus}
                                ok={device.networkStatus === "Connected"}
                            />
                        </div>
                        <div className="device-updated">
                            <Icon name="clock" />
                            {new Date(device.lastUpdated).toLocaleString()}
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

function DeviceStat({ label, value, ok }) {
    return (
        <div className={`device-stat ${ok ? "stat-ok" : "stat-warn"}`}>
            <span className="stat-dot" />
            <span className="stat-label">{label}:</span>
            <span className="stat-value">{value}</span>
        </div>
    );
}
