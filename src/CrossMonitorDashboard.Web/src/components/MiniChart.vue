<script setup lang="ts">
import { computed } from 'vue'
import VChart from 'vue-echarts'
import 'echarts'

const props = withDefaults(defineProps<{
  data: number[]
  color?: string
  height?: number
  type?: 'line' | 'bar'
}>(), {
  color: '#3c8ce6',
  height: 40,
  type: 'line'
})

function resolveColor(color: string) {
  if (!color.startsWith('var(')) return color
  const name = color.slice(4, -1).trim()
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || '#3c8ce6'
}

function hexToRgba(color: string, alpha: number) {
  const resolved = resolveColor(color)
  if (!resolved.startsWith('#')) return resolved
  const hex = resolved.replace('#', '')
  const normalized = hex.length === 3 ? hex.split('').map(c => c + c).join('') : hex
  const value = Number.parseInt(normalized, 16)
  const r = (value >> 16) & 255
  const g = (value >> 8) & 255
  const b = value & 255
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

const chartOptions = computed(() => {
  const color = resolveColor(props.color)
  return {
  grid: {
    left: 0,
    right: 0,
    top: 2,
    bottom: 2
  },
  xAxis: {
    type: 'category',
    show: false,
    data: props.data.map((_, i) => i)
  },
  yAxis: {
    type: 'value',
    show: false,
    min: 0,
    max: 100
  },
  series: [{
    type: props.type,
    data: props.data,
    smooth: true,
    showSymbol: false,
    lineStyle: {
      width: 1.5,
      color
    },
    areaStyle: {
      color: {
        type: 'linear',
        x: 0, y: 0, x2: 0, y2: 1,
        colorStops: [
          { offset: 0, color: hexToRgba(props.color, 0.32) },
          { offset: 1, color: hexToRgba(props.color, 0.03) }
        ]
      }
    },
    itemStyle: {
      color
    },
    barWidth: '60%'
  }],
  tooltip: { show: false }
  }
})
</script>

<template>
  <VChart :option="chartOptions" :style="{ height: props.height + 'px', width: '100%' }" autoresize />
</template>
