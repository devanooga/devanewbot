<template>
    <v-alert v-if="!user?.slack" type="warning" variant="tonal" density="compact" class="mb-4">
        Removing messages needs a linked Slack account belonging to a workspace admin.
        <router-link to="/admin/account">Link it on the Account page</router-link> first.
    </v-alert>

    <v-card border class="mb-4">
        <v-card-title class="d-flex align-center flex-wrap ga-3 text-body-1">
            <v-autocomplete
                :model-value="userId"
                :items="people"
                item-title="name"
                item-value="id"
                label="Person"
                :loading="directoryLoading"
                :custom-filter="matchPerson"
                style="min-width: 240px; max-width: 360px"
                @update:model-value="pickPerson"
            >
                <template #item="{ props, item }">
                    <v-list-item v-bind="props" :subtitle="`@${item.raw.handle}`" />
                </template>
            </v-autocomplete>
            <v-spacer />
            <v-btn-toggle
                v-model="hours"
                density="comfortable"
                variant="outlined"
                divided
                mandatory
                @update:model-value="load"
            >
                <v-btn :value="24">24 hours</v-btn>
                <v-btn :value="24 * 7">7 days</v-btn>
                <v-btn :value="24 * 30">30 days</v-btn>
            </v-btn-toggle>
        </v-card-title>

        <v-data-table
            v-model="selected"
            :headers="headers"
            :items="messages"
            :loading="loading"
            item-value="key"
            show-select
            :items-per-page="50"
        >
            <template #[`item.postedAt`]="{ item }">
                <div class="text-no-wrap">{{ when(item.postedAt) }}</div>
                <v-chip v-if="item.key === anchorKey" size="x-small" color="primary" variant="tonal">from Slack</v-chip>
            </template>
            <template #[`item.channel`]="{ item }">
                <a v-if="item.permalink" :href="item.permalink" target="_blank" rel="noopener">{{ item.channel }}</a>
                <span v-else>{{ item.channel }}</span>
            </template>
            <template #[`item.text`]="{ item }">
                <div class="message">{{ item.text || "(no text)" }}</div>
            </template>
            <template #no-data>
                <div class="py-8 text-center text-medium-emphasis">
                    {{ userId ? "No messages in this window." : "Pick a person." }}
                </div>
            </template>
        </v-data-table>
    </v-card>

    <v-card v-if="userId" border>
        <v-card-text class="d-flex flex-column ga-3">
            <v-textarea v-model="reason" label="Reason" hint="Shown on the public moderation log." persistent-hint rows="2" auto-grow />
            <v-checkbox
                v-model="deactivate"
                :disabled="authorIsAdmin"
                :label="`Also deactivate ${author || 'their account'}`"
                :hint="authorIsAdmin ? 'Admins cannot be deactivated from here.' : ''"
                persistent-hint
                density="compact"
            />
            <v-alert v-if="failures.length" type="error" variant="tonal" density="compact">
                <div v-for="failure in failures" :key="failure">{{ failure }}</div>
            </v-alert>
        </v-card-text>
        <v-card-actions>
            <v-spacer />
            <v-btn color="error" variant="flat" :disabled="!canSubmit" @click="confirming = true">{{ submitLabel }}</v-btn>
        </v-card-actions>
    </v-card>

    <v-dialog v-model="confirming" max-width="440">
        <v-card>
            <v-card-title>{{ submitLabel }}?</v-card-title>
            <v-card-text class="text-medium-emphasis">
                Deleted messages cannot be restored. Their text is kept in the moderation log for admins.
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="confirming = false">Cancel</v-btn>
                <v-btn color="error" :loading="busy" @click="submit">Remove</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { api, AuthoredMessage, errorMessage, SlackPerson } from "@/api/admin";
import { useAuth } from "@/composables/useAuth";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";

interface Row extends AuthoredMessage {
    key: string;
}

const route = useRoute();
const router = useRouter();
const { user } = useAuth();
const { notify } = useNotice();

const headers = [
    { title: "Posted", key: "postedAt", width: "180px" },
    { title: "Channel", key: "channel", width: "160px" },
    { title: "Message", key: "text", sortable: false },
];

