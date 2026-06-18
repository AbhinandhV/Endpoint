# Endpoint Self-Service IT Portal

A web-based self-service application that empowers end users to resolve common IT issues by executing pre-approved PowerShell commands, reducing helpdesk tickets and improving resolution time.

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Application Flow](#application-flow)
- [Project Structure](#project-structure)
- [Key Components](#key-components)
- [Action Categories](#action-categories)
- [Security](#security)
- [Getting Started](#getting-started)
- [Configuration](#configuration)

---

## Overview

**Problem**: End users often face simple technical issues (network problems, printer jams, slow computer) that require IT intervention, leading to helpdesk backlogs.

**Solution**: This self-service portal allows users to fix common issues themselves by running pre-defined, IT-approved PowerShell scripts through a user-friendly web interface.

**Key Features**:
- ✅ Windows Single Sign-On (no passwords)
- ✅ Pre-approved scripts only (no arbitrary code execution)
- ✅ Full audit trail of all actions
- ✅ Real-time feedback with success/failure status
- ✅ Mobile-friendly responsive design

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         USER'S BROWSER                          │
│                    (React Dashboard - Port 3000)                │
└─────────────────────────────┬───────────────────────────────────┘
                              │ HTTP REST API
                              │ (Windows Auth)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET CORE BACKEND                       │
│                      (API Server - Port 5252)                   │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────────────────┐ │
│  │ Controllers │──│   Services   │──│   PowerShell Service   │ │
│  │  (REST API) │  │ (Business)   │  │   (Script Execution)   │ │
│  └─────────────┘  └──────────────┘  └────────────────────────┘ │
│         │                │                      │               │
│         ▼                ▼                      ▼               │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                    SQLite Database                          ││
│  │         (Action History, Audit Logs)                        ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

| Layer | Technology | Port |
|-------|------------|------|
| Frontend | React (JavaScript) | 3000 |
| Backend | ASP.NET Core 8 (C#) | 5252 |
| Database | SQLite | - |
| Auth | Windows/Negotiate | - |

---

## Application Flow

### 1. User Authentication
```
User opens browser → Navigates to http://localhost:3000
                            │
                            ▼
              Browser sends Windows credentials
              (Kerberos/NTLM via Negotiate)
                            │
                            ▼
              Backend validates user identity
                            │
                            ▼
              User sees dashboard with their name
```

### 2. Loading the Dashboard
```
React App loads → Calls GET /api/actions/categories
                            │
                            ▼
              Backend reads appsettings.json
              (Action definitions stored here)
                            │
                            ▼
              Returns JSON array of categories + actions
                            │
                            ▼
              Dashboard renders action cards
```

### 3. Executing an Action
```
User clicks "Fix Internet Connection"
                │
                ▼
    ┌───────────────────────────────────┐
    │  Frontend: POST /api/actions/execute
    │  Body: { "actionType": "fix-internet" }
    └───────────────────────────────────┘
                │
                ▼
    ┌───────────────────────────────────┐
    │  ActionController.Execute()
    │  1. Validate action exists
    │  2. Create history record
    │  3. Call PowerShellService
    └───────────────────────────────────┘
                │
                ▼
    ┌───────────────────────────────────┐
    │  PowerShellService.ExecuteAsync()
    │  1. Write script to temp .ps1 file
    │  2. Start PowerShell process
    │  3. Capture stdout/stderr
    │  4. Wait for completion or timeout
    └───────────────────────────────────┘
                │
                ▼
    ┌───────────────────────────────────┐
    │  Response returned to user
    │  {
    │    "status": "Success",
    │    "output": "DNS cache cleared...",
    │    "durationMs": 1234
    │  }
    └───────────────────────────────────┘
                │
                ▼
    ┌───────────────────────────────────┐
    │  Audit logged to database
    │  (User, Action, Timestamp, Result)
    └───────────────────────────────────┘
```

### 4. Viewing History
```
User clicks "History" tab → GET /api/actions/history
                                    │
                                    ▼
                    Returns recent actions with:
                    - Action name
                    - Status (Success/Failed)
                    - Output
                    - Timestamp
                    - Duration
```

---

## Project Structure

```
Endpoint/
├── Endpoint.sln                 # Visual Studio solution
├── README.md                    # This file
│
├── Endpoint/                    # Backend (ASP.NET Core)
│   ├── Program.cs               # Application entry point
│   ├── appsettings.json         # Configuration + Action definitions
│   ├── controllers/
│   │   ├── ActionController.cs  # REST API for actions
│   │   ├── HealthController.cs  # Health check endpoint
│   │   └── AgentController.cs   # Agent management
│   ├── Services/
│   │   ├── PowerShellService.cs # Script execution
│   │   ├── ActionConfigService.cs
│   │   ├── ActionHistoryService.cs
│   │   └── AuditService.cs
│   ├── Models/
│   │   ├── ActionConfig.cs
│   │   ├── ActionRequest.cs
│   │   ├── ActionResponse.cs
│   │   └── ActionHistory.cs
│   └── Data/
│       └── AppDbContext.cs      # Entity Framework context
│
├── endpoint-dashboard/          # Frontend (React)
│   ├── src/
│   │   ├── App.js               # Main component
│   │   ├── api.js               # API client
│   │   └── components/
│   │       ├── ActionCard.js
│   │       ├── HistoryPanel.js
│   │       └── SearchBar.js
│   └── package.json
│
├── EndpointAgent/               # Remote agent (optional)
└── EndpointDesktop/             # Desktop client (optional)
```

---

## Key Components

### Backend Services

| Service | Purpose |
|---------|---------|
| `ActionConfigService` | Reads action definitions from appsettings.json |
| `PowerShellService` | Executes PowerShell scripts safely with timeout |
| `ActionHistoryService` | Tracks execution history in database |
| `AuditService` | Logs all actions for compliance |

### API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/actions/categories` | Get all action categories |
| POST | `/api/actions/execute` | Execute an action |
| GET | `/api/actions/history` | Get recent action history |
| POST | `/api/actions/retry/{id}` | Retry a failed action |
| GET | `/api/health` | Health check |

---

## Action Categories

| Category | Description | Example Actions |
|----------|-------------|-----------------|
| **Network Issues** | Fix internet/network problems | Flush DNS, Check connection, Renew IP |
| **Printer Issues** | Resolve printing problems | Restart spooler, Clear print queue |
| **Slow Computer** | Speed up performance | Check resources, Clear temp files |
| **Common Fixes** | Everyday quick fixes | Refresh desktop, Fix Teams/Outlook |
| **System Info** | View computer details | Uptime, System specs |
| **SCCM Client** | SCCM/ConfigMgr status | Check client status |

### Action Definition Example (appsettings.json)

```json
{
  "Id": "fix-internet",
  "Name": "Fix Internet Connection",
  "Description": "Clear DNS cache and reset network",
  "Script": "Clear-DnsClientCache; Write-Output 'DNS cache cleared'",
  "RequiresAdmin": false,
  "Timeout": 15
}
```

---

## Security

### Authentication
- **Windows Authentication (Negotiate)** — Uses existing domain credentials
- No passwords stored in application
- User identity captured for all actions

### Authorization
- All endpoints require authentication
- Actions are pre-defined — users cannot run arbitrary scripts
- Admin actions require elevated privileges

### Audit Trail
Every action is logged with:
- Username
- Action ID
- Timestamp
- Success/Failure status
- Output/Error messages
- Client IP address
- Execution duration

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Windows (for PowerShell scripts)

### Running the Backend
```bash
cd Endpoint
dotnet run
# Runs on http://localhost:5252
```

### Running the Frontend
```bash
cd endpoint-dashboard
npm install
npm start
# Runs on http://localhost:3000
```

### Access the Application
Open browser to: `http://localhost:3000`

---

## Configuration

### Adding New Actions

Edit `Endpoint/appsettings.json`:

```json
{
  "ActionCategories": [
    {
      "Id": "my-category",
      "Title": "My Category",
      "Icon": "wrench",
      "Actions": [
        {
          "Id": "my-action",
          "Name": "My Action Name",
          "Description": "What this action does",
          "Script": "Write-Output 'Hello World'",
          "RequiresAdmin": false,
          "Timeout": 30
        }
      ]
    }
  ]
}
```

### Action Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | string | Unique identifier (kebab-case) |
| `Name` | string | Display name for users |
| `Description` | string | Brief explanation |
| `Script` | string | PowerShell script to execute |
| `RequiresAdmin` | bool | Needs elevation? |
| `Timeout` | int | Max execution time (seconds) |

---

## Tech Stack Summary

| Component | Technology |
|-----------|------------|
| Backend Framework | ASP.NET Core 8 |
| Frontend Framework | React 18 |
| Database | SQLite + Entity Framework Core |
| Authentication | Windows/Negotiate (Kerberos) |
| Script Engine | PowerShell 5.1 / PowerShell Core |
| Styling | CSS + Bootstrap |

---

## License

Internal use only.
