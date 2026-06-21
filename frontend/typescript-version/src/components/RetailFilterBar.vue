<script setup lang="ts">
export interface FilterConfig {
  key: string
  label: string
  items: Array<{ title: string; value: any }>
  modelValue: any
}

const props = defineProps<{
  modelValue?: string
  searchPlaceholder?: string
  filters?: FilterConfig[]
  loading?: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'search': []
  'reload': []
  filterChange: [key: string, value: any]
}>()

const searchText = computed({
  get: () => props.modelValue ?? '',
  set: (val: string) => emit('update:modelValue', val),
})
</script>

<template>
  <VCardText class="retail-filter-bar">
    <VTextField
      v-model="searchText"
      :placeholder="searchPlaceholder || 'Tìm kiếm...'"
      prepend-inner-icon="ri-search-line"
      hide-details="auto"
      density="comfortable"
      style="min-inline-size: 240px; flex: 1;"
      @keyup.enter="emit('search')"
    />

    <VSelect
      v-for="filter in filters"
      :key="filter.key"
      :model-value="filter.modelValue"
      :label="filter.label"
      :items="filter.items"
      item-title="title"
      item-value="value"
      hide-details="auto"
      density="comfortable"
      style="min-inline-size: 180px; max-inline-size: 240px;"
      @update:model-value="emit('filterChange', filter.key, $event)"
    />

    <VBtn
      variant="tonal"
      prepend-icon="ri-refresh-line"
      :loading="loading"
      @click="emit('reload')"
    >
      Tải lại
    </VBtn>

    <slot name="actions" />
  </VCardText>
</template>
