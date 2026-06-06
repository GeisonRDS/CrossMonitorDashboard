# CrossMonitorDashboard

Cross-platform distributed system monitoring dashboard. Real-time monitoring of CPU, memory, disk, network, and temperature across multiple nodes.

## Architecture

```
Browser (14" Ubuntu Desktop / any device)
        |
        | HTTP (no direct agent access)
        v
CrossMonitorDashboard (.NET 8 + Vue 3)
        |
        | HTTPS with Bearer Token (internal)
        v
CrossMonitor Agent (on each machine: Linux, Windows, FreeBSD)
```

- **Frontend** never accesses agents directly
- **Frontend** never knows tokens
- **Tokens** stay only in backend configuration

## Stack

| Layer     | Technology                        |
|-----------|-----------------------------------|
| Backend   | .NET 8 Minimal API (C#)          |
| Frontend  | Vue 3, TypeScript, Vite           |
| Charts    | Apache ECharts / vue-echarts     |
| Icons     | Iconify (MDI)                    |
| Deploy    | Docker (single container)         |

## Nodes

The dashboard is configured to monitor these machines:

| ID              | Machine              | Type        | OS           |
|----------------|----------------------|-------------|--------------|
| m920q          | M920q - OPNsense     | firewall    | FreeBSD      |
| m92p           | M92p - Docker        | server      | Ubuntu       |
| m92p-display   | Tela 14 - Ubuntu     | display     | Ubuntu       |
| c6200          | C6200 - CasaOS       | nas         | Ubuntu       |
| windows-dayz   | Windows - DayZ       | game-server | Windows      |
| pc-gamer       | PC Gamer             | desktop     | Windows      |

## Configuration

1. Copy the example config:

```bash
cp config/dashboard.example.json config/dashboard.json
```

2. Edit `config/dashboard.json` and replace placeholder tokens with real tokens:

```json
{
  "visual": {
    "theme": "glass-blue",
    "refreshSeconds": 5,
    ...
  },
  "nodes": [
    {
      "id": "m92p",
      "name": "M92p - Docker",
      "type": "server",
      "url": "http://192.168.10.10:9580",
      "token": "YOUR_REAL_TOKEN_HERE",
      "enabled": true
    }
  ]
}
```

3. The backend reads this file and uses tokens internally.

**Never commit `config/dashboard.json` with real tokens.**

## Token Protection

- Tokens are stored only in `config/dashboard.json` on the server
- Backend uses tokens internally when polling agents
- **No endpoint returns tokens to the frontend**
- **No tokens are ever logged**
- Frontend calls only `/api/dashboard/*` endpoints
- Frontend never knows agent URLs or tokens

## Development

### Prerequisites

- .NET 8 SDK
- Node.js 20+

### Frontend

```bash
cd src/CrossMonitorDashboard.Web
npm install
npm run dev
```

Vite dev server runs on port 5173 with proxy to backend.

### Backend

```bash
cd src/CrossMonitorDashboard.Api
DASHBOARD_CONFIG_PATH=../../config/dashboard.json dotnet run
```

Or copy `config/dashboard.example.json` to `config/dashboard.json` in the Api project directory.

### Full Development

In separate terminals:

```bash
# Terminal 1: Backend
cd src/CrossMonitorDashboard.Api
dotnet run

# Terminal 2: Frontend
cd src/CrossMonitorDashboard.Web
npm run dev
```

Access: http://localhost:5173 (Vite proxies /api to backend)

## Docker

### Build and Run

```bash
# Build
docker compose build

# Run
docker compose up -d

# View logs
docker compose logs -f
```

Access: http://localhost:9580

### Configuration with Docker

Edit `config/dashboard.json` on the host. Docker mounts this file as read-only.

### Stop

```bash
docker compose down
```

## Themes

Available themes:

| Theme            | Description                                |
|------------------|--------------------------------------------|
| glass-blue       | Default. Dark with blue glassmorphism      |
| neon-green       | Dark green terminal-style monitoring       |
| cyber-red        | Aggressive red/pink cyberpunk look         |
| terminal-green   | Classic green-on-black terminal aesthetic  |
| pixel-platformer | Retro 2D platformer game style             |

Switch themes from the Settings page.

## Dashboard API Endpoints

| Method | Endpoint                           | Description                    |
|--------|------------------------------------|--------------------------------|
| GET    | `/api/dashboard/nodes`             | All node summaries (no tokens) |
| GET    | `/api/dashboard/nodes/{id}`        | Node details (no tokens)       |
| GET    | `/api/dashboard/nodes/{id}/history`| Node history data              |
| GET    | `/api/dashboard/summary`           | Environment summary            |
| GET    | `/api/dashboard/themes`            | Available themes list          |
| GET    | `/api/dashboard/config/public`     | Safe visual config (no tokens) |
| POST   | `/api/dashboard/config/validate`   | Validate JSON config           |
| GET    | `/health`                          | Health check                   |

## Frontend Pages

| Route       | Description                    |
|-------------|--------------------------------|
| `/`         | Dashboard with node cards grid |
| `/nodes/:id`| Node detail page with charts   |
| `/settings` | Visual and display settings    |
| `/editor`   | JSON configuration editor      |
| `/about`    | Architecture and tech info     |

## Security

1. Tokens never leave the backend
2. No token exposure in any API response
3. No token logging
4. Frontend never calls agents directly
5. Config file with real tokens is in `.gitignore`
6. Configuration validation warns about token exposure

## Next Steps

- [ ] Configuration editor save/load functionality
- [ ] Card layout customization and drag reorder
- [ ] User authentication for dashboard access
- [ ] Persistent settings storage
- [ ] Notification/alerts system
- [ ] More chart types and metrics
- [ ] Multi-language support
