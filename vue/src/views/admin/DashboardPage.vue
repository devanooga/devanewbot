<template>
    <div v-if="!status" class="d-flex justify-center py-16">
        <v-progress-circular indeterminate color="primary" size="48" />
    </div>

    <div v-else>
        <v-alert
            v-for="problem in status.problems"
            :key="problem"
            type="warning"
            variant="tonal"
            class="mb-4"
        >
            {{ problem }}
        </v-alert>

        <h2 class="text-subtitle-1 text-medium-emphasis mb-3">Feeds</h2>
        <v-row>
            <v-col v-for="feed in status.feeds" :key="feed.name" cols="12" sm="6" md="4" lg="3">
                <v-card :border="true" class="h-100">
                    <v-card-item>
                        <template #prepend>
                            <v-avatar :color="feed.connected ? 'success' : 'error'" size="36" variant="tonal">
                                <v-icon :icon="feed.connected ? 'mdi-access-point' : 'mdi-access-point-off'" />
                            </v-avatar>
                        </template>
                        <v-card-title class="text-body-1">{{ feed.name }}</v-card-title>
                        <v-card-subtitle>
                            {{ feed.connected ? "connected" : "down" }}
                        </v-card-subtitle>
                    </v-card-item>
                    <v-card-text class="pt-0">
                        <div class="text-caption text-medium-emphasis">
                            Last spot {{ ago(feed.lastSpotAt) }}
                        </div>
                        <div v-if="feed.lastError" class="text-caption text-error mt-1">
                            {{ feed.lastError }}
                        </div>
                    </v-card-text>
                </v-card>
            </v-col>

            <v-col v-if="status.feeds.length === 0" cols="12">
                <v-alert type="info" variant="tonal">No feeds have reported in yet.</v-alert>
            </v-col>
        </v-row>

        <h2 class="text-subtitle-1 text-medium-emphasis mt-8 mb-3">Spots</h2>
        <v-row>
            <v-col cols="6" md="3">
                <v-card border>
                    <v-card-text>
                        <div class="text-caption text-medium-emphasis">Stored</div>
                        <div class="text-h4">{{ count(status.spots.total) }}</div>
                    </v-card-text>
                </v-card>
            </v-col>
            <v-col cols="6" md="3">
                <v-card border>
                    <v-card-text>
                        <div class="text-caption text-medium-emphasis">Last 24 hours</div>
                        <div class="text-h4">{{ count(status.spots.lastDay) }}</div>
                    </v-card-text>
                </v-card>
            </v-col>
            <v-col cols="6" md="3">
                <v-card border>
                    <v-card-text>
                        <div class="text-caption text-medium-emphasis">Live sessions</div>
                        <div class="text-h4">{{ liveSessions }}</div>
                    </v-card-text>
                </v-card>
            </v-col>
            <v-col cols="6" md="3">
                <v-card border>
                    <v-card-text>
                        <div class="text-caption text-medium-emphasis">Watched callsigns</div>
                        <div class="text-h4">{{ status.watches.length }}</div>
                    </v-card-text>
                </v-card>
            </v-col>
        </v-row>

        <v-card border class="mt-4">
            <v-card-title class="text-body-1">Spots by source, last 24 hours</v-card-title>
            <v-card-text>
                <div v-if="status.spots.bySource.length === 0" class="text-medium-emphasis">
                    Nothing in the last day.
                </div>
                <div v-for="source in sortedSources" :key="source.source" class="mb-3">
                    <div class="d-flex justify-space-between text-body-2 mb-1">
                        <span>{{ source.source }}</span>
                        <span class="text-medium-emphasis">{{ count(source.count) }}</span>
                    </div>
                    <v-progress-linear
                        :model-value="(source.count / maxSourceCount) * 100"
                        color="primary"
                        height="6"
                        rounded
                    />
                </div>
            </v-card-text>
        </v-card>

        <h2 class="text-subtitle-1 text-medium-emphasis mt-8 mb-3">Jobs</h2>
        <div class="d-flex flex-wrap ga-2 mb-3">
            <v-chip v-for="stat in jobStats" :key="stat.label" variant="tonal" :color="stat.color">
                {{ stat.label }}: {{ stat.value }}
            </v-chip>
        </div>
        <v-card border>
            <v-data-table
                :headers="jobHeaders"
                :items="status.jobs.recurring"
                item-value="id"
                hide-default-footer
                :items-per-page="-1"
            >
                <template #[`item.lastExecution`]="{ item }">{{ when(item.lastExecution) }}</template>
                <template #[`item.nextExecution`]="{ item }">{{ when(item.nextExecution) }}</template>
                <template #[`item.lastJobState`]="{ item }">
                    <v-chip
                        v-if="item.lastJobState"
                        size="small"
                        variant="tonal"
                        :color="item.lastJobState === 'Failed' ? 'error' : 'success'"
                    >
                        {{ item.lastJobState }}
                    </v-chip>
                </template>
                <template #[`item.error`]="{ item }">
                    <span class="text-error text-caption">{{ item.error }}</span>
                </template>
            </v-data-table>
        </v-card>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useAdminData } from "@/composables/useAdminData";
import { ago, count, when } from "@/composables/useFormat";

const { status } = useAdminData();

const jobHeaders = [
    { title: "Job", key: "id" },
    { title: "Cron", key: "cron" },
    { title: "Last run", key: "lastExecution" },
    { title: "Next run", key: "nextExecution" },
    { title: "State", key: "lastJobState" },
    { title: "Error", key: "error" },
];

const liveSessions = computed(
    () => status.value?.sessions.filter((session) => session.closedAt === null).length ?? 0,
);

const sortedSources = computed(() =>
    [...(status.value?.spots.bySource ?? [])].sort((a, b) => b.count - a.count),
);

const maxSourceCount = computed(() =>
    Math.max(1, ...(status.value?.spots.bySource ?? []).map((source) => source.count)),
);

const jobStats = computed(() => {
    const jobs = status.value?.jobs;
    return [
        { label: "Enqueued", value: jobs?.enqueued ?? 0, color: "info" },
        { label: "Processing", value: jobs?.processing ?? 0, color: "info" },
        { label: "Scheduled", value: jobs?.scheduled ?? 0, color: "secondary" },
        { label: "Succeeded", value: count(jobs?.succeeded ?? 0), color: "success" },
        { label: "Failed", value: count(jobs?.failed ?? 0), color: jobs?.failed ? "error" : "secondary" },
    ];
});
</script>
