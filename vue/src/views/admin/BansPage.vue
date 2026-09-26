<template>
    <v-card border>
        <v-card-title class="d-flex align-center flex-wrap ga-3 text-body-1">
            Channel bans
            <v-spacer />
            <v-btn-toggle v-model="showAll" density="comfortable" variant="outlined" divided mandatory>
                <v-btn :value="false">Active</v-btn>
                <v-btn :value="true">All</v-btn>
            </v-btn-toggle>
            <v-btn color="primary" prepend-icon="mdi-gavel" @click="openCreate">Ban someone</v-btn>
        </v-card-title>

        <v-data-table
            :headers="headers"
            :items="visibleBans"
            :loading="loading"
            item-value="id"
            :items-per-page="25"
        >
            <template #[`item.userId`]="{ item }">
                <div class="font-weight-medium">{{ personName(item.userId) }}</div>
                <div class="text-caption text-medium-emphasis">{{ personHandle(item.userId) }}</div>
            </template>
            <template #[`item.channelId`]="{ item }">{{ channelName(item.channelId) }}</template>
            <template #[`item.reason`]="{ item }">
                <div class="reason">{{ item.reason }}</div>
            </template>
            <template #[`item.bannedAt`]="{ item }">
                <div>{{ personName(item.bannedBy) }}</div>
                <div class="text-caption text-medium-emphasis">{{ when(item.bannedAt) }}</div>
            </template>
            <template #[`item.expiresAt`]="{ item }">
                <span v-if="item.expiresAt">{{ day(item.expiresAt) }}</span>
                <span v-else class="text-medium-emphasis">never</span>
            </template>
            <template #[`item.active`]="{ item }">
                <v-chip size="small" variant="tonal" :color="item.active ? 'error' : 'secondary'">
                    {{ item.active ? "Active" : item.liftedEarly ? "Lifted" : "Expired" }}
                </v-chip>
            </template>
            <template #[`item.actions`]="{ item }">
                <v-btn v-if="item.active" size="small" variant="tonal" @click="confirmLift(item)">Lift</v-btn>
            </template>
            <template #no-data>
                <div class="py-8 text-center text-medium-emphasis">
                    {{ showAll ? "Nobody has been banned." : "No active bans." }}
                </div>
            </template>
        </v-data-table>
    </v-card>

    <v-dialog v-model="creating" max-width="520">
        <v-card>
            <v-card-title>Ban someone from a channel</v-card-title>
            <v-card-text class="d-flex flex-column ga-4">
                <v-alert v-if="!user?.slack" type="warning" variant="tonal" density="compact">
                    Banning needs a linked Slack account belonging to a workspace admin.
                    <router-link to="/admin/account">Link it on the Account page</router-link> first.
                </v-alert>
                <v-autocomplete
                    v-model="form.userId"
                    :items="people"
                    item-title="name"
                    item-value="id"
                    label="Person"
                    :loading="directoryLoading"
                    :custom-filter="matchPerson"
                >
                    <template #item="{ props, item }">
                        <v-list-item v-bind="props" :subtitle="`@${item.raw.handle}`" />
                    </template>
                </v-autocomplete>
                <v-autocomplete
                    v-model="form.channelId"
                    :items="channelChoices"
                    item-title="title"
                    item-value="id"
                    label="Channel"
                    :loading="directoryLoading"
                />
                <v-textarea v-model="form.reason" label="Reason" rows="2" auto-grow />
                <v-text-field
                    v-model="form.expiresOn"
                    label="Ends on"
                    type="date"
                    :min="tomorrow"
                    hint="Leave empty for a permanent ban"
                    persistent-hint
                    clearable
                />
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="creating = false">Cancel</v-btn>
                <v-btn color="error" :loading="busy" :disabled="!canBan" @click="create">Ban</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>

    <v-dialog v-model="lifting" max-width="420">
        <v-card>
            <v-card-title>Lift this ban?</v-card-title>
            <v-card-text class="text-medium-emphasis">
                {{ target ? personName(target.userId) : "" }} can rejoin
                {{ target ? channelName(target.channelId) : "" }} and gets a DM saying so.
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="lifting = false">Cancel</v-btn>
                <v-btn color="primary" :loading="busy" @click="lift">Lift ban</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { api, ChannelBan, errorMessage, SlackChannel, SlackPerson } from "@/api/admin";
