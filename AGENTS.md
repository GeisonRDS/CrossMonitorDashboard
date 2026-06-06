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

27. **Line and bar chart windows align right.** Empty positions stay on the left, the newest sample appears on the far right, and older samples shift left.

28. **Node cards use six metric blocks.** Keep CPU, TEMP, RAM, DISCO, DOWNLOAD, and UPLOAD visible when working on card layout.

29. **Warning and critical cards must explain the cause.** Highlight the metric responsible for warning/critical when it can be calculated from dashboard data.

30. **Chart right-alignment uses `alignRight` function in `utils/chart.ts`.** Line and bar charts always have 10 fixed positions. Empty positions are null, actual data is right-aligned. Newest point always at far right.

31. **Line chart rendering rules.** `connectNulls: false`, `smooth` enabled only when >= 2 non-null points, `showSymbol: true` always, `areaStyle` hidden when < 2 non-null points.

32. **Card status reason priority.** CPU >= 90, RAM >= 90, any disk >= 90, any temperature >= 85 → critical. CPU >= 75, RAM >= 75, any disk >= 80, any temperature >= 70, collector errors, system errors → warning. All checks use the full list from node details, not just primary values.

33. **Causer metric highlighting.** When a metric triggers critical/warning, its tile gets `is-causer` class with stronger border/glow, and a pulsing status chip (CRITICAL/WARNING) appears next to its value.

34. **Card status shows only ONLINE or OFFLINE text.** Badge color may still derive from `node.status` for visual severity, but text must never show CRITICAL or WARNING. An inline SVG alert icon appears to the left of ONLINE when any detail item (visible or not) needs attention.

35. **Card border and tile colors only react to visible metrics.** CPU, TEMP, RAM, DISCO, DOWNLOAD, UPLOAD thresholds control the card visual state; auxiliary sensors (non-primary disks/temperatures, collector errors, system errors) trigger the alert icon in the badge but do NOT change the card border or tile colors.

36. **Background images must be local only.** Use files from `public/backgrounds/` (jpg, png, webp, max 2MB). External URLs are stripped by `sanitizeImagePath` in `useTheme.ts`. Never load background images from external URLs.

37. **Select/option elements must be legible in all themes.** Apply `--bg-card`/`--border-color`/`--text-primary` with `!important` in global CSS. Every theme's colors must produce readable select elements and dropdown options.

38. **Card shows only ONLINE or OFFLINE text.** CRITICAL/WARNING must never appear as text on the card badge.

39. **Alert icon must be outside the status badge.** The SVG alert icon sits to the left of the badge, not inside it. Icon uses `var(--warning)` color and `--glow-warning` drop shadow.

40. **About screen and menu must not be reintroduced without authorization.** The About route, view, and sidebar entry are removed.

41. **Internationalization (i18n) is required.** All fixed UI text must use the `useI18n` composable with locale files in `src/locales/`. Supported locales: `en` and `pt`. Machine names, API data, sensor names, and technical values are not translated.

42. **New themes must maintain legibility across all components.** Cards, sidebar, selects, charts, details, and the settings page must work correctly in every theme.

43. **Pixel Platformer theme** must use block/pixel aesthetic with 2D platformer colors (dark base, red/orange accents), pixel fonts (`Press Start 2P`), hard shadows, and solid borders.

44. **Background images must be from `public/backgrounds/` only.** External URLs are rejected by `sanitizeImagePath()`. Register new backgrounds in `src/config/backgrounds.ts`.

45. **Total theme count is 16.** Do not remove existing themes. New themes added: `terminal-mono`, `terminal-blue`, `terminal-red`, `terminal-green-matte`, `material-slate`, `material-graphite`, `material-ocean`, `material-forest`, `hacker-prompt`, `code-editor`, `black-white`.

46. **Icons must use `var(--text-primary)` or `var(--text-secondary)` colors.** Never rely on `:deep()` or `filter: drop-shadow()` for SVG icons. Use `fill: currentColor` and `stroke: none` directly on SVG elements.

47. **Restricted-palette themes must use only their allowed colors.**
    - `terminal-mono`: black, gray, white only
    - `terminal-blue`: black, gray, blue only
    - `terminal-red`: black, gray, red only
    - `terminal-green-matte`: black, gray, green only
    - `black-white`: black and white only
    Success/warning/critical values must use only palette colors. Use border style/weight to differentiate, not out-of-palette colors.

48. **`black-white` theme must use pure black and white.** No gray tones allowed as accent/primary/secondary. Minimal gray for text-muted is acceptable for readability.

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
│           ├── utils/               # Utility functions (chart alignment, etc.)
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
