<template>
    <p v-if="items.length === 0" class="empty">Nothing in this period.</p>
    <ul v-else class="bars">
        <li v-for="item in items" :key="item.label">
            <div class="row">
                <span class="name">{{ item.label }}</span>
                <span class="amount">{{ money(item.value) }}</span>
            </div>
            <div class="track">
                <div class="bar" :class="series" :style="{ width: `${share(item.value)}%` }" />
            </div>
        </li>
    </ul>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { money } from "./scale";

const props = defineProps<{ items: { label: string; value: number }[]; series: "in" | "out" }>();

const max = computed(() => Math.max(0, ...props.items.map((item) => item.value)));

function share(value: number): number {
    return max.value === 0 ? 0 : Math.max(0.75, (value / max.value) * 100);
}
</script>

<style scoped>
.empty {
    margin: 0;
    color: var(--muted);
}

.bars {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    gap: 12px;
}

.row {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    font-size: 14px;
    margin-bottom: 4px;
}

.name {
    color: var(--muted);
}

.amount {
    font-variant-numeric: tabular-nums;
}

.track {
    height: 12px;
}

.bar {
    height: 100%;
    border-radius: 0 4px 4px 0;
}

.bar.in {
    background: var(--series-in);
}

.bar.out {
    background: var(--series-out);
}
</style>
