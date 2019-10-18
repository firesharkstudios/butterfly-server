<template>
  <v-list-item>
    <v-list-item-content>
      <v-list-item-title>{{ item.name }}</v-list-item-title>
    </v-list-item-content>

    <v-list-item-action>
      <v-btn icon @click="$emit('remove', item.id)"><v-icon>mdi-delete</v-icon></v-btn>
    </v-list-item-action>
  </v-list-item>
</template>

<script>
export default {
  props: {
    item: null,
  },
  data() {
    return {
    }
  },
}
</script>

<style scoped>
</style>