const people = ref<SlackPerson[]>([]);
const messages = ref<Row[]>([]);
const selected = ref<string[]>([]);
const author = ref("");
const authorIsAdmin = ref(false);
const hours = ref(24);
const reason = ref("");
const deactivate = ref(false);
const failures = ref<string[]>([]);
const loading = ref(false);
const directoryLoading = ref(false);
const busy = ref(false);
const confirming = ref(false);

const userId = computed(() => (typeof route.query.user === "string" ? route.query.user : ""));
const anchorKey = computed(() =>
    typeof route.query.channel === "string" && typeof route.query.ts === "string"
        ? `${route.query.channel}:${route.query.ts}`
        : "",
);
const canSubmit = computed(
    () => Boolean(user.value?.slack && reason.value.trim() && (selected.value.length > 0 || deactivate.value)),
);
const submitLabel = computed(() => {
    const count = selected.value.length;
    const removal = count === 0 ? "" : `Remove ${count} message${count === 1 ? "" : "s"}`;
    if (!deactivate.value) {
        return removal || "Remove";
    }
    return removal ? `${removal} and deactivate` : "Deactivate";
});

function matchPerson(_value: string, query: string, item?: { raw: SlackPerson }): boolean {
    const needle = query.toLowerCase();
    return Boolean(
        item && (item.raw.name.toLowerCase().includes(needle) || item.raw.handle.toLowerCase().includes(needle)),
    );
}

async function pickPerson(id: string | null) {
    await router.replace({ query: id ? { user: id } : {} });
    await load();
}

async function load() {
    failures.value = [];
    if (!userId.value) {
        messages.value = [];
        return;
    }

    loading.value = true;
    try {
        const recent = await api.recentMessages(userId.value, hours.value);
        author.value = recent.author;
        authorIsAdmin.value = recent.authorIsAdmin;
        const rows: Row[] = recent.messages.map((message) => ({ ...message, key: `${message.channelId}:${message.ts}` }));

        // Slack's search index can lag a freshly posted message; keep the one the admin clicked on regardless.
        if (anchorKey.value && !rows.some((row) => row.key === anchorKey.value)) {
            const [channelId, ts] = anchorKey.value.split(":");
            rows.unshift({
                key: anchorKey.value,
                channelId,
                channel: "(not indexed yet)",
                ts,
                postedAt: new Date(Number(ts) * 1000).toISOString(),
                text: "",
                permalink: null,
            });
        }

        messages.value = rows;
        const keys = new Set(rows.map((row) => row.key));
        selected.value = selected.value.filter((key) => keys.has(key));
        if (anchorKey.value && !selected.value.includes(anchorKey.value)) {
            selected.value.push(anchorKey.value);
        }
    } catch (failure) {
        notify(errorMessage(failure, "Could not load their messages."), "error");
    } finally {
        loading.value = false;
    }
}

async function loadDirectory() {
    directoryLoading.value = true;
    try {
        people.value = (await api.slackDirectory()).people;
    } catch (failure) {
        notify(errorMessage(failure, "Could not load Slack people."), "error");
    } finally {
        directoryLoading.value = false;
    }
}

async function submit() {
    busy.value = true;
    const chosen = new Set(selected.value);
    try {
        const result = await api.removeMessages({
            userId: userId.value,
            messages: messages.value
                .filter((row) => chosen.has(row.key))
                .map((row) => ({ channelId: row.channelId, ts: row.ts, text: row.text })),
            reason: reason.value.trim(),
            deactivate: deactivate.value,
        });
        confirming.value = false;

        const done = [
            result.removed ? `Removed ${result.removed} message${result.removed === 1 ? "" : "s"}` : "",
            result.deactivated ? `deactivated ${author.value}` : "",
        ].filter(Boolean);
        if (result.failures.length === 0) {
            notify(`${done.join(" and ")}.`);
            router.push("/admin/moderation");
            return;
        }

        failures.value = result.failures;
        notify(done.length ? `${done.join(" and ")}, with some failures.` : "Nothing was removed.", "error");
        selected.value = [];
        await load();
    } catch (failure) {
        notify(errorMessage(failure, "Could not remove them."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(() => Promise.all([load(), loadDirectory()]));
</script>

<style scoped>
.message {
    white-space: pre-wrap;
    overflow-wrap: anywhere;
    padding: 6px 0;
}
</style>
