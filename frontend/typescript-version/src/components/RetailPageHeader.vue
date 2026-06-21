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
  <div class="retail-page-header mb-6">
    <div class="retail-page-header__content">
      <div
        v-if="eyebrow"
        class="retail-page-header__eyebrow"
      >
        <VIcon
          icon="ri-sparkling-2-line"
          size="15"
        />
        <span>{{ eyebrow }}</span>
      </div>
      <h1 class="text-h4 text-md-h3 font-weight-bold mb-2">
        {{ title }}
      </h1>
      <p class="text-body-1 text-medium-emphasis mb-0">
        {{ subtitle }}
      </p>
    </div>

    <div class="retail-page-header__actions">
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
  </div>
</template>

<style scoped>
.retail-page-header {
  display: grid;
  align-items: start;
  padding: clamp(1.25rem, 2.5vw, 1.75rem);
  border: 1px solid rgba(var(--v-theme-primary), 0.14);
  border-radius: 22px;
  background:
    radial-gradient(circle at 0 0, rgba(var(--v-theme-primary), 0.14), transparent 34%),
    linear-gradient(135deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.86));
  box-shadow: 0 22px 60px rgba(15, 23, 42, 0.08);
  gap: 1.25rem;
  grid-template-columns: minmax(0, 1fr) auto;
}

.retail-page-header__content p {
  max-inline-size: 760px;
}

.retail-page-header__eyebrow {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.625rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.18);
  border-radius: 999px;
  margin-block-end: 0.75rem;
  background: rgba(var(--v-theme-primary), 0.08);
  color: rgb(var(--v-theme-primary));
  column-gap: 0.375rem;
  font-size: 0.75rem;
  font-weight: 700;
}

.retail-page-header__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.75rem;
}

@media (max-width: 767px) {
  .retail-page-header {
    grid-template-columns: 1fr;
  }

  .retail-page-header__actions {
    justify-content: flex-start;
  }
}
</style>
