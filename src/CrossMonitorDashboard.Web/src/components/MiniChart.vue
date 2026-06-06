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

const chartOptions = computed(() => ({
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
      color: props.color
    },
    areaStyle: {
      color: {
        type: 'linear',
        x: 0, y: 0, x2: 0, y2: 1,
        colorStops: [
          { offset: 0, color: props.color + '40' },
          { offset: 1, color: props.color + '05' }
        ]
      }
    },
    itemStyle: {
      color: props.color
    },
    barWidth: '60%'
  }],
  tooltip: { show: false }
}))
</script>

<template>
  <VChart :option="chartOptions" :style="{ height: height + 'px', width: '100%' }" autoresize />
</template>
