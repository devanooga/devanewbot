<template>
    <v-card-text class="pt-0">
        <div class="d-flex ga-2 flex-wrap mb-2">
            <v-chip color="success" variant="tonal">{{ count("New") }} new</v-chip>
            <v-chip color="warning" variant="tonal">{{ count("Changed") }} changed</v-chip>
            <v-chip variant="tonal">{{ count("Unchanged") }} already up to date</v-chip>
            <v-chip v-if="missing.length" color="error" variant="tonal">{{ missing.length }} no longer in {{ source }}</v-chip>
        </div>
        <div v-if="note" class="text-caption text-medium-emphasis">{{ note }}</div>
    </v-card-text>

    <v-data-table
        v-if="pending.length"
        v-model="selected"
        :headers="headers"
        :items="pending"
        item-value="key"
        show-select
        :items-per-page="50"
        density="compact"
    >
        <template #[`item.status`]="{ item }">
            <v-chip size="small" variant="tonal" :color="item.status === 'New' ? 'success' : 'warning'">{{ item.status }}</v-chip>
            <div v-if="item.changes.length" class="text-caption text-medium-emphasis">{{ item.changes.join(", ") }}</div>
        </template>
        <template #[`item.title`]="{ item }">
            <div>{{ item.title }}</div>
            <div v-if="item.subtitle" class="text-caption text-medium-emphasis">{{ item.subtitle }}</div>
        </template>
        <template #[`item.amount`]="{ item }">{{ money(item.amount) }}</template>
    </v-data-table>

    <v-card-text v-if="missing.length">
        <div class="text-body-2 mb-1">In the ledger but not in this export</div>
        <div class="text-caption text-medium-emphasis mb-2">Ticked ones get hidden from the public page.</div>
        <v-checkbox
            v-for="item in missing"
            :key="item.id"
            v-model="hidden"
            :value="item.id"
            :label="item.label"
            density="compact"
            hide-details
        />
    </v-card-text>

    <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="emit('discard')">Discard</v-btn>
        <v-btn color="primary" variant="flat" :loading="applying" :disabled="selected.length === 0 && hidden.length === 0" @click="emit('apply')">
            Apply {{ selected.length }} {{ selected.length === 1 ? "change" : "changes" }}
            <template v-if="hidden.length">and hide {{ hidden.length }}</template>
        </v-btn>
    </v-card-actions>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { money } from "@/components/charts/scale";

export interface DiffRow {
    key: string;
    status: "New" | "Changed" | "Unchanged";
    changes: string[];
    date: string;
    title: string;
    subtitle: string;
    amount: number;
}

const props = defineProps<{
    rows: DiffRow[];
    missing: { id: string; label: string }[];
    source: string;
    note?: string;
    applying: boolean;
}>();
const emit = defineEmits<{ apply: []; discard: [] }>();
const selected = defineModel<string[]>("selected", { required: true });
const hidden = defineModel<string[]>("hidden", { required: true });

const headers = [
    { title: "Status", key: "status" },
    { title: "Date", key: "date", nowrap: true },
    { title: "Description", key: "title", minWidth: "220px" },
    { title: "Amount", key: "amount", align: "end" as const, nowrap: true },
];

const pending = computed(() => props.rows.filter((row) => row.status !== "Unchanged"));

function count(status: string): number {
    return props.rows.filter((row) => row.status === status).length;
}
</script>
