<template>
    <figure class="chart">
        <figcaption class="legend">
            <span><i class="swatch in" />Money in</span>
            <span><i class="swatch out" />Money out</span>
        </figcaption>
        <div ref="plot" class="plot" @pointerleave="active = null">
            <svg :viewBox="`0 0 ${width} ${height}`" role="img" :aria-label="summary">
                <g class="grid">
                    <template v-for="tick in scale.ticks" :key="tick">
                        <line :x1="left" :x2="width - right" :y1="y(tick)" :y2="y(tick)" />
                        <text :x="left - 8" :y="y(tick)" dy="0.32em" text-anchor="end">{{ dollars(tick) }}</text>
                    </template>
                </g>
                <g v-for="(group, index) in groups" :key="group.label">
                    <rect
                        class="hit"
                        :class="{ active: active === index }"
                        :x="bandX(index)"
                        :y="top"
                        :width="band"
                        :height="plotHeight"
                        tabindex="0"
                        :aria-label="`${group.label}: in ${money(group.income)}, out ${money(group.expenses)}`"
                        @pointerenter="active = index"
                        @focus="active = index"
                        @blur="active = null"
                    />
                    <path class="in" :d="columnPath(barX(index, 0), y(group.income), barWidth, y(0) - y(group.income))" />
                    <path class="out" :d="columnPath(barX(index, 1), y(group.expenses), barWidth, y(0) - y(group.expenses))" />
                    <text class="tick" :x="bandX(index) + band / 2" :y="height - 6" text-anchor="middle">{{ group.label }}</text>
                </g>
                <line class="baseline" :x1="left" :x2="width - right" :y1="y(0)" :y2="y(0)" />
            </svg>
            <div v-if="active !== null" class="tooltip" :style="tooltipStyle">
                <div class="tooltip-title">{{ groups[active].label }}</div>
                <div><i class="key in" /><strong>{{ money(groups[active].income) }}</strong> in</div>
                <div><i class="key out" /><strong>{{ money(groups[active].expenses) }}</strong> out</div>
            </div>
        </div>
    </figure>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useWidth } from "./useWidth";
import { columnPath, dollars, money, niceScale } from "./scale";

const props = defineProps<{ groups: { label: string; income: number; expenses: number }[] }>();

const plot = ref<HTMLElement | null>(null);
const width = useWidth(plot, 640);
const height = 240;
const left = 56;
const right = 8;
const top = 12;
const bottom = 28;
const plotHeight = height - top - bottom;
const active = ref<number | null>(null);

const scale = computed(() => niceScale(Math.max(0, ...props.groups.flatMap((g) => [g.income, g.expenses]))));
const band = computed(() => (width.value - left - right) / Math.max(1, props.groups.length));
const barWidth = computed(() => Math.min(24, (band.value - 16) / 2));
const summary = computed(() =>
    props.groups.map((g) => `${g.label}: in ${money(g.income)}, out ${money(g.expenses)}`).join("; "),
);
const tooltipStyle = computed(() => {
    const index = active.value ?? 0;
    const center = (bandX(index) + band.value / 2) / width.value;
    return center > 0.6 ? { right: `${(1 - center) * 100 + 4}%` } : { left: `${center * 100 + 4}%` };
});

function y(value: number): number {
    return top + plotHeight - (value / scale.value.max) * plotHeight;
}

function bandX(index: number): number {
    return left + index * band.value;
}

function barX(index: number, slot: number): number {
    const pair = barWidth.value * 2 + 2;
    return bandX(index) + (band.value - pair) / 2 + slot * (barWidth.value + 2);
}
</script>

<style scoped src="./chart.css"></style>
