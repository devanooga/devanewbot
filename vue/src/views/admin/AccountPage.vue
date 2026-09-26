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

            <v-card v-if="slackAvailable" border class="mt-4">
                <v-card-item>
                    <v-card-title class="text-body-1">Slack account</v-card-title>
                    <v-card-subtitle v-if="user?.slack">
                        Linked to {{ user.slack.name ?? user.slack.userId }}
                    </v-card-subtitle>
                    <v-card-subtitle v-else>Needed to ban people, and lets you sign in with Slack</v-card-subtitle>
                </v-card-item>
                <v-card-actions class="px-4 pb-4">
                    <v-spacer />
                    <v-btn v-if="user?.slack" variant="text" :loading="busy" @click="unlink">Unlink</v-btn>
                    <v-btn v-else color="primary" prepend-icon="mdi-slack" href="/api/v0/auth/slack/start?mode=link">
                        Link Slack account
                    </v-btn>
                </v-card-actions>
            </v-card>
        </v-col>
    </v-row>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { api, errorMessage } from "@/api/admin";
import { useAuth } from "@/composables/useAuth";
import { useNotice } from "@/composables/useNotice";

const { user, update } = useAuth();
const { notify } = useNotice();
const route = useRoute();
const router = useRouter();

const slackOutcomes: Record<string, [string, "success" | "error"]> = {
    linked: ["Slack account linked.", "success"],
    taken: ["That Slack account is already linked to another login.", "error"],
    rejected: ["Slack did not confirm an account in this workspace.", "error"],
    "not-admin": ["Only Slack workspace admins can link their account.", "error"],
    expired: ["The Slack sign-in expired. Try again.", "error"],
};

const slackAvailable = ref(false);

const currentPassword = ref("");
const newPassword = ref("");
const confirmPassword = ref("");
const busy = ref(false);

const mismatch = computed(
    () => confirmPassword.value.length > 0 && newPassword.value !== confirmPassword.value,
);

async function unlink() {
    busy.value = true;
    try {
        update(await api.unlinkSlack());
        notify("Slack account unlinked.");
    } catch (failure) {
        notify(errorMessage(failure, "Could not unlink Slack."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(async () => {
    const outcome = slackOutcomes[route.query.slack as string];
    if (outcome) {
        notify(...outcome);
        update(await api.me());
        await router.replace({ query: {} });
    }

    slackAvailable.value = (await api.providers()).slack || Boolean(user.value?.slack);
});

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
