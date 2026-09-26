<template>
    <PublicPage title="Moderation log" :loading="loading" :failed="failed">
        <template #intro>
            Every administrative action taken in the Devanooga community, per our
            <a href="https://www.devanooga.com/code-of-conduct/">code of conduct</a>.
        </template>

        <div class="card">
            <table>
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Action</th>
                        <th>Reason</th>
                        <th>Administrator</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="entry in entries" :key="entry.id">
                        <td class="when">
                            <div>{{ day(entry.occurredAt) }}</div>
                            <div class="time muted">{{ time(entry.occurredAt) }}</div>
                        </td>
                        <td><InlineText :text="entry.action" /></td>
                        <td class="muted"><InlineText :text="entry.reason" /></td>
                        <td class="admin muted">{{ entry.administrator }}</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </PublicPage>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import axios from "axios";
import InlineText from "@/components/InlineText.vue";
import PublicPage from "@/components/PublicPage.vue";

interface Entry {
    id: string;
    occurredAt: string;
    action: string;
    reason: string;
    administrator: string;
}

const CommunityTimeZone = "America/New_York";

const entries = ref<Entry[]>([]);
const loading = ref(true);
const failed = ref(false);

const dayFormat = new Intl.DateTimeFormat("en-CA", { timeZone: CommunityTimeZone });
const timeFormat = new Intl.DateTimeFormat("en-US", {
    timeZone: CommunityTimeZone,
    hour: "numeric",
    minute: "2-digit",
    timeZoneName: "short",
});

function day(value: string): string {
    return dayFormat.format(new Date(value));
}

function time(value: string): string {
    return timeFormat.format(new Date(value));
}

onMounted(async () => {
    try {
        entries.value = (await axios.get<Entry[]>("/api/v0/moderation")).data;
    } catch {
        failed.value = true;
    } finally {
        loading.value = false;
    }
});
</script>

<style scoped>
.when {
    white-space: nowrap;
}

.time {
    font-size: 13px;
}

@media (max-width: 720px) {
    .when {
        display: flex;
        gap: 8px;
        font-weight: 600;
    }

    .time {
        font-size: inherit;
        font-weight: 400;
    }

    .admin::before {
        content: "by ";
    }
}
</style>
