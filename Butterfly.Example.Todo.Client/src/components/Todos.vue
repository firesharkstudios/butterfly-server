<template>
  <div>
    <div class="px-3 py-3 text-xs-center" v-if="items.length==0">
      No todos yet
    </div>
    <v-list v-else>
      <Todo v-for="item in items" :key="item.id" :item="item" @remove="remove" />
    </v-list>

    <div class="px-3 py-3 text-xs-center">
      <v-btn color="primary" @click="add">Add Todo</v-btn>
    </div>
  </div>
</template>

<script>
  import Todo from '@/components/Todo'

  export default {
    components: {
      Todo,
    },
    data () {
      return {
        items: [],
      }
    },
    methods: {
      add() {
        this.$root.callApi('/api/todo/insert', {
          name: 'A new todo item',
        });
      },
      remove(id) {
        this.$root.callApi('/api/todo/delete', id);
      },
    },
    mounted() {
      let self = this;
      self.$root.subscribe({
        arrayMapping: {
          todo: self.items,
        },
        key: 'todos',
        vars: {
          clientName: 'WebClient'
        }
      });
    }
  }
</script>

<style scoped>
</style>
