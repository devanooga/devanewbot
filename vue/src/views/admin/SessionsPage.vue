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
                <span v-if="item.furthestKm">
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
                                    {{ kilometres(spot.distanceKm) }}
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
import { api, SessionDetail } from "@/api/admin";
import { useAdminData } from "@/composables/useAdminData";
import { useNotice } from "@/composables/useNotice";
import { ago, kilometres, megahertz, when } from "@/composables/useFormat";

const { status } = useAdminData();
const { notify } = useNotice();

const search = ref("");
const expanded = ref<string[]>([]);
const detail = ref<SessionDetail | null>(null);
const loadingDetail = ref(false);

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
