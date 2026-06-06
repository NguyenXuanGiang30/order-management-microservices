<script setup lang="ts">
import { statusColor } from '@/data/retailOps'

const props = defineProps<{
  status: string
}>()
</script>

<template>
  <VChip
    :color="statusColor(props.status)"
    size="small"
    variant="tonal"
    class="font-weight-medium"
  >
    {{ status }}
  </VChip>
</template>
