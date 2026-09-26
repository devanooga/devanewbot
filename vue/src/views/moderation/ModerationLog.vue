<template>
    <div class="moderation">
        <header>
            <h1>Moderation log</h1>
            <p>
                Every administrative action taken in the Devanooga community, per our
                <a href="https://www.devanooga.com/code-of-conduct/">code of conduct</a>.
            </p>
        </header>

        <p v-if="failed" class="status">Could not load the log. Try again in a moment.</p>
        <p v-else-if="loading" class="status">Loading…</p>

        <table v-else>
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
                        <div class="time">{{ time(entry.occurredAt) }}</div>
                    </td>
                    <td><InlineText :text="entry.action" /></td>
                    <td class="reason"><InlineText :text="entry.reason" /></td>
                    <td class="admin">{{ entry.administrator }}</td>
                </tr>
            </tbody>
        </table>
    </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import axios from "axios";
import InlineText from "@/components/InlineText.vue";

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
.moderation {
    box-sizing: border-box;
    max-width: 1100px;
    margin: 0 auto;
    padding: 48px 16px;
    font-family: "Open Sans", sans-serif;
    color: #232323;
    line-height: 1.45;
}

header {
    margin-bottom: 32px;
}

h1 {
    font-size: 32px;
    font-weight: 700;
    margin: 0 0 8px;
}

header p,
.status {
    color: #555;
    margin: 0;
}

a {
    color: #2f6fc4;
}

table {
    width: 100%;
    border-collapse: collapse;
    font-size: 15px;
}

th {
    text-align: left;
    font-size: 13px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: #666;
    padding: 8px 12px;
    border-bottom: 2px solid #ddd;
}

td {
    vertical-align: top;
    padding: 12px;
    border-bottom: 1px solid #e6e6e6;
    overflow-wrap: anywhere;
}

.when {
    white-space: nowrap;
}

.time,
.admin {
    color: #666;
}

.time {
    font-size: 13px;
}

code {
    font-size: 0.9em;
    background: #eee;
    border-radius: 3px;
    padding: 0 3px;
}

@media (max-width: 720px) {
    thead {
        display: none;
    }

    tr,
    td {
        display: block;
    }

    tr {
        padding: 12px 0;
        border-bottom: 1px solid #e6e6e6;
    }

    td {
        padding: 2px 0;
        border: 0;
    }

    .when {
        display: flex;
        gap: 8px;
        font-weight: 600;
    }

    .time {
        font-size: inherit;
        font-weight: 400;
    }

    .reason {
        color: #555;
    }

    .admin::before {
        content: "by ";
    }
}
</style>
