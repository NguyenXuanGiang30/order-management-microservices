<script setup lang="ts">
import type { MetricItem } from '@/data/retailOps'

defineProps<{
  metric: MetricItem
}>()
</script>

<template>
  <VCard class="retail-metric-card h-100">
    <VCardText class="retail-metric-card__body">
      <div class="d-flex align-start justify-space-between ga-4 mb-5">
        <div class="min-w-0">
          <div class="retail-metric-card__label">
            {{ metric.label }}
          </div>
          <div class="retail-metric-card__value">
            {{ metric.value }}
          </div>
        </div>
        <VAvatar
          :color="metric.color"
          variant="tonal"
          rounded="lg"
          size="46"
          class="retail-metric-card__icon"
        >
          <VIcon
            :icon="metric.icon"
            size="23"
          />
        </VAvatar>
      </div>

      <div class="retail-metric-card__footer">
        <VIcon
          icon="ri-information-line"
          size="16"
        />
        <span>{{ metric.helper }}</span>
      </div>
    </VCardText>
  </VCard>
</template>

<style scoped>
.retail-metric-card {
  position: relative;
  overflow: hidden;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  background:
    radial-gradient(circle at 100% 0, rgba(var(--v-theme-primary), 0.1), transparent 34%),
    rgb(var(--v-theme-surface));
  box-shadow: none;
  transition: transform 220ms cubic-bezier(0.22, 1, 0.36, 1), border-color 220ms ease, box-shadow 220ms ease;
}

.retail-metric-card::before {
  position: absolute;
  border-radius: 999px;
  background: rgba(var(--v-theme-primary), 0.48);
  block-size: 3px;
  content: '';
  inline-size: 44px;
  inset-block-start: 0;
  inset-inline-start: 1.25rem;
}

.retail-metric-card__body {
  display: flex;
  flex-direction: column;
  block-size: 100%;
}

.retail-metric-card__label {
  color: rgba(var(--v-theme-on-surface), 0.64);
  font-size: 0.8125rem;
  font-weight: 700;
}

.retail-metric-card__value {
  color: rgba(var(--v-theme-on-surface), 0.94);
  font-size: clamp(1.35rem, 2vw, 1.65rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
  margin-block-start: 0.375rem;
}

.retail-metric-card__icon {
  box-shadow: inset 0 0 0 1px rgba(var(--v-theme-on-surface), 0.04);
}

.retail-metric-card__footer {
  display: flex;
  align-items: center;
  padding-block-start: 0.875rem;
  border-block-start: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  margin-block-start: auto;
  color: rgba(var(--v-theme-on-surface), 0.62);
  column-gap: 0.5rem;
  font-size: 0.8125rem;
  line-height: 1.35;
}

.retail-metric-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.28);
  box-shadow: 0 18px 42px rgba(15, 23, 42, 0.1) !important;
  transform: translateY(-4px);
}

@media (prefers-reduced-motion: reduce) {
  .retail-metric-card {
    transition: none;
  }

  .retail-metric-card:hover {
    transform: none;
  }
}
</style>
