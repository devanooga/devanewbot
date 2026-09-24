<template>
    <v-card border class="mb-4">
        <v-card-title class="text-body-1">Invite someone</v-card-title>
        <v-card-text>
            <v-form class="d-flex ga-3 flex-wrap" @submit.prevent="send">
                <v-text-field
                    v-model="inviteEmail"
                    label="Email address"
                    type="email"
                    style="min-width: 260px"
                    class="flex-grow-1"
                />
                <v-btn color="primary" size="large" :loading="busy" @click="send">Send invite</v-btn>
            </v-form>
        </v-card-text>
    </v-card>

    <v-card border>
        <v-card-title class="d-flex align-center flex-wrap ga-3 text-body-1">
            Invite log
            <v-spacer />
            <v-select
                v-model="filter"
                :items="statusOptions"
                label="Status"
                style="max-width: 200px"
                @update:model-value="load"
            />
            <v-text-field
                v-model="search"
                label="Email or IP"
                prepend-inner-icon="mdi-magnify"
                style="max-width: 240px"
                @keyup.enter="load"
            />
            <v-btn variant="tonal" :loading="loading" @click="load">Search</v-btn>
        </v-card-title>

        <v-data-table
            :headers="headers"
            :items="invites"
            :loading="loading"
            item-value="id"
            :items-per-page="25"
        >
            <template #[`item.createdAt`]="{ item }">{{ when(item.createdAt) }}</template>
            <template #[`item.location`]="{ item }">
                <div>{{ [item.city, item.region, item.country].filter(Boolean).join(", ") }}</div>
                <div class="text-caption text-medium-emphasis">
                    {{ item.isp }}
                    <v-chip v-if="item.proxy" size="x-small" color="warning" variant="tonal" class="ml-1">
                        proxy
                    </v-chip>
                    <v-chip v-if="item.hosting" size="x-small" color="warning" variant="tonal" class="ml-1">
                        datacenter
                    </v-chip>
                    <v-chip v-if="item.mobile" size="x-small" color="info" variant="tonal" class="ml-1">
                        mobile
                    </v-chip>
                </div>
            </template>
            <template #[`item.flag`]="{ item }">
                <v-chip v-if="item.flag" size="small" variant="tonal" :color="flagColor(item.flag)">
                    {{ item.flagMessage ?? item.flag }}
                </v-chip>
            </template>
            <template #[`item.status`]="{ item }">
                <v-chip size="small" variant="tonal" :color="statusColor(item.status)">
                    {{ item.status }}
                </v-chip>
                <div v-if="item.error" class="text-caption text-error">{{ item.error }}</div>
            </template>
            <template #[`item.decidedBy`]="{ item }">
                <div v-if="item.decidedBy">{{ item.decidedBy }}</div>
                <div class="text-caption text-medium-emphasis">
                    {{ item.decisionSource }}
                    <span v-if="item.decidedAt"> · {{ when(item.decidedAt) }}</span>
                </div>
            </template>
            <template #[`item.actions`]="{ item }">
                <div class="d-flex ga-1 justify-end">
                    <template v-if="item.status === 'Pending'">
                        <v-btn size="small" color="success" variant="tonal" :loading="busy" @click="decide(item, true)">
                            Approve
                        </v-btn>
                        <v-btn size="small" color="error" variant="tonal" :loading="busy" @click="decide(item, false)">
                            Decline
                        </v-btn>
                    </template>
                    <v-btn size="small" variant="text" icon="mdi-information-outline" @click="open(item.id)" />
                </div>
            </template>
            <template #no-data>
                <div class="py-8 text-center text-medium-emphasis">No invites match.</div>
            </template>
        </v-data-table>
    </v-card>

    <v-dialog v-model="showDetail" max-width="720">
        <v-card :loading="detailLoading">
            <v-card-title class="text-body-1">{{ detail?.email ?? "Invite" }}</v-card-title>
            <v-card-text v-if="detail">
                <v-table density="compact">
                    <tbody>
                        <tr v-for="row in detailRows" :key="row.label">
                            <td class="text-medium-emphasis" style="width: 180px">{{ row.label }}</td>
                            <td>{{ row.value }}</td>
                        </tr>
                    </tbody>
                </v-table>
                <div v-if="detail.locationJson" class="mt-4">
                    <div class="text-caption text-medium-emphasis mb-1">GeoIP response</div>
                    <pre class="geoip">{{ prettyLocation }}</pre>
                </div>
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn @click="close">Close</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { api, errorMessage, Invite, InviteDetail, InviteStatuses } from "@/api/admin";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";

