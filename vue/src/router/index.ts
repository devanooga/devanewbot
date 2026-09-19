import { createRouter, createWebHistory, RouteRecordRaw } from "vue-router";
import { useAuth } from "@/composables/useAuth";

const routes: Array<RouteRecordRaw> = [
    {
        path: "/",
        name: "home",
        component: () => import("../views/home/index.vue"),
        meta: { title: "Join the community" },
    },
    {
        path: "/admin/login",
        name: "admin-login",
        component: () => import("../views/admin/LoginPage.vue"),
        meta: { title: "Sign in" },
    },
    {
        path: "/admin",
        component: () => import("../views/admin/AdminLayout.vue"),
        meta: { requiresAuth: true },
        children: [
            { path: "", redirect: "/admin/dashboard" },
            {
                path: "dashboard",
                name: "admin-dashboard",
                component: () => import("../views/admin/DashboardPage.vue"),
                meta: { title: "Dashboard" },
            },
            {
                path: "sessions",
                name: "admin-sessions",
                component: () => import("../views/admin/SessionsPage.vue"),
                meta: { title: "Sessions" },
            },
            {
                path: "watch-list",
                name: "admin-watch-list",
                component: () => import("../views/admin/WatchListPage.vue"),
                meta: { title: "Watch list" },
            },
            {
                path: "invites",
                name: "admin-invites",
                component: () => import("../views/admin/InvitesPage.vue"),
                meta: { title: "Invites" },
            },
            {
                path: "logs",
                name: "admin-logs",
                component: () => import("../views/admin/LogsPage.vue"),
                meta: { title: "Logs" },
            },
            {
                path: "users",
                name: "admin-users",
                component: () => import("../views/admin/UsersPage.vue"),
                meta: { title: "Users" },
            },
            {
                path: "account",
                name: "admin-account",
                component: () => import("../views/admin/AccountPage.vue"),
                meta: { title: "Account" },
            },
        ],
    },
];

const router = createRouter({
    history: createWebHistory(process.env.BASE_URL),
    routes,
});

router.beforeEach(async (to) => {
    if (!to.meta.requiresAuth) {
        return true;
    }

    const { resolve } = useAuth();
    if (await resolve()) {
        return true;
    }

    return { path: "/admin/login", query: { next: to.fullPath } };
});

const SiteName = "Devanooga";
const AdminName = "devanewbot admin";

router.afterEach((to) => {
    const page = to.meta.title as string | undefined;
    const suffix = to.path.startsWith("/admin") ? AdminName : SiteName;
    document.title = page ? `${page} · ${suffix}` : suffix;
});

export default router;
