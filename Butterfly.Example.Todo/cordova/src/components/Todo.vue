<template>
  <v-list-tile>
    <v-list-tile-content>
      <v-list-tile-title>{{ item.name }}</v-list-tile-title>
    </v-list-tile-content>

    <v-list-tile-action>
      <v-btn icon @click="$emit('remove', item.id)"><v-icon>delete</v-icon></v-btn>
    </v-list-tile-action>
  </v-list-tile>
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
