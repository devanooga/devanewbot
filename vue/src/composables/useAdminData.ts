import { onUnmounted, ref } from "vue";
import { useRouter } from "vue-router";
import { api, isUnauthorized, Status } from "@/api/admin";
import { useAuth } from "@/composables/useAuth";

const RefreshIntervalMs = 30_000;

const status = ref<Status | null>(null);
const refreshedAt = ref<Date | null>(null);
const failure = ref("");

export function useAdminData() {
    const router = useRouter();
    const { clear } = useAuth();
    let timer: number | undefined;

    async function refresh() {
        try {
            status.value = await api.status();
            refreshedAt.value = new Date();
            failure.value = "";
        } catch (error) {
            if (isUnauthorized(error)) {
                clear();
                router.push("/admin/login");
                return;
            }
            failure.value = "Could not reach the server.";
        }
    }

    function startAutoRefresh() {
        stopAutoRefresh();
        timer = window.setInterval(refresh, RefreshIntervalMs);
    }

    function stopAutoRefresh() {
        if (timer !== undefined) {
            window.clearInterval(timer);
            timer = undefined;
        }
    }

    onUnmounted(stopAutoRefresh);

    return { status, refreshedAt, failure, refresh, startAutoRefresh, stopAutoRefresh };
}
