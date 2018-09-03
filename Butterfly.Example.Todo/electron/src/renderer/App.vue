<template>
    <v-app>
        <v-content>
            <v-toolbar>
                <v-toolbar-title>Butterfly Server .NET Todo Example</v-toolbar-title>
                <v-spacer />
                <v-toolbar-items>
                </v-toolbar-items>
            </v-toolbar>
            <router-view v-if="$root.channelClientState=='Connected'" />
            <div class="px-5 py-5 text-xs-center" v-else>
                <v-progress-circular indeterminate color="primary" /> <span class="pl-2 title">{{ $root.channelClientState }}...</span>
            </div>
        </v-content>
    </v-app>
</template>

<script>
</script>

<style>
    @import url('https://fonts.googleapis.com/css?family=Roboto:300,400,500,700|Material+Icons');
    /* Global CSS */
</style>
