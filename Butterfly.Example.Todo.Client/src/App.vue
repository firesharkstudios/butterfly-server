<template>
  <v-app>
    <v-content>
      <v-toolbar>
        <v-toolbar-title>Butterfly Server .NET Todo Example</v-toolbar-title>
        <v-spacer />
        <v-toolbar-items class="py-3">
          Connection: {{ $root.status }}
        </v-toolbar-items>
      </v-toolbar>
      <router-view/>
    </v-content>
    <v-footer :fixed="true" app class="text-xs-center">
      <span>&copy; Fireshark Studios, LLC 2018</span>
    </v-footer>
  </v-app>
</template>

<script>
export default {
}
</script>
