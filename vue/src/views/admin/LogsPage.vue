<template>
    <v-card border>
        <v-card-title class="d-flex align-center flex-wrap ga-3 text-body-1">
            Application log
            <v-spacer />
            <v-select
                v-model="level"
                :items="levels"
                label="Minimum level"
                style="max-width: 180px"
                @update:model-value="restart"
            />
            <v-text-field
                v-model="search"
                label="Search"
                prepend-inner-icon="mdi-magnify"
                clearable
                style="max-width: 240px"
                @keyup.enter="restart"
                @click:clear="restart"
            />
            <v-btn
                :color="live ? 'success' : 'secondary'"
                :prepend-icon="live ? 'mdi-pause' : 'mdi-play'"
                variant="tonal"
                @click="live = !live"
            >
                {{ live ? "Live" : "Paused" }}
            </v-btn>
            <v-btn variant="text" prepend-icon="mdi-notification-clear-all" @click="restart">Clear</v-btn>
        </v-card-title>

        <v-card-subtitle class="pb-3">
            Showing {{ entries.length }} of {{ buffered }} buffered entries, newest first.
            <span v-if="missed > 0" class="text-warning">{{ missed }} entries scrolled out of the buffer.</span>
        </v-card-subtitle>

        <v-divider />

        <div class="log">
            <div v-if="entries.length === 0" class="pa-8 text-center text-medium-emphasis">
                Nothing logged at this level yet.
            </div>
            <div
                v-for="entry in entries"
                :key="entry.sequence"
                class="entry"
                :class="{ expandable: entry.exception }"
                @click="toggle(entry)"
            >
                <span class="time text-medium-emphasis">{{ clock(entry.timestampUtc) }}</span>
                <span class="level" :class="`text-${levelColor(entry.level)}`">{{ short(entry.level) }}</span>
                <span class="category text-medium-emphasis" :title="entry.category">{{ leaf(entry.category) }}</span>
                <span class="message">
                    {{ entry.message }}
                    <v-icon
                        v-if="entry.exception"
                        :icon="expanded.has(entry.sequence) ? 'mdi-chevron-up' : 'mdi-alert-circle-outline'"
                        size="14"
                        class="ml-1 text-error"
                    />
                    <pre v-if="entry.exception && expanded.has(entry.sequence)" class="exception">{{ entry.exception }}</pre>
                </span>
            </div>
        </div>
    </v-card>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import { api, errorMessage, LogEntry, LogLevels } from "@/api/admin";
import { useNotice } from "@/composables/useNotice";

const PollIntervalMs = 3000;
const ClientBufferLimit = 2000;

const { notify } = useNotice();

const levels = LogLevels;
const level = ref("Information");
const search = ref("");
const live = ref(true);
const entries = ref<LogEntry[]>([]);
const expanded = ref(new Set<number>());
const buffered = ref(0);
const missed = ref(0);

let afterSequence = 0;
let timer: number | undefined;

function clock(utc: string): string {
    return new Date(utc).toLocaleTimeString([], { hour12: false });
}

function short(value: string): string {
    return value.slice(0, 4).toUpperCase();
}

function leaf(category: string): string {
    return category.split(".").pop() ?? category;
}

function levelColor(value: string): string {
    if (value === "Error" || value === "Critical") {
        return "error";
    }
    if (value === "Warning") {
        return "warning";
    }
    return value === "Information" ? "info" : "medium-emphasis";
}

function toggle(entry: LogEntry) {
    if (!entry.exception) {
        return;
    }

    const next = new Set(expanded.value);
    if (next.has(entry.sequence)) {
        next.delete(entry.sequence);
    } else {
        next.add(entry.sequence);
    }
    expanded.value = next;
}

async function poll() {
    if (!live.value) {
        return;
    }

    try {
        const page = await api.logs(afterSequence, level.value, search.value);
        buffered.value = page.buffered;
        missed.value += page.missed;

        if (page.entries.length > 0) {
            entries.value = [...page.entries].reverse().concat(entries.value).slice(0, ClientBufferLimit);
        }

        afterSequence = Math.max(afterSequence, page.newest);
    } catch (failure) {
        notify(errorMessage(failure, "Could not read the log."), "error");
    }
}

function restart() {
    afterSequence = 0;
    entries.value = [];
    expanded.value = new Set();
    missed.value = 0;
    poll();
}

onMounted(() => {
    poll();
    timer = window.setInterval(poll, PollIntervalMs);
});

onUnmounted(() => {
    if (timer !== undefined) {
        window.clearInterval(timer);
    }
});
</script>

<style lang="scss" scoped>
.log {
    font-family: ui-monospace, "SF Mono", Menlo, Consolas, monospace;
    font-size: 0.78rem;
    max-height: 70vh;
    overflow-y: auto;
}

.entry {
    display: flex;
    gap: 0.75rem;
    padding: 0.2rem 1rem;
    align-items: baseline;
    border-bottom: 1px solid rgba(128, 128, 128, 0.12);

    &.expandable {
        cursor: pointer;
    }

    &:hover {
        background: rgba(128, 128, 128, 0.08);
    }
}

.time {
    flex: 0 0 auto;
}

.level {
    flex: 0 0 2.6rem;
    font-weight: 600;
}

.category {
    flex: 0 0 11rem;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.message {
    flex: 1 1 auto;
    word-break: break-word;
}

.exception {
    white-space: pre-wrap;
    margin: 0.5rem 0;
    padding: 0.5rem;
    background: rgba(128, 128, 128, 0.12);
    border-radius: 4px;
    font-size: 0.72rem;
}
</style>
