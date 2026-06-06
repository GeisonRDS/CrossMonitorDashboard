<script setup lang="ts">
import { ref } from 'vue'
import { validateConfig } from '../api/dashboard'

const jsonContent = ref('{\n  "$schema": "./config.schema.json",\n  "theme": "glass-blue",\n  "refreshSeconds": 5,\n  "cardSize": "normal"\n}')
const validationResult = ref<{ valid: boolean; errors: string[] } | null>(null)
const validating = ref(false)

function loadExample() {
  jsonContent.value = JSON.stringify({
    theme: 'glass-blue',
    refreshSeconds: 5,
    cardSize: 'normal',
    animations: { enabled: true, intensity: 'medium' },
    background: { type: 'gradient', imagePath: '', opacity: 0.8, blur: 12, overlay: 0.5 }
  }, null, 2)
}

async function validateJson() {
  validating.value = true
  validationResult.value = null
  try {
    const parsed = JSON.parse(jsonContent.value)
    validationResult.value = await validateConfig(parsed)
  } catch (e: any) {
    validationResult.value = { valid: false, errors: [e.message || 'Invalid JSON'] }
  } finally {
    validating.value = false
  }
}
</script>

<template>
  <div class="editor-page fade-in">
    <h1 class="page-title">JSON Editor</h1>

    <div class="editor-warning glass-card">
      <p>⚠ Never paste or expose tokens, passwords, or secrets in this editor. Configuration is stored locally.</p>
    </div>

    <div class="editor-layout">
      <div class="editor-panel glass-card">
        <div class="editor-header">
          <h3>config.json</h3>
          <div class="editor-actions">
            <button class="action-btn" @click="loadExample">Load Example</button>
            <button class="action-btn primary" @click="validateJson" :disabled="validating">
              {{ validating ? 'Validating...' : 'Validate' }}
            </button>
          </div>
        </div>
        <textarea
          v-model="jsonContent"
          class="editor-textarea"
          spellcheck="false"
          placeholder="Enter JSON configuration..."
        ></textarea>
      </div>

      <div v-if="validationResult" class="validation-panel glass-card" :class="validationResult.valid ? 'valid' : 'invalid'">
        <h3>{{ validationResult.valid ? '✓ Valid Configuration' : '✗ Validation Errors' }}</h3>
        <ul v-if="!validationResult.valid">
          <li v-for="err in validationResult.errors" :key="err">{{ err }}</li>
        </ul>
      </div>
    </div>
  </div>
</template>

<style scoped>
.editor-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  max-width: 900px;
}

.page-title {
  font-size: 1.3rem;
  font-weight: 600;
}

.editor-warning {
  padding: 0.75rem 1rem;
  font-size: 0.8rem;
  background: rgba(255, 171, 0, 0.1);
  border-color: var(--warning);
  color: var(--warning);
}

.editor-layout {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.editor-panel {
  padding: 1rem;
}

.editor-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
}

.editor-header h3 {
  font-size: 0.8rem;
  font-family: var(--font-mono);
  color: var(--text-secondary);
}

.editor-actions {
  display: flex;
  gap: 0.5rem;
}

.action-btn {
  padding: 0.4rem 0.8rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-radius: 6px;
  font-size: 0.75rem;
  font-family: var(--font-mono);
  cursor: pointer;
  transition: all var(--transition-speed);
}

.action-btn:hover {
  background: var(--bg-card-hover);
  border-color: var(--border-glow);
}

.action-btn.primary {
  background: var(--accent);
  border-color: var(--accent);
  color: #fff;
}

.action-btn.primary:hover {
  background: var(--accent-light);
}

.editor-textarea {
  width: 100%;
  height: 400px;
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  padding: 1rem;
  resize: vertical;
  line-height: 1.5;
  tab-size: 2;
}

.editor-textarea:focus {
  outline: none;
  border-color: var(--accent);
}

.validation-panel {
  padding: 1rem;
}

.validation-panel.valid {
  border-color: var(--success);
  background: rgba(0, 200, 83, 0.05);
}

.validation-panel.invalid {
  border-color: var(--critical);
  background: rgba(255, 23, 68, 0.05);
}

.validation-panel h3 {
  font-size: 0.85rem;
  margin-bottom: 0.5rem;
}

.validation-panel ul {
  list-style: none;
  padding: 0;
}

.validation-panel li {
  font-size: 0.8rem;
  font-family: var(--font-mono);
  color: var(--critical);
  padding: 4px 0;
  border-bottom: 1px solid var(--border-color);
}
</style>
