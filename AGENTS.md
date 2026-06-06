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
