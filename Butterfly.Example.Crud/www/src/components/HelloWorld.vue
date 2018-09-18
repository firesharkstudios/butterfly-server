<template>
  <v-container fluid>

    <!-- Contact List -->
    <v-list v-if="contacts.length>0">
      <v-list-tile v-for="contact in contacts" :key="contact.id">
        <v-list-tile-content>
          <v-list-tile-title>
            {{ contact.first_name }} {{ contact.last_name }}
          </v-list-tile-title>
        </v-list-tile-content>
        <v-list-tile-action>
          <v-btn icon @click="showDialog(true, contact)">
            <v-icon>edit</v-icon>
          </v-btn>
        </v-list-tile-action>
        <v-list-tile-action>
          <v-btn icon @click="remove(contact.id)">
            <v-icon>delete</v-icon>
          </v-btn>
        </v-list-tile-action>
      </v-list-tile>
    </v-list>
    <v-flex class="px-5 py-5 text-xs-center" v-else>
      - No Contacts -
    </v-flex>

    <!-- Add Contact Button -->
    <v-flex class="text-xs-center py-3">
      <v-btn @click="showDialog(true)" color="primary">
        <v-icon>add</v-icon> Add Contact
      </v-btn>
    </v-flex>

    <!-- Contact Dialog -->
    <v-dialog v-model="dialogShow" persistent max-width="500px">
      <v-card>
        <v-card-title>
          <span class="headline" v-if="dialogId">Update Contact</span>
          <span class="headline" v-else>Add Contact</span>
        </v-card-title>
        <v-card-text>
          <v-container grid-list-md>
            <v-layout wrap>
              <v-flex xs12 sm6>
                <v-text-field label="First Name"
                              v-model="dialogFirstName" required />
              </v-flex>
              <v-flex xs12 sm6>
                <v-text-field label="Last Name"
                              v-model="dialogLastName" required>
                </v-text-field>
              </v-flex>
            </v-layout>
          </v-container>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="blue darken-1" flat
                 @click="showDialog(false)">Close</v-btn>
          <v-btn color="blue darken-1" flat
                 @click="save">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>
<script>export default {
    data () {
      return {
        contacts: [],
        dialogShow: false,
        dialogId: null,
        dialogFirstName: null,
        dialogLastName: null,
      }
    },
    methods: {
      showDialog(show, item) {
       this.dialogShow = show;
       this.dialogId = (item || {}).id;
       this.dialogFirstName = (item || {}).first_name;
       this.dialogLastName = (item || {}).last_name;
      },
      save() {
        let data = {
          first_name: this.dialogFirstName,
          last_name: this.dialogLastName,
        };

        let apiUrl;
        if (this.dialogId) {
          apiUrl = '/api/contact/update';
          data.id = this.dialogId;
        }
        else {
          apiUrl = '/api/contact/insert';
        }

        let self = this;
        this.$root.callApi(apiUrl, data)
          .then(res => self.dialogShow = false);
      },
      remove(id) {
        this.$root.callApi('/api/contact/delete', id);
      },
    },
    mounted() {
      this.$root.subscribe({
        channel: 'all-contacts',
        arrayMapping: {
          contact: this.contacts,
        },
      });
    },
    destroyed() {
      this.$root.unsubscribe('all-contacts');
    }
  }
</script>
