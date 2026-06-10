<script setup lang="ts">
import { computed } from 'vue'
import { useTheme, type BackgroundEffectIntensity } from '../composables/useTheme'

const { visualSettings } = useTheme()

const effect = computed(() => visualSettings.value.backgroundEffect)
const intensity = computed(() => visualSettings.value.backgroundEffectIntensity)

const opacityMap: Record<BackgroundEffectIntensity, number> = { low: 0.3, medium: 0.55, high: 0.8 }
const effectOpacity = computed(() => opacityMap[intensity.value] ?? 0.55)

const isReducedMotion = computed(() => {
  if (typeof window === 'undefined') return false
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches
})

const effectClass = computed(() => {
  if (effect.value === 'none' || !effect.value) return ''
  return `effect-${effect.value}`
})

const styleVars = computed(() => ({
  '--effect-opacity': effectOpacity.value,
  '--theme-accent': 'var(--accent)',
  '--theme-text': 'var(--text-primary)',
  '--theme-muted': 'var(--text-muted)',
  '--theme-border': 'var(--border-color)',
}))
</script>

<template>
  <div
    v-if="effect !== 'none'"
    class="background-effects"
    :class="[effectClass, { 'reduced-motion': isReducedMotion }]"
    :style="styleVars"
    aria-hidden="true"
  >
    <div v-if="effect === 'penguin'" class="penguin-scene">
      <div class="penguin-walk">
        <svg class="penguin-svg" viewBox="0 0 32 32" width="32" height="32" aria-hidden="true">
          <rect x="10" y="4" width="12" height="3" fill="var(--theme-text)" />
          <rect x="8" y="7" width="16" height="14" fill="var(--theme-text)" rx="1" />
          <rect x="11" y="9" width="10" height="8" fill="var(--theme-muted)" rx="1" />
          <rect x="12" y="4" width="2" height="2" fill="var(--theme-accent)" />
          <rect x="18" y="4" width="2" height="2" fill="var(--theme-accent)" />
          <rect x="14" y="21" width="4" height="2" fill="var(--theme-accent)" />
          <rect x="8" y="21" width="3" height="4" fill="var(--theme-accent)" />
          <rect x="21" y="21" width="3" height="4" fill="var(--theme-accent)" />
          <rect x="10" y="6" width="2" height="1" fill="var(--theme-accent)" />
          <rect x="20" y="6" width="2" height="1" fill="var(--theme-accent)" />
        </svg>
      </div>
      <div class="penguin-walk penguin-walk-2">
        <svg class="penguin-svg" viewBox="0 0 32 32" width="32" height="32" aria-hidden="true">
          <rect x="10" y="4" width="12" height="3" fill="var(--theme-text)" />
          <rect x="8" y="7" width="16" height="14" fill="var(--theme-text)" rx="1" />
          <rect x="11" y="9" width="10" height="8" fill="var(--theme-muted)" rx="1" />
          <rect x="12" y="4" width="2" height="2" fill="var(--theme-accent)" />
          <rect x="18" y="4" width="2" height="2" fill="var(--theme-accent)" />
          <rect x="14" y="21" width="4" height="2" fill="var(--theme-accent)" />
          <rect x="7" y="21" width="4" height="3" fill="var(--theme-accent)" />
          <rect x="21" y="22" width="3" height="3" fill="var(--theme-accent)" />
          <rect x="10" y="6" width="2" height="1" fill="var(--theme-accent)" />
          <rect x="20" y="6" width="2" height="1" fill="var(--theme-accent)" />
        </svg>
      </div>
    </div>

    <div v-else-if="effect === 'snake'" class="snake-scene">
      <div class="snake-segment snake-head"></div>
      <div class="snake-segment snake-body snake-b1"></div>
      <div class="snake-segment snake-body snake-b2"></div>
      <div class="snake-segment snake-body snake-b3"></div>
      <div class="snake-segment snake-body snake-b4"></div>
      <div class="snake-segment snake-body snake-b5"></div>
      <div class="snake-segment snake-body snake-b6"></div>
      <div class="snake-segment snake-body snake-b7"></div>
      <div class="snake-food"></div>
    </div>

    <div v-else-if="effect === 'solar-system'" class="solar-scene">
      <div class="sun"></div>
      <div class="orbit orbit-1">
        <div class="planet planet-1"></div>
      </div>
      <div class="orbit orbit-2">
        <div class="planet planet-2"></div>
      </div>
      <div class="orbit orbit-3">
        <div class="planet planet-3"></div>
      </div>
      <div class="orbit orbit-4">
        <div class="planet planet-4"></div>
      </div>
      <div class="star-field">
        <div v-for="i in 20" :key="i" class="star" :style="{ top: `${Math.random() * 100}%`, left: `${Math.random() * 100}%`, animationDelay: `${Math.random() * 4}s` }"></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.background-effects {
  position: fixed;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
  opacity: var(--effect-opacity);
  transition: opacity 0.5s ease;
}

