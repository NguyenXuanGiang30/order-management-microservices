<script lang="ts" setup>
import DefaultLayoutWithVerticalNav from './components/DefaultLayoutWithVerticalNav.vue'
</script>

<template>
  <DefaultLayoutWithVerticalNav>
    <RouterView v-slot="{ Component, route }">
      <Transition
        name="page-fade"
        mode="out-in"
      >
        <div :key="route.fullPath">
          <component :is="Component" />
        </div>
      </Transition>
    </RouterView>
  </DefaultLayoutWithVerticalNav>
</template>

<style lang="scss">
// As we are using `layouts` plugin we need its styles to be imported
@use "@layouts/styles/default-layout";

.page-fade-enter-active,
.page-fade-leave-active {
  transition: opacity 180ms ease, transform 180ms ease;
}

.page-fade-enter-from,
.page-fade-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