import { useAuth } from "@/composables/useAuth";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";

const { user } = useAuth();
const { notify } = useNotice();

const headers = [
    { title: "Person", key: "userId", minWidth: "140px" },
    { title: "Channel", key: "channelId" },
    { title: "Reason", key: "reason", sortable: false, minWidth: "180px" },
    { title: "Banned by", key: "bannedAt", minWidth: "140px" },
    { title: "Ends", key: "expiresAt" },
    { title: "Status", key: "active" },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const bans = ref<ChannelBan[]>([]);
const people = ref<SlackPerson[]>([]);
const channels = ref<SlackChannel[]>([]);
const showAll = ref(false);
const loading = ref(false);
const directoryLoading = ref(false);
const busy = ref(false);
const creating = ref(false);
const lifting = ref(false);
const target = ref<ChannelBan | null>(null);
const form = reactive({
    userId: null as string | null,
    channelId: null as string | null,
    reason: "",
    expiresOn: "",
});

const peopleById = computed(() => new Map(people.value.map((person) => [person.id, person])));
const channelsById = computed(() => new Map(channels.value.map((channel) => [channel.id, channel])));
const channelChoices = computed(() => channels.value.map((channel) => ({ id: channel.id, title: `#${channel.name}` })));
const visibleBans = computed(() => (showAll.value ? bans.value : bans.value.filter((ban) => ban.active)));
const canBan = computed(() => Boolean(user.value?.slack && form.userId && form.channelId && form.reason.trim()));
const tomorrow = computed(() => {
    const date = new Date();
    date.setDate(date.getDate() + 1);
    return date.toISOString().slice(0, 10);
});

function personName(id: string): string {
    return peopleById.value.get(id)?.name ?? id;
}

function personHandle(id: string): string {
    const person = peopleById.value.get(id);
    return person ? `@${person.handle}` : "";
}

function channelName(id: string): string {
    const channel = channelsById.value.get(id);
    return channel ? `#${channel.name}` : id;
}

function day(value: string): string {
    return new Date(value).toLocaleDateString();
}

function matchPerson(_value: string, query: string, item?: { raw: SlackPerson }): boolean {
    const needle = query.toLowerCase();
    return Boolean(
        item && (item.raw.name.toLowerCase().includes(needle) || item.raw.handle.toLowerCase().includes(needle)),
    );
}

async function load() {
    loading.value = true;
    try {
        bans.value = await api.bans();
    } catch (failure) {
        notify(errorMessage(failure, "Could not load bans."), "error");
    } finally {
        loading.value = false;
    }
}

async function loadDirectory() {
    directoryLoading.value = true;
    try {
        const directory = await api.slackDirectory();
        people.value = directory.people;
        channels.value = directory.channels;
    } catch (failure) {
        notify(errorMessage(failure, "Could not load Slack people and channels."), "error");
    } finally {
        directoryLoading.value = false;
    }
}

function openCreate() {
    Object.assign(form, { userId: null, channelId: null, reason: "", expiresOn: "" });
    creating.value = true;
}

async function create() {
    if (!form.userId || !form.channelId) {
        return;
    }

    busy.value = true;
    try {
        bans.value = await api.createBan({
            userId: form.userId,
            channelId: form.channelId,
            reason: form.reason.trim(),
            expiresOn: form.expiresOn || null,
        });
        notify(`Banned ${personName(form.userId)} from ${channelName(form.channelId)}.`);
        creating.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not ban them."), "error");
    } finally {
        busy.value = false;
    }
}

function confirmLift(ban: ChannelBan) {
    target.value = ban;
    lifting.value = true;
}

async function lift() {
    if (!target.value) {
        return;
    }

    busy.value = true;
    try {
        bans.value = await api.liftBan(target.value.id);
        notify(`Lifted the ban on ${personName(target.value.userId)}.`);
        lifting.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not lift the ban."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(() => Promise.all([load(), loadDirectory()]));
</script>

<style scoped>
.reason {
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}
</style>