const { notify } = useNotice();
const route = useRoute();
const router = useRouter();

const headers = [
    { title: "Requested", key: "createdAt" },
    { title: "Email", key: "email" },
    { title: "IP", key: "ip" },
    { title: "Location", key: "location", sortable: false },
    { title: "Flag", key: "flag", sortable: false },
    { title: "Source", key: "source" },
    { title: "Status", key: "status" },
    { title: "Decided by", key: "decidedBy" },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const statusOptions = [{ title: "All", value: "" }, ...InviteStatuses.map((s) => ({ title: s, value: s }))];

const invites = ref<Invite[]>([]);
const filter = ref("");
const search = ref("");
const inviteEmail = ref("");
const loading = ref(false);
const busy = ref(false);
const showDetail = ref(false);
const detailLoading = ref(false);
const detail = ref<InviteDetail | null>(null);

const detailRows = computed(() => {
    const invite = detail.value;
    if (!invite) {
        return [];
    }

    const signals = [
        invite.proxy ? "proxy" : null,
        invite.hosting ? "datacenter" : null,
        invite.mobile ? "mobile" : null,
    ].filter(Boolean);

    return [
        { label: "Email", value: invite.email },
        { label: "IP", value: invite.ip },
        { label: "Location", value: [invite.city, invite.region, invite.country].filter(Boolean).join(", ") },
        { label: "ISP", value: invite.isp ?? "" },
        { label: "Signals", value: signals.join(", ") },
        { label: "Flag", value: [invite.flag, invite.flagMessage].filter(Boolean).join(" · ") },
        { label: "Source", value: invite.source },
        { label: "Status", value: invite.status },
        { label: "Requested", value: when(invite.createdAt) },
        { label: "Decided", value: invite.decidedAt ? `${when(invite.decidedAt)} by ${invite.decidedBy}` : "" },
        { label: "Decided via", value: invite.decisionSource ?? "" },
        { label: "Error", value: invite.error ?? "" },
    ].filter((row) => row.value);
});

const prettyLocation = computed(() => {
    if (!detail.value?.locationJson) {
        return "";
    }
    try {
        return JSON.stringify(JSON.parse(detail.value.locationJson), null, 2);
    } catch {
        return detail.value.locationJson;
    }
});

function flagColor(flag: string): string {
    return flag === "Green" ? "success" : flag === "Red" ? "error" : "warning";
}

function statusColor(status: string): string {
    if (status === "Approved") {
        return "success";
    }
    if (status === "Declined" || status === "Failed") {
        return "error";
    }
    return status === "Pending" ? "warning" : "secondary";
}

async function load() {
    loading.value = true;
    try {
        invites.value = await api.invites(filter.value, search.value);
    } catch (failure) {
        notify(errorMessage(failure, "Could not load invites."), "error");
    } finally {
        loading.value = false;
    }
}

async function send() {
    busy.value = true;
    try {
        const result = await api.createInvite(inviteEmail.value);
        notify(
            result.result === "AlreadyInvited"
                ? `${inviteEmail.value} was already invited.`
                : `Invited ${inviteEmail.value}.`,
        );
        inviteEmail.value = "";
        await load();
    } catch (failure) {
        notify(errorMessage(failure, "Could not send the invite."), "error");
    } finally {
        busy.value = false;
    }
}

async function open(id: string) {
    if (route.query.id !== id) {
        await router.replace({ query: { ...route.query, id } });
    }

    showDetail.value = true;
    detailLoading.value = true;
    try {
        detail.value = await api.invite(id);
    } catch (failure) {
        notify(errorMessage(failure, "Could not load that invite."), "error");
        close();
    } finally {
        detailLoading.value = false;
    }
}

function close() {
    showDetail.value = false;
}

watch(showDetail, async (isOpen) => {
    if (!isOpen && route.query.id) {
        const query = { ...route.query };
        delete query.id;
        await router.replace({ query });
    }
});

async function decide(invite: Invite, approve: boolean) {
    busy.value = true;
    try {
        await api.decideInvite(invite.id, approve);
        notify(`${approve ? "Approved" : "Declined"} ${invite.email}.`);
        await load();
    } catch (failure) {
        notify(errorMessage(failure, "Could not update the invite."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(async () => {
    await load();
    if (typeof route.query.id === "string") {
        await open(route.query.id);
    }
});
</script>

<style scoped>
.geoip {
    font-size: 0.8rem;
    white-space: pre-wrap;
    word-break: break-all;
}
</style>
