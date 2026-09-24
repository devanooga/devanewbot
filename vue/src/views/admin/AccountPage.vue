<template>
    <v-row justify="center">
        <v-col cols="12" md="6" lg="5">
            <v-card border>
                <v-card-item>
                    <v-card-title class="text-body-1">Change your password</v-card-title>
                    <v-card-subtitle>{{ user?.email }}</v-card-subtitle>
                </v-card-item>

                <v-card-text class="d-flex flex-column ga-4">
                    <v-text-field
                        v-model="currentPassword"
                        label="Current password"
                        type="password"
                        autocomplete="current-password"
                    />
                    <v-text-field
                        v-model="newPassword"
                        label="New password"
                        type="password"
                        autocomplete="new-password"
                    />
                    <v-text-field
                        v-model="confirmPassword"
                        label="Confirm new password"
                        type="password"
                        autocomplete="new-password"
                        :error-messages="mismatch ? 'The new passwords do not match.' : undefined"
                    />
                </v-card-text>

                <v-card-actions class="px-4 pb-4">
                    <v-spacer />
                    <v-btn color="primary" :loading="busy" :disabled="mismatch" @click="submit">
                        Change password
                    </v-btn>
                </v-card-actions>
            </v-card>
        </v-col>
    </v-row>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { api, errorMessage } from "@/api/admin";
import { useAuth } from "@/composables/useAuth";
import { useNotice } from "@/composables/useNotice";

const { user } = useAuth();
const { notify } = useNotice();

const currentPassword = ref("");
const newPassword = ref("");
const confirmPassword = ref("");
const busy = ref(false);

const mismatch = computed(
    () => confirmPassword.value.length > 0 && newPassword.value !== confirmPassword.value,
);

async function submit() {
    busy.value = true;
    try {
        await api.changePassword(currentPassword.value, newPassword.value);
        notify("Password changed.");
        currentPassword.value = "";
        newPassword.value = "";
        confirmPassword.value = "";
    } catch (failure) {
        notify(errorMessage(failure, "Could not change the password."), "error");
    } finally {
        busy.value = false;
    }
}
</script>
