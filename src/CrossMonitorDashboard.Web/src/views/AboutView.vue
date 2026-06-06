<script setup lang="ts">
</script>

<template>
  <div class="about-page fade-in">
    <h1 class="page-title">About CrossMonitor Dashboard</h1>

    <div class="about-card glass-card">
      <div class="about-header">
        <h2>CrossMonitor</h2>
        <span class="version">v1.0.0</span>
      </div>
      <p class="description">
        Cross-platform distributed system monitoring dashboard. Monitor CPU, memory, disk, network, and temperature across multiple nodes in real-time.
      </p>
    </div>

    <div class="about-card glass-card">
      <h3>Architecture</h3>
      <pre class="arch-diagram">
┌─────────────────────────────────────────────────────┐
│                 CrossMonitor Dashboard               │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐│
│  │Dashboard│  │ Details │  │Settings │  │  JSON   ││
│  │  View   │  │  View   │  │  View   │  │ Editor  ││
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘│
│       └─────────────┴───────────┴─────────────┘     │
│                        │                             │
│              ┌─────────┴─────────┐                   │
│              │   Vue Router      │                   │
│              └─────────┬─────────┘                   │
│                        │                             │
│              ┌─────────┴─────────┐                   │
│              │  Dashboard Store  │                   │
│              └─────────┬─────────┘                   │
│                        │                             │
│              ┌─────────┴─────────┐                   │
│              │   API Layer       │                   │
│              └─────────┬─────────┘                   │
└────────────────────────┼─────────────────────────────┘
                         │ HTTP / WebSocket
┌────────────────────────┼─────────────────────────────┐
│              ┌─────────┴─────────┐                   │
│              │  Backend (Go/.NET)│                   │
│              └─────────┬─────────┘                   │
│                        │                             │
│         ┌──────────────┼──────────────┐              │
│  ┌──────┴──────┐ ┌─────┴─────┐ ┌─────┴─────┐       │
│  │   Node 1    │ │  Node 2   │ │  Node N   │       │
│  │  (Agent)    │ │  (Agent)  │ │  (Agent)  │       │
│  └─────────────┘ └───────────┘ └───────────┘       │
└─────────────────────────────────────────────────────┘
      </pre>
    </div>

    <div class="about-card glass-card">
      <h3>Tech Stack</h3>
      <div class="tech-list">
        <div class="tech-item">
          <span class="tech-name">Frontend</span>
          <span class="tech-value">Vue 3, TypeScript, Vite</span>
        </div>
        <div class="tech-item">
          <span class="tech-name">Charts</span>
          <span class="tech-value">ECharts, vue-echarts</span>
        </div>
        <div class="tech-item">
          <span class="tech-name">Icons</span>
          <span class="tech-value">Iconify (MDI)</span>
        </div>
        <div class="tech-item">
          <span class="tech-name">Routing</span>
          <span class="tech-value">Vue Router 4</span>
        </div>
        <div class="tech-item">
          <span class="tech-name">State</span>
          <span class="tech-value">Vue reactive (lightweight)</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.about-page {
  max-width: 700px;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.page-title {
  font-size: 1.3rem;
  font-weight: 600;
}

.about-card {
  padding: 1.25rem;
}

.about-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.about-header h2 {
  font-size: 1.2rem;
  font-weight: 700;
  color: var(--accent);
}

.version {
  font-family: var(--font-mono);
  font-size: 0.75rem;
  color: var(--text-muted);
  background: var(--bg-card);
  padding: 2px 8px;
  border-radius: 4px;
  border: 1px solid var(--border-color);
}

.description {
  font-size: 0.85rem;
  line-height: 1.6;
  color: var(--text-secondary);
}

.arch-diagram {
  font-family: var(--font-mono);
  font-size: 0.6rem;
  line-height: 1.3;
  color: var(--text-secondary);
  overflow-x: auto;
  white-space: pre;
  background: rgba(0, 0, 0, 0.2);
  padding: 0.75rem;
  border-radius: 8px;
}

.about-card h3 {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-secondary);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 0.75rem;
}

.tech-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.tech-item {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  padding: 6px 0;
  border-bottom: 1px solid var(--border-color);
}

.tech-item:last-child {
  border-bottom: none;
}

.tech-name {
  font-family: var(--font-mono);
  color: var(--text-muted);
}

.tech-value {
  color: var(--text-primary);
}
</style>
