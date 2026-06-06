# CrossMonitorDashboard

Cross-platform distributed system monitoring dashboard. Real-time monitoring of CPU, memory, disk, network, and temperature across multiple nodes.

CrossMonitorDashboard is separate from CrossMonitor Agent. The Agent runs on monitored machines and exposes `/api/v1/*`; the Dashboard runs centrally and exposes only `/api/dashboard/*` to the browser.

## Architecture

```
Browser (14" Ubuntu Desktop / any device)
        |
        | HTTP (no direct agent access)
        v
CrossMonitorDashboard (.NET 8 + Vue 3, external port 9590)
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

2. Edit local `config/dashboard.json` and replace placeholder tokens with real tokens:

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

**Never commit `config/dashboard.json` with real tokens.** Only `config/dashboard.example.json` is versioned.

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

Access: http://localhost:9590

### Configuration with Docker

Edit `config/dashboard.json` on the host. Docker mounts this file as read-only.

The container listens internally on `9580`; Docker Compose publishes it externally as `9590` to avoid conflicts with CrossMonitor Agents that use `9580`.

### Validate Runtime

```bash
curl http://localhost:9590/health
curl http://localhost:9590/api/dashboard/nodes
curl http://localhost:9590/api/dashboard/summary
curl http://localhost:9590/api/dashboard/themes
```

On PowerShell, use `Invoke-RestMethod` if `curl` is aliased.

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

Theme and visual preferences are stored locally in the browser under `crossmonitor-dashboard-visual-settings`. This storage contains only safe UI preferences such as theme, card size, refresh interval, animation settings, and background options. Tokens and node configuration are never stored in the frontend.

Metric chart preferences are also stored in the same local key. Supported chart types are:

| Type | Inspired by | Use |
|------|-------------|-----|
| `line-glow` | `grafico1` | Smooth history line with glow |
| `radial-gauge` | `grafico2` | Donut/radial gauge for current value |
| `bar-pulse` | `grafico3` | Historical vertical bars |

The Settings page lets you choose a chart type for CPU, memory, disk, temperature, and network. Cards show one visual representation per metric: label, numeric value, and the selected chart.

Line and bar charts display a sliding visual window of the latest 10 points. New samples enter from the right, older samples leave on the left, and radial gauges show only the current value.

The JSON editor was removed from the UI. Local node configuration continues to live in `config/dashboard.json` on the backend host only.

Visual direction is inspired by local references in `docs/design-references`: dark NOC dashboards, glass cards, strong icon contrast, neon glows, integrated charts, and a readable pixel-platformer variant. These images are references only and are not copied or loaded as external assets.

The main dashboard intentionally has no top header, hero banner, or summary section above the cards. Details remain accessible by clicking the full node card; there is no separate Nodes menu item.

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

## CrossMonitor Agent Contract

The dashboard consumes these protected Agent endpoints internally with `Authorization: Bearer <node-token>`:

| Agent Endpoint | Dashboard Use |
|----------------|---------------|
| `/api/v1/system` | Agent, host, CPU, memory, disks, network, temperatures, errors, timestamp |
| `/api/v1/status` | Readiness, snapshot timing, collector status/errors |
| `/api/v1/version` | Agent name/version/API/build metadata |

Metrics come from `/api/v1/system`. `/api/v1/status` does not contain CPU, memory, disk, network, or temperature metrics.

## Frontend Pages

| Route       | Description                    |
|-------------|--------------------------------|
| `/`         | Dashboard with node cards grid |
| `/nodes/:id`| Node detail page with charts   |
| `/settings` | Visual and display settings    |
| `/about`    | Architecture and tech info     |

## Visual Validation

After running Docker, open http://localhost:9590 and verify:

- Three node cards appear and update without page reload
- Sidebar icons are clear and active/hover states are visible
- Clicking a full card opens the details page
- Details charts render after polling history is available
- Theme selection persists after reload
- Chart type selection per metric persists after reload
- Memory, CPU, disk, and temperature do not duplicate bar + chart in the same card
- `pixel-platformer` remains available as a theme

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
