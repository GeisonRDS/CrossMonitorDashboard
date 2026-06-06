# CrossMonitorDashboard - Agent Rules

## Critical Rules

1. **Never expose tokens in the frontend.** Tokens must only exist in backend configuration.

2. **Never commit config with real tokens.** Only `dashboard.example.json` with placeholders.

3. **Never log tokens.** Backend must not log token values in any log level.

4. **Frontend never calls CrossMonitor directly.** All communication goes through the dashboard backend.

5. **Backend consults agents internally.** Backend uses HttpClient with Bearer tokens.

6. **Dashboard API never returns tokens.** Endpoints return only safe data.

7. **Preserve responsiveness.** All pages must work on 14" screens, desktops, tablets, and phones.

8. **Preserve theme system.** Never remove existing themes. Add new themes via CSS variables.

9. **No unnecessary dependencies.** Keep the stack lean.

10. **No database in first version.** Use in-memory state with history.

11. **Models must match the real CrossMonitor Agent JSON.** Do not invent simplified contracts.

12. **`/api/v1/system` contains metrics.** Agent, host, CPU, memory, disks, network, temperatures, errors, and timestamp come from this endpoint.

13. **`/api/v1/status` contains collector state only.** It has ready/snapshot/interval/collectors; it does not have CPU, memory, disks, network, or temperatures.

14. **`/api/v1/version` contains version metadata.** Name, version, API version, build commit/date, and Go version belong here.

15. **Keep node cards fully clickable.** The user approved opening details by clicking the whole card.

16. **Do not reintroduce the JSON editor UI without explicit authorization.** Backend validation endpoints may remain if safe, but the frontend must not expose the editor screen.

17. **Themes must persist locally.** Store only safe visual preferences in localStorage; never store tokens, node URLs with secrets, or full backend config.

18. **Design references are inspiration only.** Images in `docs/design-references` may guide visual style but must not be copied literally or loaded from paid/external assets.

19. **Validate details charts.** CPU, memory, disk, temperature, and network history charts must render or show a clear empty-history state.

20. **Do not reintroduce the Nodes menu icon/screen without explicit authorization.** Details remain accessible by clicking node cards.

21. **Do not reintroduce a hero/summary section above dashboard cards.** The main dashboard should open directly on the card grid for 14" screens.

22. **Do not duplicate metric visualizations in a card.** A metric may show label/value plus one selected chart type, not a separate bar and graph for the same metric.

23. **Metric chart preferences must persist locally.** Store chart types per metric in `crossmonitor-dashboard-visual-settings`; never store tokens or full backend config.

24. **Charts should update with history, not reset visually per polling cycle.** Avoid keys/dispose/reinit patterns that recreate ECharts unless necessary.

25. **Line and bar charts show a 10-point visual window.** New points enter from the right and old points leave on the left; radial charts show only the current value.

26. **Sidebar icons must remain visible in every theme.** Keep high contrast for icon SVG color/fill/stroke and do not reintroduce Nodes or JSON editor menu items.

## Development Workflow

1. Branch: `develop` for all work. Main branch is protected.

2. Before building: restore dependencies.
   ```bash
   cd src/CrossMonitorDashboard.Api && dotnet restore
   cd src/CrossMonitorDashboard.Web && npm install
   ```

3. Build validation:
   ```bash
   # Backend
   cd src/CrossMonitorDashboard.Api && dotnet build

   # Frontend
   cd src/CrossMonitorDashboard.Web && npm run build
   ```

4. Type checking:
   ```bash
   cd src/CrossMonitorDashboard.Web && npx vue-tsc --noEmit
   ```

5. No commit without authorization.
6. No push without authorization.
7. No merge without authorization.
8. Before commit and merge, ensure working tree is clean.
9. Do not merge to main with pending files.

10. Before concluding Docker work, run `docker compose up -d --build` and validate `/health`, `/api/dashboard/nodes`, and `/api/dashboard/summary` on external port `9590`.

11. Keep `config/dashboard.json` ignored and never stage it.

## Project Structure

```
CrossMonitorDashboard/
├── src/
│   ├── CrossMonitorDashboard.Api/   # .NET 8 Minimal API
│   │   ├── Models/                  # Data models
│   │   ├── Services/                # Config, polling, dashboard services
│   │   └── Program.cs               # App setup and endpoints
│   └── CrossMonitorDashboard.Web/   # Vue 3 + Vite + TypeScript
│       └── src/
│           ├── api/                 # API client
│           ├── components/          # Vue components
│           ├── composables/         # Composable functions
│           ├── router/              # Vue Router
│           ├── stores/              # Reactive state
│           ├── styles/              # CSS themes and globals
│           ├── types/               # TypeScript interfaces
│           └── views/               # Page components
├── config/
│   └── dashboard.example.json       # Example config with placeholders
├── docker-compose.yml
└── Dockerfile
```

## Theme Guidelines

Themes use CSS custom properties on `[data-theme="..."]` selector.

Required variables:
- `--bg-primary`, `--bg-card`, `--bg-card-hover`, `--bg-sidebar`
- `--text-primary`, `--text-secondary`, `--text-muted`
- `--border-color`, `--border-glow`
- `--accent`, `--accent-light`, `--accent-dark`
- `--success`, `--warning`, `--critical`, `--offline`
- `--glow-success`, `--glow-warning`, `--glow-critical`, `--glow-accent`
- `--font-mono`, `--font-main`
- `--card-radius`, `--card-blur`, `--transition-speed`

## API Contract

All dashboard API endpoints are under `/api/dashboard/`.

Never add endpoints that return tokens.

Never add proxy endpoints that forward to agents.

## Glassmorphism Guidelines

Cards use `.glass-card` class with:
- `background: var(--bg-card)`
- `backdrop-filter: blur(var(--card-blur))`
- `border: 1px solid var(--border-color)`
- `border-radius: var(--card-radius)`
