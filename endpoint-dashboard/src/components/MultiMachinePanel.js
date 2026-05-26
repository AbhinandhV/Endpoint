import React, { useState } from "react";
import Icon from "./Icon";
import { api } from "../api";

export default function MultiMachinePanel({ categories, addNotification }) {
    const [deviceInput, setDeviceInput] = useState("");
    const [devices, setDevices] = useState([]);
    const [selectedAction, setSelectedAction] = useState("");
    const [running, setRunning] = useState(false);
    const [results, setResults] = useState(null);
    const [uploading, setUploading] = useState(false);

    const addDevices = () => {
        const names = deviceInput
            .split(/[,\n;]+/)
            .map((n) => n.trim())
            .filter((n) => n.length > 0 && !devices.includes(n));
        if (names.length > 0) {
            setDevices((prev) => [...prev, ...names]);
            setDeviceInput("");
        }
    };

    const removeDevice = (name) => {
        setDevices((prev) => prev.filter((d) => d !== name));
    };

    const clearDevices = () => {
        setDevices([]);
        setResults(null);
    };

    const handleFileUpload = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        setUploading(true);
        try {
            const result = await api.uploadDevices(file);
            if (result.devices && result.devices.length > 0) {
                const newDevices = result.devices.filter((d) => !devices.includes(d));
                setDevices((prev) => [...prev, ...newDevices]);
                addNotification("success", "CSV Imported", `${result.count} devices loaded from file`);
            } else if (result.error) {
                addNotification("error", "Import Failed", result.error);
            }
        } catch (err) {
            addNotification("error", "Upload Failed", err.message);
        }
        setUploading(false);
        e.target.value = "";
    };

    const executeOnAll = async () => {
        if (!selectedAction || devices.length === 0) return;

        setRunning(true);
        setResults(null);
        try {
            const response = await api.executeMulti(selectedAction, devices);
            setResults(response);
            const msg = `${response.succeeded}/${response.totalDevices} succeeded`;
            if (response.failed > 0) {
                addNotification("error", "Multi-Machine Complete", msg);
            } else {
                addNotification("success", "Multi-Machine Complete", msg);
            }
        } catch (err) {
            addNotification("error", "Execution Failed", err.message);
        }
        setRunning(false);
    };

    return (
        <div className="multi-machine-panel">
            <div className="mm-header">
                <h3><Icon name="devices" /> Run on Multiple Machines</h3>
            </div>

            <div className="mm-controls">
                <div className="mm-section">
                    <label className="mm-label">1. Select Action</label>
                    <select
                        className="mm-select"
                        value={selectedAction}
                        onChange={(e) => setSelectedAction(e.target.value)}
                    >
                        <option value="">-- Choose an action --</option>
                        {categories.map((cat) => (
                            <optgroup key={cat.id} label={cat.title}>
                                {cat.actions.map((a) => (
                                    <option key={a.id} value={a.id}>
                                        {a.name} {a.requiresAdmin ? "(Admin)" : ""}
                                    </option>
                                ))}
                            </optgroup>
                        ))}
                    </select>
                </div>

                <div className="mm-section">
                    <label className="mm-label">2. Add Devices</label>
                    <div className="mm-input-row">
                        <textarea
                            className="mm-textarea"
                            placeholder="Enter device names (comma, newline, or semicolon separated)&#10;e.g. PC-001, PC-002, PC-003"
                            value={deviceInput}
                            onChange={(e) => setDeviceInput(e.target.value)}
                            rows={3}
                        />
                        <div className="mm-input-actions">
                            <button className="btn btn-sm btn-primary" onClick={addDevices}>
                                <Icon name="plus" /> Add
                            </button>
                            <label className="btn btn-sm btn-ghost mm-upload-btn">
                                <Icon name="upload" /> {uploading ? "Uploading..." : "Upload CSV"}
                                <input
                                    type="file"
                                    accept=".csv,.txt"
                                    onChange={handleFileUpload}
                                    style={{ display: "none" }}
                                    disabled={uploading}
                                />
                            </label>
                        </div>
                    </div>
                </div>

                {devices.length > 0 && (
                    <div className="mm-section">
                        <label className="mm-label">
                            Devices ({devices.length})
                            <button className="btn btn-xs btn-ghost" onClick={clearDevices}>Clear all</button>
                        </label>
                        <div className="mm-device-tags">
                            {devices.map((d) => (
                                <span key={d} className="mm-device-tag">
                                    {d}
                                    <button className="mm-tag-remove" onClick={() => removeDevice(d)}>&times;</button>
                                </span>
                            ))}
                        </div>
                    </div>
                )}

                <div className="mm-section">
                    <button
                        className={`btn btn-primary mm-execute-btn ${running ? "btn-disabled" : ""}`}
                        onClick={executeOnAll}
                        disabled={running || !selectedAction || devices.length === 0}
                    >
                        {running ? (
                            <><span className="spinner" /> Running on {devices.length} device(s)...</>
                        ) : (
                            <><Icon name="play" /> Execute on {devices.length} device(s)</>
                        )}
                    </button>
                </div>
            </div>

            {results && (
                <div className="mm-results">
                    <div className="mm-results-header">
                        <h4>Results: {results.succeeded}/{results.totalDevices} succeeded</h4>
                    </div>
                    <div className="mm-results-list">
                        {results.results && results.results.map((r, i) => (
                            <div key={i} className={`mm-result-item mm-result-${r.status?.toLowerCase()}`}>
                                <div className="mm-result-main">
                                    <span className={`status-dot dot-${r.status?.toLowerCase()}`} />
                                    <span className="mm-result-device">{r.deviceName}</span>
                                    <span className={`status-badge badge-${r.status?.toLowerCase()}`}>
                                        {r.status}
                                    </span>
                                    <span className="mm-result-duration">{r.durationMs}ms</span>
                                </div>
                                {r.output && r.output !== "Action completed" && (
                                    <pre className="mm-result-output">{r.output}</pre>
                                )}
                                {r.error && (
                                    <pre className="log-error-mini">{r.error}</pre>
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}
