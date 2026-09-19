import { ref } from "vue";
import { api, CurrentUser } from "@/api/admin";

const user = ref<CurrentUser | null>(null);
const resolved = ref(false);

export function useAuth() {
    async function resolve(): Promise<CurrentUser | null> {
        if (!resolved.value) {
            try {
                user.value = await api.me();
            } catch {
                user.value = null;
            }
            resolved.value = true;
        }
        return user.value;
    }

    async function login(email: string, password: string) {
        user.value = await api.login(email, password);
        resolved.value = true;
    }

    async function logout() {
        await api.logout();
        user.value = null;
        resolved.value = true;
    }

    function clear() {
        user.value = null;
        resolved.value = true;
    }

    return { user, resolve, login, logout, clear };
}
