<script setup lang="ts">
import { computed } from 'vue'
import VChart from 'vue-echarts'
import 'echarts'

export type MetricChartType = 'line-glow' | 'radial-gauge' | 'bar-pulse'

const props = withDefaults(defineProps<{
  data: number[]
  color?: string
  secondaryColor?: string
  height?: number
  chartType?: MetricChartType
  max?: number
  unit?: string
  compact?: boolean
}>(), {
  color: 'var(--accent)',
  secondaryColor: 'var(--accent-light)',
  height: 52,
  chartType: 'line-glow',
  max: 100,
  unit: '%',
  compact: true
})

function resolveColor(color: string) {
  if (!color.startsWith('var(')) return color
  const name = color.slice(4, -1).trim()
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || '#3c8ce6'
}

function toRgba(color: string, alpha: number) {
  const resolved = resolveColor(color)
  if (resolved.startsWith('rgb')) return resolved.replace(/rgba?\(([^)]+)\)/, (_m, body) => `rgba(${body.split(',').slice(0, 3).join(',')}, ${alpha})`)
  if (!resolved.startsWith('#')) return resolved
  const hex = resolved.replace('#', '')
  const normalized = hex.length === 3 ? hex.split('').map(c => c + c).join('') : hex
  const value = Number.parseInt(normalized, 16)
  const r = (value >> 16) & 255
  const g = (value >> 8) & 255
  const b = value & 255
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

const chartWindowValues = computed(() => props.data.slice(-10).map(v => Math.max(0, Math.min(props.max, Number(v) || 0))))
const radialValues = computed(() => props.data.slice(-1).map(v => Math.max(0, Math.min(props.max, Number(v) || 0))))
const values = computed(() => props.chartType === 'radial-gauge' ? radialValues.value : chartWindowValues.value)
const latest = computed(() => values.value.at(-1) ?? 0)

const chartOptions = computed(() => {
  const color = resolveColor(props.color)
  const secondary = resolveColor(props.secondaryColor)
  const textColor = resolveColor('var(--text-secondary)')

  if (props.chartType === 'radial-gauge') {
    return {
      animationDurationUpdate: 650,
      series: [{
        type: 'pie',
        radius: props.compact ? ['58%', '78%'] : ['62%', '82%'],
        center: ['50%', '50%'],
        startAngle: 90,
        silent: true,
        label: { show: false },
        labelLine: { show: false },
        data: [
          { value: latest.value, itemStyle: { color } },
          { value: Math.max(0, props.max - latest.value), itemStyle: { color: toRgba(props.secondaryColor, 0.18) } }
        ]
      }, {
        type: 'gauge',
        min: 0,
        max: props.max,
        center: ['50%', '50%'],
        radius: props.compact ? '52%' : '58%',
        pointer: { show: false },
        progress: { show: false },
        axisLine: { show: false },
        axisTick: { show: false },
        splitLine: { show: false },
        axisLabel: { show: false },
        detail: {
          valueAnimation: true,
          formatter: (value: number) => `${Math.round(value)}${props.unit}`,
          color: textColor,
          fontSize: props.compact ? 12 : 22,
          fontWeight: 800,
          offsetCenter: [0, 4]
        },
        data: [{ value: latest.value }]
      }]
    }
  }

  if (props.chartType === 'bar-pulse') {
    return {
      animationDurationUpdate: 520,
      grid: { left: 0, right: 0, top: 4, bottom: 0 },
      xAxis: { type: 'category', show: false, data: values.value.map((_, i) => i) },
      yAxis: { type: 'value', show: false, min: 0, max: props.max },
      series: [{
        type: 'bar',
        data: values.value,
        barWidth: props.compact ? '54%' : '44%',
        itemStyle: {
          borderRadius: [6, 6, 2, 2],
          color: {
            type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [
              { offset: 0, color: secondary },
              { offset: 1, color }
            ]
          },
          shadowBlur: 10,
          shadowColor: toRgba(props.color, 0.42)
        }
      }],
      tooltip: { show: false }
    }
  }

  return {
    animationDurationUpdate: 560,
    grid: { left: 0, right: 0, top: 4, bottom: 2 },
      xAxis: { type: 'category', show: false, data: values.value.map((_, i) => i) },
    yAxis: { type: 'value', show: false, min: 0, max: props.max },
    series: [{
      type: 'line',
      data: values.value,
      smooth: true,
      showSymbol: !props.compact && values.value.length < 18,
      symbolSize: 5,
      lineStyle: { width: props.compact ? 2 : 2.8, color, shadowBlur: 10, shadowColor: toRgba(props.color, 0.45) },
      itemStyle: { color },
      areaStyle: {
        color: {
          type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: toRgba(props.color, 0.28) },
            { offset: 1, color: toRgba(props.color, 0.02) }
          ]
        }
      }
    }],
    tooltip: { show: false }
  }
})
</script>

<template>
  <VChart :option="chartOptions" :style="{ height: `${height}px`, width: '100%' }" :update-options="{ notMerge: false, lazyUpdate: true }" autoresize />
</template>
