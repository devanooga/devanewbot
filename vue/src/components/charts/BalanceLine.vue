<template>
    <figure class="chart">
        <div ref="plot" class="plot" @pointerleave="active = null">
            <svg :viewBox="`0 0 ${width} ${height}`" role="img" :aria-label="summary">
                <g class="grid">
                    <template v-for="tick in scale.ticks" :key="tick">
                        <line :x1="left" :x2="width - right" :y1="y(tick)" :y2="y(tick)" />
                        <text :x="left - 8" :y="y(tick)" dy="0.32em" text-anchor="end">{{ dollars(tick) }}</text>
                    </template>
                </g>
                <text
                    v-for="year in yearTicks"
                    :key="year.label"
                    class="tick"
                    :x="x(year.index)"
                    :y="height - 6"
                    text-anchor="middle"
                >
                    {{ year.label }}
                </text>
                <path class="area" :d="areaPath" />
                <path class="line" :d="linePath" />
                <line class="baseline" :x1="left" :x2="width - right" :y1="y(0)" :y2="y(0)" />
                <template v-if="active !== null">
                    <line class="crosshair" :x1="x(active)" :x2="x(active)" :y1="top" :y2="y(0)" />
                    <circle class="dot" :cx="x(active)" :cy="y(points[active].value)" r="4" />
                </template>
                <template v-else-if="points.length">
                    <circle class="dot" :cx="x(points.length - 1)" :cy="y(last.value)" r="4" />
                    <text class="value" :x="x(points.length - 1) - 8" :y="y(last.value) - 10" text-anchor="end">
                        {{ money(last.value) }}
                    </text>
                </template>
                <rect
                    class="hit"
                    :x="left"
                    :y="top"
                    :width="width - left - right"
                    :height="plotHeight"
                    tabindex="0"
                    :aria-label="`Balance, ${summary}. Use the arrow keys to step through months.`"
                    @pointermove="track"
                    @focus="active = active ?? points.length - 1"
                    @blur="active = null"
                    @keydown.left.prevent="step(-1)"
                    @keydown.right.prevent="step(1)"
                />
            </svg>
            <div v-if="active !== null" class="tooltip" :style="tooltipStyle">
                <div class="tooltip-title">{{ points[active].label }}</div>
                <div><i class="key in" /><strong>{{ money(points[active].value) }}</strong> balance</div>
            </div>
        </div>
    </figure>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useWidth } from "./useWidth";
import { dollars, money, niceScale } from "./scale";

const props = defineProps<{ points: { label: string; year: string; value: number }[] }>();

const plot = ref<HTMLElement | null>(null);
const width = useWidth(plot, 640);
const height = 220;
const left = 56;
const right = 16;
const top = 16;
const bottom = 28;
const plotHeight = height - top - bottom;
const active = ref<number | null>(null);

const scale = computed(() => niceScale(Math.max(0, ...props.points.map((point) => point.value))));
const last = computed(() => props.points[props.points.length - 1]);
const linePath = computed(() => props.points.map((point, i) => `${i ? "L" : "M"}${x(i)},${y(point.value)}`).join(""));
const areaPath = computed(() =>
    props.points.length ? `${linePath.value}L${x(props.points.length - 1)},${y(0)}L${x(0)},${y(0)}Z` : "",
);
const yearTicks = computed(() =>
    props.points
        .map((point, index) => ({ label: point.year, index }))
        .filter((point, index, all) => index === 0 || point.label !== all[index - 1].label),
);
const summary = computed(() =>
    props.points.length ? `from ${money(props.points[0].value)} in ${props.points[0].label} to ${money(last.value.value)} in ${last.value.label}` : "no data",
);
const tooltipStyle = computed(() => {
    const share = x(active.value ?? 0) / width.value;
    return share > 0.6 ? { right: `${(1 - share) * 100 + 2}%` } : { left: `${share * 100 + 2}%` };
});

function x(index: number): number {
    return left + (index / Math.max(1, props.points.length - 1)) * (width.value - left - right);
}

function y(value: number): number {
    return top + plotHeight - (value / scale.value.max) * plotHeight;
}

function track(event: PointerEvent) {
    const bounds = (event.currentTarget as SVGRectElement).getBoundingClientRect();
    const share = (event.clientX - bounds.left) / bounds.width;
    active.value = Math.max(0, Math.min(props.points.length - 1, Math.round(share * (props.points.length - 1))));
}

function step(delta: number) {
    active.value = Math.max(0, Math.min(props.points.length - 1, (active.value ?? props.points.length - 1) + delta));
}
</script>

<style scoped src="./chart.css"></style>
