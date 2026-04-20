import React from "react";
import Icon from "./Icon";

export default function SearchBar({ value, onChange, resultCount }) {
    return (
        <div className="search-bar">
            <Icon name="search" className="search-icon" />
            <input
                type="text"
                placeholder="Search actions... (e.g. DNS, cache, service)"
                value={value}
                onChange={(e) => onChange(e.target.value)}
            />
            {value && (
                <span className="search-count">
                    {resultCount} result{resultCount !== 1 ? "s" : ""}
                </span>
            )}
        </div>
    );
}
