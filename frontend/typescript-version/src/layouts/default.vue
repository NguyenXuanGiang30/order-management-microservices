<script lang="ts" setup>
import DefaultLayoutWithVerticalNav from './components/DefaultLayoutWithVerticalNav.vue'
</script>

<template>
  <DefaultLayoutWithVerticalNav>
    <RouterView v-slot="{ Component, route }">
      <Transition
        name="page"
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
</style>
