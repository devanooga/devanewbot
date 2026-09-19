<template>
    <v-card border>
        <v-card-title class="d-flex align-center text-body-1">
            Recent sessions
            <v-spacer />
            <v-text-field
                v-model="search"
                density="compact"
                placeholder="Filter by callsign"
                prepend-inner-icon="mdi-magnify"
                style="max-width: 260px"
            />
        </v-card-title>

        <v-data-table
            :headers="headers"
            :items="sessions"
            :search="search"
            item-value="id"
            show-expand
            :expanded="expanded"
            :items-per-page="25"
            @update:expanded="onExpand"
        >
            <template #[`item.callsign`]="{ item }">
                <span class="font-weight-medium">{{ item.callsign }}</span>
            </template>
            <template #[`item.bandMode`]="{ item }">
                <v-chip size="small" variant="tonal">{{ item.band }}</v-chip>
                <span class="ml-2 text-medium-emphasis">{{ item.mode }}</span>
            </template>
            <template #[`item.closedAt`]="{ item }">
                <v-chip
                    size="small"
                    variant="tonal"
                    :color="item.closedAt ? 'secondary' : 'success'"
                >
                    {{ item.closedAt ? "closed" : "live" }}
                </v-chip>
            </template>
            <template #[`item.lastHeardAt`]="{ item }">{{ ago(item.lastHeardAt) }}</template>
            <template #[`item.furthestKm`]="{ item }">
                <span v-if="item.furthestKm" class="text-no-wrap">
                    {{ kilometres(item.furthestKm) }}
                    <span class="text-medium-emphasis">{{ item.furthestReporter }}</span>
                </span>
            </template>
            <template #[`item.bestSnr`]="{ item }">
                <span v-if="item.bestSnr != null">
                    {{ item.bestSnr }} dB
                    <span class="text-medium-emphasis">{{ item.bestSnrReporter }}</span>
                </span>
            </template>

            <template #[`item.actions`]="{ item }">
                <v-tooltip text="Post a fresh Slack message for this session" location="top">
                    <template #activator="{ props }">
                        <v-btn
                            v-bind="props"
                            icon="mdi-bullhorn"
                            size="small"
                            variant="text"
                            :loading="announcing === item.id"
                            @click.stop="reannounce(item)"
                        />
                    </template>
                </v-tooltip>
            </template>

            <template #expanded-row="{ columns, item }">
                <tr>
                    <td :colspan="columns.length" class="pa-4 bg-surface">
                        <div v-if="loadingDetail" class="d-flex justify-center py-6">
                            <v-progress-circular indeterminate size="28" color="primary" />
                        </div>
                        <div v-else-if="detail && detail.id === item.id">
                            <div class="d-flex flex-wrap ga-4 mb-3 text-caption text-medium-emphasis">
                                <span>Opened {{ when(detail.openedAt) }}</span>
                                <span>Grid {{ detail.grid ?? "unknown" }}</span>
                                <span>{{ detail.spots.length }} most recent spots</span>
                                <span v-if="detail.slackMessageTs">
                                    Slack {{ detail.slackChannelId }} / {{ detail.slackMessageTs }}
                                </span>
                            </div>
                            <v-data-table
                                :headers="spotHeaders"
                                :items="detail.spots"
                                item-value="id"
                                density="compact"
                                :items-per-page="25"
                            >
                                <template #[`item.heardAt`]="{ item: spot }">{{ when(spot.heardAt) }}</template>
                                <template #[`item.frequencyHz`]="{ item: spot }">
                                    {{ megahertz(spot.frequencyHz) }}
                                </template>
                                <template #[`item.snr`]="{ item: spot }">
                                    <span v-if="spot.snr != null">{{ spot.snr }} dB</span>
                                </template>
                                <template #[`item.wpm`]="{ item: spot }">
                                    <span v-if="spot.wpm != null">{{ spot.wpm }} wpm</span>
                                </template>
                                <template #[`item.distanceKm`]="{ item: spot }">
                                    <span class="text-no-wrap">{{ kilometres(spot.distanceKm) }}</span>
                                </template>
                            </v-data-table>
                        </div>
                    </td>
                </tr>
            </template>
        </v-data-table>
    </v-card>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { api, errorMessage, Session, SessionDetail } from "@/api/admin";
import { useAdminData } from "@/composables/useAdminData";
import { useNotice } from "@/composables/useNotice";
import { ago, kilometres, megahertz, when } from "@/composables/useFormat";

const { status } = useAdminData();
const { notify } = useNotice();

const search = ref("");
const expanded = ref<string[]>([]);
const detail = ref<SessionDetail | null>(null);
const loadingDetail = ref(false);
const announcing = ref<string | null>(null);

const headers = [
    { title: "Callsign", key: "callsign" },
    { title: "Band / mode", key: "bandMode", sortable: false },
    { title: "Grid", key: "grid" },
    { title: "State", key: "closedAt" },
    { title: "Last heard", key: "lastHeardAt" },
    { title: "Spots", key: "spotCount" },
    { title: "Reporters", key: "reporterCount" },
    { title: "Furthest", key: "furthestKm" },
    { title: "Best signal", key: "bestSnr" },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const spotHeaders = [
    { title: "Heard", key: "heardAt" },
    { title: "Source", key: "source" },
    { title: "Reporter", key: "reporter" },
    { title: "Grid", key: "reporterGrid" },
    { title: "Country", key: "reporterCountry" },
    { title: "Frequency", key: "frequencyHz" },
    { title: "SNR", key: "snr" },
    { title: "Speed", key: "wpm" },
    { title: "Distance", key: "distanceKm" },
    { title: "Comment", key: "comment" },
];

const sessions = computed(() => status.value?.sessions ?? []);

async function reannounce(session: Session) {
    announcing.value = session.id;
    try {
        await api.reannounce(session.id);
        notify(`${session.callsign} will be announced again within a minute.`);
    } catch (failure) {
        notify(errorMessage(failure, "Could not queue the announcement."), "error");
    } finally {
        announcing.value = null;
    }
}

async function onExpand(value: unknown) {
    const ids = value as string[];
    const opened = ids[ids.length - 1];
    expanded.value = opened ? [opened] : [];

    if (!opened) {
        detail.value = null;
        return;
    }

    loadingDetail.value = true;
    try {
        detail.value = await api.session(opened);
    } catch {
        notify("Could not load that session.", "error");
        expanded.value = [];
    } finally {
        loadingDetail.value = false;
    }
}
</script>
