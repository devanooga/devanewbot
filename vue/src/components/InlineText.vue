<template>
    <span>
        <template v-for="(segment, index) in segments" :key="index">
            <code v-if="segment.kind === 'code'">{{ segment.text }}</code>
            <a v-else-if="segment.kind === 'link'" :href="segment.href" target="_blank" rel="noopener">{{ segment.text }}</a>
            <template v-else>{{ segment.text }}</template>
        </template>
    </span>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { inlineSegments } from "@/composables/useFormat";

const props = defineProps<{ text: string }>();
const segments = computed(() => inlineSegments(props.text));
</script>
