<template>
    <v-app>
        <v-main class="login-background">
            <v-container class="fill-height" fluid>
                <v-row justify="center" align="center">
                    <v-col cols="12" sm="8" md="5" lg="4">
                        <v-card class="pa-2" elevation="8">
                            <v-card-item>
                                <div class="d-flex align-center ga-3 mb-2">
                                    <v-icon icon="mdi-antenna" color="primary" size="32" />
                                    <div>
                                        <div class="text-h6">devanewbot</div>
                                        <div class="text-caption text-medium-emphasis">Admin sign in</div>
                                    </div>
                                </div>
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
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuth } from "@/composables/useAuth";
import { errorMessage, isUnauthorized } from "@/api/admin";

const route = useRoute();
const router = useRouter();
const { login } = useAuth();

const email = ref("");
const password = ref("");
const reveal = ref(false);
const error = ref("");
const busy = ref(false);

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
.login-background {
    background: radial-gradient(circle at 20% 20%, rgba(79, 140, 255, 0.18), transparent 55%);
}
</style>
