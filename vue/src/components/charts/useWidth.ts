import { onBeforeUnmount, onMounted, Ref, ref } from "vue";

// Charts draw in real pixels so their 12px labels stay 12px at any card width.
export function useWidth(element: Ref<HTMLElement | null>, fallback: number) {
    const width = ref(fallback);
    let observer: ResizeObserver | undefined;

    onMounted(() => {
        if (!element.value) {
            return;
        }
        width.value = element.value.clientWidth || fallback;
        observer = new ResizeObserver(([entry]) => {
            width.value = Math.max(240, Math.round(entry.contentRect.width));
        });
        observer.observe(element.value);
    });

    onBeforeUnmount(() => observer?.disconnect());

    return width;
}
