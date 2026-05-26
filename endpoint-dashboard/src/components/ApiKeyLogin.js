import React, { useState } from "react";
import Icon from "./Icon";

export default function ApiKeyLogin({ onLogin }) {
    const [apiKey, setApiKey] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!apiKey.trim()) {
            setError("Please enter an API key");
            return;
        }

        setLoading(true);
        setError("");

        // Test the API key by making a request
        try {
            const response = await fetch(
                `${process.env.REACT_APP_API_BASE}/actions/categories`,
                { headers: { "X-Api-Key": apiKey.trim() } }
            );

            if (response.ok) {
                onLogin(apiKey.trim());
            } else if (response.status === 401) {
                setError("Invalid API key");
            } else {
                setError("Connection error. Please try again.");
            }
        } catch {
            setError("Cannot connect to server");
        }

        setLoading(false);
    };

    return (
        <div className="app" style={{ display: "flex", alignItems: "center", justifyContent: "center" }}>
            <div className="login-card" style={{
                background: "var(--card-bg)",
                padding: "2rem",
                borderRadius: "12px",
                boxShadow: "var(--shadow-lg)",
                maxWidth: "400px",
                width: "100%"
            }}>
                <div style={{ textAlign: "center", marginBottom: "1.5rem" }}>
                    <div className="logo" style={{ fontSize: "2rem", marginBottom: "0.5rem" }}>&#9881;</div>
                    <h2 style={{ margin: 0 }}>Endpoint Control Panel</h2>
                    <p style={{ color: "var(--text-muted)", margin: "0.5rem 0 0" }}>
                        Enter your API key to continue
                    </p>
                </div>

                <form onSubmit={handleSubmit}>
                    <div style={{ marginBottom: "1rem" }}>
                        <input
                            type="password"
                            className="mm-textarea"
                            placeholder="API Key"
                            value={apiKey}
                            onChange={(e) => setApiKey(e.target.value)}
                            style={{
                                width: "100%",
                                padding: "0.75rem",
                                fontSize: "1rem",
                                borderRadius: "8px"
                            }}
                            autoFocus
                        />
                    </div>

                    {error && (
                        <div style={{
                            color: "var(--danger)",
                            marginBottom: "1rem",
                            fontSize: "0.875rem"
                        }}>
                            {error}
                        </div>
                    )}

                    <button
                        type="submit"
                        className="btn btn-primary"
                        disabled={loading}
                        style={{ width: "100%", padding: "0.75rem", fontSize: "1rem" }}
                    >
                        {loading ? (
                            <><span className="spinner" /> Connecting...</>
                        ) : (
                            <><Icon name="login" /> Login</>
                        )}
                    </button>
                </form>
            </div>
        </div>
    );
}
