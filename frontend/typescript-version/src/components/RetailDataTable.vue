<script setup lang="ts">
defineProps<{
  headers: Array<{ title: string; key: string; align?: string; width?: string }>
  loading?: boolean
  emptyText?: string
  emptyIcon?: string
}>()
</script>

<template>
  <div>
    <!-- Loading State -->
    <VCardText
      v-if="loading"
      class="pt-0"
    >
      <VSkeletonLoader type="table-heading, table-tbody" />
    </VCardText>

    <!-- Data Table -->
    <VTable
      v-else
      class="retail-table"
    >
      <thead>
        <tr>
          <th
            v-for="header in headers"
            :key="header.key"
            :class="header.align === 'end' ? 'text-end' : header.align === 'center' ? 'text-center' : ''"
            :style="header.width ? `width: ${header.width}` : ''"
          >
            {{ header.title }}
          </th>
        </tr>
      </thead>
      <tbody>
        <slot name="body" />

        <!-- Empty State -->
        <tr v-if="!loading && !$slots.body">
          <td
            :colspan="headers.length"
            class="text-center"
          >
            <div class="retail-empty-state">
              <VIcon
                :icon="emptyIcon || 'ri-inbox-2-line'"
                size="48"
                class="retail-empty-state__icon"
              />
              <div class="retail-empty-state__title">
                {{ emptyText || 'Chưa có dữ liệu' }}
              </div>
            </div>
          </td>
        </tr>
      </tbody>
    </VTable>
  </div>
</template>
