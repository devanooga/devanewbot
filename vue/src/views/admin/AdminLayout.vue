<template>
    <v-app>
        <v-navigation-drawer v-model="drawer" color="surface">
            <v-list-item class="py-4">
                <template #prepend>
                    <v-icon icon="mdi-antenna" color="primary" size="28" />
                </template>
                <v-list-item-title class="text-h6">devanewbot</v-list-item-title>
                <v-list-item-subtitle>{{ callsignLabel }}</v-list-item-subtitle>
            </v-list-item>

            <v-divider />

            <v-list nav density="comfortable">
                <v-list-item
                    v-for="page in pages"
                    :key="page.to"
                    :to="page.to"
                    :prepend-icon="page.icon"
                    :title="page.title"
                >
                    <template v-if="page.badge" #append>
                        <v-chip size="x-small" color="warning" variant="flat">{{ page.badge }}</v-chip>
                    </template>
                </v-list-item>
            </v-list>

            <template #append>
                <v-divider />
                <v-list nav density="comfortable">
                    <v-list-item
                        href="/hangfire"
                        target="_blank"
                        prepend-icon="mdi-open-in-new"
                        title="Hangfire"
                    />
                </v-list>
            </template>
        </v-navigation-drawer>

        <v-app-bar flat border="b" color="surface">
            <v-app-bar-nav-icon @click="drawer = !drawer" />
            <v-app-bar-title>{{ title }}</v-app-bar-title>

            <v-spacer />

            <v-chip
                v-if="refreshedAt"
                size="small"
                variant="tonal"
                :color="failure ? 'error' : 'success'"
                class="mr-2 d-none d-sm-flex"
            >
                <v-icon start :icon="failure ? 'mdi-alert-circle' : 'mdi-circle'" size="10" />
                {{ failure || `updated ${timeOnly(refreshedAt.toISOString())}` }}
            </v-chip>

            <v-btn icon="mdi-refresh" variant="text" :loading="refreshing" @click="manualRefresh" />
            <v-btn
                :icon="isDark ? 'mdi-weather-sunny' : 'mdi-weather-night'"
                variant="text"
                @click="toggleTheme"
            />

            <v-menu>
                <template #activator="{ props }">
                    <v-btn icon="mdi-account-circle" variant="text" v-bind="props" />
                </template>
                <v-list density="compact">
                    <v-list-item :subtitle="user?.email" title="Signed in as" />
                    <v-divider />
                    <v-list-item to="/admin/account" prepend-icon="mdi-key" title="Change password" />
                    <v-list-item prepend-icon="mdi-logout" title="Log out" @click="signOut" />
                </v-list>
            </v-menu>
        </v-app-bar>

        <v-main>
            <v-container fluid class="pa-4 pa-md-6">
                <router-view />
            </v-container>
        </v-main>

        <v-snackbar v-model="showNotice" :color="noticeColor" timeout="4000">
            {{ noticeText }}
        </v-snackbar>
    </v-app>
</template>

<script setup lang="ts">
import "@/plugins/vuetify-styles";
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useTheme } from "vuetify";
import { useAuth } from "@/composables/useAuth";
import { useAdminData } from "@/composables/useAdminData";
import { useNotice } from "@/composables/useNotice";
import { timeOnly } from "@/composables/useFormat";

const route = useRoute();
const router = useRouter();
const theme = useTheme();
const { user, logout } = useAuth();
const { status, refreshedAt, failure, refresh, startAutoRefresh } = useAdminData();
const { showNotice, noticeText, noticeColor } = useNotice();

const drawer = ref(true);
const refreshing = ref(false);

const isDark = computed(() => theme.global.current.value.dark);
const title = computed(() => (route.meta.title as string) ?? "Admin");
const callsignLabel = computed(() => {
    const feeds = status.value?.feeds.filter((feed) => feed.connected).length ?? 0;
    const total = status.value?.feeds.length ?? 0;
    return total === 0 ? "no feeds yet" : `${feeds}/${total} feeds up`;
});

const pages = computed(() => [
    { to: "/admin/dashboard", icon: "mdi-view-dashboard", title: "Dashboard", badge: 0 },
    { to: "/admin/sessions", icon: "mdi-radio-tower", title: "Sessions", badge: liveSessions.value },
    { to: "/admin/watch-list", icon: "mdi-account-search", title: "Watch list", badge: 0 },
    { to: "/admin/invites", icon: "mdi-email-fast", title: "Invites", badge: 0 },
    { to: "/admin/users", icon: "mdi-shield-account", title: "Users", badge: 0 },
]);

const liveSessions = computed(
    () => status.value?.sessions.filter((session) => session.closedAt === null).length ?? 0,
);

async function manualRefresh() {
    refreshing.value = true;
    await refresh();
    refreshing.value = false;
}

function toggleTheme() {
    const next = isDark.value ? "devanoogaLight" : "devanoogaDark";
    theme.global.name.value = next;
    try {
        window.localStorage.setItem("devanewbot-theme", next);
    } catch {
        // Private browsing blocks storage; the theme just will not stick.
    }
}

async function signOut() {
    await logout();
    router.push("/admin/login");
}

onMounted(async () => {
    try {
        const stored = window.localStorage.getItem("devanewbot-theme");
        if (stored) {
            theme.global.name.value = stored;
        }
    } catch {
        // Ignore unavailable storage.
    }

    await refresh();
    startAutoRefresh();
});
</script>
