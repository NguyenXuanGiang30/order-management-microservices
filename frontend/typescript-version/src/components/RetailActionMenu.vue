<script setup lang="ts">
export interface ActionMenuItem {
  label: string
  icon: string
  color?: string
  handler: () => void
  show?: boolean
}

defineProps<{
  items: ActionMenuItem[]
}>()
</script>

<template>
  <VMenu location="bottom end">
    <template #activator="{ props: menuProps }">
      <VBtn
        v-bind="menuProps"
        icon="ri-more-2-fill"
        variant="text"
        size="small"
        color="default"
      />
    </template>
    <VList
      density="compact"
      class="retail-action-menu"
    >
      <template
        v-for="item in items"
        :key="item.label"
      >
        <VListItem
          v-if="item.show !== false"
          :prepend-icon="item.icon"
          @click="item.handler"
        >
          <VListItemTitle :class="item.color ? `text-${item.color}` : ''">
            {{ item.label }}
          </VListItemTitle>
        </VListItem>
      </template>
    </VList>
  </VMenu>
</template>