.reduced-motion * {
  animation-duration: 0.01ms !important;
  animation-iteration-count: 1 !important;
  transition-duration: 0.01ms !important;
}

/* ===================== PENGUIN ===================== */

.penguin-scene {
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 80px;
}

.penguin-walk {
  position: absolute;
  bottom: 8px;
  animation: penguinWalkLeft linear 24s infinite;
  will-change: transform;
}

.penguin-walk-2 {
  animation-delay: -12s;
  animation-duration: 28s;
  opacity: 0.7;
}

@keyframes penguinWalkLeft {
  0% { transform: translateX(calc(100vw + 10px)) scaleX(1); }
  48% { transform: translateX(-40px) scaleX(1); }
  50% { transform: translateX(-40px) scaleX(-1); }
  98% { transform: translateX(calc(100vw + 10px)) scaleX(-1); }
  100% { transform: translateX(calc(100vw + 10px)) scaleX(1); }
}

.penguin-svg {
  display: block;
}

/* ===================== SNAKE ===================== */

.snake-scene {
  position: absolute;
  inset: 0;
}

.snake-segment {
  position: absolute;
  width: 12px;
  height: 12px;
  border-radius: 2px;
}

.snake-head {
  background: var(--theme-accent);
  animation: snakePathHead 16s linear infinite;
  will-change: transform;
  z-index: 1;
}

.snake-body {
  background: var(--theme-text);
  opacity: 0.5;
}

.snake-b1 { animation: snakePath1 16s linear infinite; }
.snake-b2 { animation: snakePath2 16s linear infinite; }
.snake-b3 { animation: snakePath3 16s linear infinite; }
.snake-b4 { animation: snakePath4 16s linear infinite; }
.snake-b5 { animation: snakePath5 16s linear infinite; }
.snake-b6 { animation: snakePath6 16s linear infinite; }
.snake-b7 { animation: snakePath7 16s linear infinite; }

.snake-food {
  position: absolute;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--theme-accent);
  opacity: 0.6;
  animation: snakeFood 16s linear infinite;
  will-change: transform;
}

@keyframes snakePathHead {
  0%, 3% { transform: translate(5vw, 20vh); }
  12% { transform: translate(5vw, 70vh); }
  25% { transform: translate(30vw, 70vh); }
  37% { transform: translate(30vw, 30vh); }
  50% { transform: translate(60vw, 30vh); }
  62% { transform: translate(60vw, 70vh); }
  75% { transform: translate(85vw, 70vh); }
  87% { transform: translate(85vw, 20vh); }
  97%, 100% { transform: translate(5vw, 20vh); }
}

@keyframes snakePath1 {
  0%, 5% { transform: translate(3vw, 20vh); }
  14% { transform: translate(3vw, 70vh); }
  27% { transform: translate(28vw, 70vh); }
  39% { transform: translate(28vw, 30vh); }
  52% { transform: translate(58vw, 30vh); }
  64% { transform: translate(58vw, 70vh); }
  77% { transform: translate(83vw, 70vh); }
  89% { transform: translate(83vw, 20vh); }
  100% { transform: translate(3vw, 20vh); }
}

@keyframes snakePath2 {
  0%, 7% { transform: translate(1vw, 22vh); }
  16% { transform: translate(1vw, 68vh); }
  29% { transform: translate(26vw, 68vh); }
  41% { transform: translate(26vw, 32vh); }
  54% { transform: translate(56vw, 32vh); }
  66% { transform: translate(56vw, 68vh); }
  79% { transform: translate(81vw, 68vh); }
  91% { transform: translate(81vw, 22vh); }
  100% { transform: translate(1vw, 22vh); }
}

@keyframes snakePath3 {
  0%, 9% { transform: translate(4vw, 24vh); }
  18% { transform: translate(4vw, 66vh); }
  31% { transform: translate(24vw, 66vh); }
  43% { transform: translate(24vw, 34vh); }
  56% { transform: translate(54vw, 34vh); }
  68% { transform: translate(54vw, 66vh); }
  81% { transform: translate(79vw, 66vh); }
  93% { transform: translate(79vw, 24vh); }
  100% { transform: translate(4vw, 24vh); }
}

@keyframes snakePath4 {
  0%, 11% { transform: translate(2vw, 26vh); }
  20% { transform: translate(2vw, 64vh); }
  33% { transform: translate(22vw, 64vh); }
  45% { transform: translate(22vw, 36vh); }
  58% { transform: translate(52vw, 36vh); }
  70% { transform: translate(52vw, 64vh); }
  83% { transform: translate(77vw, 64vh); }
  95% { transform: translate(77vw, 26vh); }
  100% { transform: translate(2vw, 26vh); }
}

@keyframes snakePath5 {
  0%, 13% { transform: translate(5vw, 28vh); }
  22% { transform: translate(5vw, 62vh); }
  35% { transform: translate(20vw, 62vh); }
  47% { transform: translate(20vw, 38vh); }
  60% { transform: translate(50vw, 38vh); }
  72% { transform: translate(50vw, 62vh); }
  85% { transform: translate(75vw, 62vh); }
  97% { transform: translate(75vw, 28vh); }
  100% { transform: translate(5vw, 28vh); }
}

