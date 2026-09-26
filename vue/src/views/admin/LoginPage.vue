<template>
    <v-app>
        <v-main class="login-background">
            <v-container class="fill-height" fluid>
                <v-row justify="center" align="center">
                    <v-col cols="12" sm="8" md="5" lg="4">
                        <v-card class="pa-2" elevation="8">
                            <v-card-item>
                                <img src="/img/devanooga-logo.svg" alt="Devanooga" class="wordmark mb-3" />
                                <div class="text-caption text-medium-emphasis">devanewbot admin sign in</div>
                            </v-card-item>

                            <v-card-text>
                                <v-form @submit.prevent="submit">
                                    <v-text-field
                                        v-model="email"
                                        label="Email"
                                        type="email"
                                        autocomplete="username"
                                        prepend-inner-icon="mdi-email"
                                        class="mb-3"
                                        required
                                    />
                                    <v-text-field
                                        v-model="password"
                                        label="Password"
                                        :type="reveal ? 'text' : 'password'"
                                        autocomplete="current-password"
                                        prepend-inner-icon="mdi-lock"
                                        :append-inner-icon="reveal ? 'mdi-eye-off' : 'mdi-eye'"
                                        required
                                        @click:append-inner="reveal = !reveal"
                                    />

                                    <v-alert
                                        v-if="error"
                                        type="error"
                                        variant="tonal"
                                        density="compact"
                                        class="mt-4"
                                    >
                                        {{ error }}
                                    </v-alert>

                                    <v-btn
                                        type="submit"
                                        color="primary"
                                        size="large"
                                        block
                                        class="mt-5"
                                        :loading="busy"
                                    >
                                        Log in
                                    </v-btn>
                                </v-form>

                                <template v-if="slackAvailable">
                                    <div class="text-caption text-medium-emphasis text-center my-3">or</div>
                                    <v-btn
                                        block
                                        size="large"
                                        variant="outlined"
                                        prepend-icon="mdi-slack"
                                        href="/api/v0/auth/slack/start"
                                    >
                                        Sign in with Slack
                                    </v-btn>
                                </template>
                            </v-card-text>
                        </v-card>
                    </v-col>
                </v-row>
            </v-container>
        </v-main>
    </v-app>
</template>

<script setup lang="ts">
import "@/plugins/vuetify-styles";
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuth } from "@/composables/useAuth";
import { api, errorMessage, isUnauthorized } from "@/api/admin";

const route = useRoute();
const router = useRouter();
const { login } = useAuth();

const email = ref("");
const password = ref("");
const reveal = ref(false);
const error = ref("");
const busy = ref(false);
const slackAvailable = ref(false);

const slackErrors: Record<string, string> = {
    unlinked: "No login matches that Slack account's email. Sign in with your password and link Slack on the Account page.",
    rejected: "Slack did not confirm an account in this workspace.",
    "not-admin": "Only Slack workspace admins can sign in with Slack.",
    expired: "The Slack sign-in expired. Try again.",
};

onMounted(async () => {
    error.value = slackErrors[route.query.slack as string] ?? "";
    try {
        slackAvailable.value = (await api.providers()).slack;
    } catch {
        slackAvailable.value = false;
    }
});

async function submit() {
    busy.value = true;
    error.value = "";
    try {
        await login(email.value, password.value);
        router.push((route.query.next as string) ?? "/admin/dashboard");
    } catch (failure) {
        error.value = isUnauthorized(failure)
            ? "Wrong email or password."
            : errorMessage(failure, "Login failed.");
    } finally {
        busy.value = false;
    }
}
</script>

<style scoped>
.wordmark {
    display: block;
    width: 160px;
}

.login-background {
    background: radial-gradient(circle at 20% 20%, rgba(224, 100, 79, 0.2), transparent 55%);
}
</style>
