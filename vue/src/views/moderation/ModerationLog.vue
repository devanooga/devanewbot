<template>
    <div class="page">
        <div class="moderation">
            <header>
                <h1>Moderation log</h1>
                <p>
                    Every administrative action taken in the Devanooga
                    community, per our
                    <a href="https://www.devanooga.com/code-of-conduct/"
                        >code of conduct</a
                    >.
                </p>
            </header>

            <p v-if="failed" class="status">
                Could not load the log. Try again in a moment.
            </p>
            <p v-else-if="loading" class="status">Loading…</p>

            <div v-else class="table">
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
                                <div class="time">
                                    {{ time(entry.occurredAt) }}
                                </div>
                            </td>
                            <td><InlineText :text="entry.action" /></td>
                            <td class="reason">
                                <InlineText :text="entry.reason" />
                            </td>
                            <td class="admin">{{ entry.administrator }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
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

const dayFormat = new Intl.DateTimeFormat("en-CA", {
    timeZone: CommunityTimeZone,
});
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
:global(body) {
    margin: 0;
}

.page {
    --page: #f5f6f8;
    --surface: #ffffff;
    --text: #1d2026;
    --muted: #5b6472;
    --border: #e1e4e8;
    --code: #eef0f3;
    --link: #c4513d;
    min-height: 100vh;
    background: var(--page);
    color: var(--text);
}

@media (prefers-color-scheme: dark) {
    .page {
        --page: #0f1115;
        --surface: #181b22;
        --text: #e6e8eb;
        --muted: #9aa3b0;
        --border: #2a2e37;
        --code: #242833;
        --link: #f08a77;
    }
}

.moderation {
    box-sizing: border-box;
    max-width: 1100px;
    margin: 0 auto;
    padding: 48px 16px;
    font-family: "Open Sans", sans-serif;
    line-height: 1.5;
}

header {
    margin-bottom: 24px;
}

h1 {
    font-size: 32px;
    font-weight: 700;
    margin: 0 0 8px;
}

header p,
.status {
    color: var(--muted);
    margin: 0;
}

a {
    color: var(--link);
}

.table {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 8px;
    overflow: hidden;
}

table {
    width: 100%;
    border-collapse: collapse;
    font-size: 15px;
}

th {
    text-align: left;
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--muted);
    padding: 12px 16px;
    border-bottom: 1px solid var(--border);
}

td {
    vertical-align: top;
    padding: 12px 16px;
    border-bottom: 1px solid var(--border);
    overflow-wrap: anywhere;
}

tbody tr:last-child td {
    border-bottom: 0;
}

.when {
    white-space: nowrap;
}

.time,
.admin,
.reason {
    color: var(--muted);
}

.time {
    font-size: 13px;
}

code {
    font-size: 0.9em;
    background: var(--code);
    border-radius: 4px;
    padding: 1px 4px;
}

@media (max-width: 720px) {
    .moderation {
        padding: 32px 16px;
    }

    thead {
        display: none;
    }

    table,
    tbody,
    tr,
    td {
        display: block;
    }

    tr {
        padding: 12px 16px;
        border-bottom: 1px solid var(--border);
    }

    tbody tr:last-child {
        border-bottom: 0;
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

    .admin::before {
        content: "by ";
    }
}
</style>