@keyframes snakePath6 {
  0%, 15% { transform: translate(3vw, 30vh); }
  24% { transform: translate(3vw, 60vh); }
  37% { transform: translate(18vw, 60vh); }
  49% { transform: translate(18vw, 40vh); }
  62% { transform: translate(48vw, 40vh); }
  74% { transform: translate(48vw, 60vh); }
  87% { transform: translate(73vw, 60vh); }
  99% { transform: translate(73vw, 30vh); }
  100% { transform: translate(3vw, 30vh); }
}

@keyframes snakePath7 {
  0%, 17% { transform: translate(6vw, 32vh); }
  26% { transform: translate(6vw, 58vh); }
  39% { transform: translate(16vw, 58vh); }
  51% { transform: translate(16vw, 42vh); }
  64% { transform: translate(46vw, 42vh); }
  76% { transform: translate(46vw, 58vh); }
  89% { transform: translate(71vw, 58vh); }
  100% { transform: translate(6vw, 32vh); }
}

@keyframes snakeFood {
  0%, 24% { transform: translate(20vw, 45vh); opacity: 0; }
  25%, 35% { transform: translate(20vw, 45vh); opacity: 0.6; }
  45%, 74% { transform: translate(50vw, 55vh); opacity: 0; }
  75%, 85% { transform: translate(50vw, 55vh); opacity: 0.6; }
  95%, 100% { transform: translate(50vw, 55vh); opacity: 0; }
}

/* ===================== SOLAR SYSTEM ===================== */

.solar-scene {
  position: absolute;
  inset: 0;
}

.sun {
  position: absolute;
  bottom: 60px;
  left: 60px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--theme-accent);
  box-shadow: 0 0 20px var(--theme-accent), 0 0 40px var(--theme-accent);
  opacity: 0.8;
}

.orbit {
  position: absolute;
  bottom: 60px;
  left: 60px;
  border-radius: 50%;
  border: 1px dashed var(--theme-border);
  opacity: 0.4;
  animation: orbitRotate 20s linear infinite;
}

.orbit-1 {
  width: 70px;
  height: 70px;
  margin-left: -21px;
  margin-bottom: -21px;
  animation-duration: 8s;
}

.orbit-2 {
  width: 120px;
  height: 120px;
  margin-left: -46px;
  margin-bottom: -46px;
  animation-duration: 14s;
}

.orbit-3 {
  width: 180px;
  height: 180px;
  margin-left: -76px;
  margin-bottom: -76px;
  animation-duration: 22s;
}

.orbit-4 {
  width: 250px;
  height: 250px;
  margin-left: -111px;
  margin-bottom: -111px;
  animation-duration: 32s;
}

.planet {
  position: absolute;
  border-radius: 50%;
  top: 0;
  left: 50%;
  transform: translateX(-50%);
}

.planet-1 {
  width: 6px;
  height: 6px;
  background: var(--theme-text);
  opacity: 0.8;
}

.planet-2 {
  width: 8px;
  height: 8px;
  background: var(--theme-accent);
  opacity: 0.7;
  top: -1px;
}

.planet-3 {
  width: 6px;
  height: 6px;
  background: var(--theme-muted);
  opacity: 0.7;
}

.planet-4 {
  width: 5px;
  height: 5px;
  background: var(--theme-text);
  opacity: 0.5;
  top: 1px;
}

@keyframes orbitRotate {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.star-field {
  position: absolute;
  inset: 0;
  pointer-events: none;
}

.star {
  position: absolute;
  width: 2px;
  height: 2px;
  background: var(--theme-text);
  border-radius: 50%;
  opacity: 0;
  animation: starTwinkle 5s ease-in-out infinite;
}

@keyframes starTwinkle {
  0%, 100% { opacity: 0; }
  40%, 60% { opacity: 0.7; }
}

/* ===================== RESPONSIVE ===================== */

@media (max-width: 760px) {
  .penguin-scene {
    height: 60px;
  }
  .penguin-svg {
    width: 24px;
    height: 24px;
  }
  .snake-segment {
    width: 8px;
    height: 8px;
  }
  .sun {
    width: 20px;
    height: 20px;
    bottom: 30px;
    left: 30px;
  }
  .orbit {
    bottom: 30px;
    left: 30px;
  }
  .orbit-1 { width: 50px; height: 50px; margin-left: -15px; margin-bottom: -15px; }
  .orbit-2 { width: 90px; height: 90px; margin-left: -35px; margin-bottom: -35px; }
  .orbit-3 { width: 130px; height: 130px; margin-left: -55px; margin-bottom: -55px; }
  .orbit-4 { width: 180px; height: 180px; margin-left: -80px; margin-bottom: -80px; }
}
</style>
