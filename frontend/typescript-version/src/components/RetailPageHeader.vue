<script setup lang="ts">
defineProps<{
  title: string
  subtitle: string
  eyebrow?: string
  actionLabel?: string
  actionIcon?: string
}>()
</script>

<template>
  <div class="retail-page-header d-flex align-start justify-space-between flex-wrap mb-6">
    <div class="me-4">
      <div
        v-if="eyebrow"
        class="text-overline text-primary font-weight-bold mb-1"
      >
        {{ eyebrow }}
      </div>
      <h1 class="text-h4 text-md-h3 font-weight-bold mb-2">
        {{ title }}
      </h1>
      <p class="text-body-1 text-medium-emphasis mb-0">
        {{ subtitle }}
      </p>
    </div>

    <slot name="actions">
      <VBtn
        v-if="actionLabel"
        color="primary"
        :prepend-icon="actionIcon"
      >
        {{ actionLabel }}
      </VBtn>
    </slot>
  </div>
</template>

<style scoped>
.retail-page-header {
  gap: 1rem;
}

.retail-page-header p {
  max-inline-size: 760px;
}
</style>
