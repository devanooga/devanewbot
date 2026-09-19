<template>
    <div class="admin">
        <header>
            <h1>devanewbot admin</h1>
            <nav v-if="user">
                <span>{{ user.email }}</span>
                <span v-if="refreshedAt" class="muted">updated {{ refreshedAt.toLocaleTimeString() }}</span>
                <a href="/hangfire" target="_blank">Hangfire</a>
                <button @click="refresh">Refresh</button>
                <button @click="showPassword = !showPassword">Change password</button>
                <button @click="logout">Log out</button>
            </nav>
        </header>

        <form v-if="!user" class="stack" @submit.prevent="login">
            <label>Email <input v-model="email" type="email" autocomplete="username" required /></label>
            <label>Password <input v-model="password" type="password" autocomplete="current-password" required /></label>
            <button type="submit" :disabled="busy">Log in</button>
            <p v-if="error" class="bad">{{ error }}</p>
        </form>

        <template v-else-if="status">
            <form v-if="showPassword" class="stack" @submit.prevent="changePassword">
                <label>Current password <input v-model="currentPassword" type="password" autocomplete="current-password" required /></label>
                <label>New password <input v-model="newPassword" type="password" autocomplete="new-password" required /></label>
                <label>Confirm new password <input v-model="confirmPassword" type="password" autocomplete="new-password" required /></label>
                <button type="submit" :disabled="busy">Change password</button>
                <p v-if="passwordMessage" :class="passwordError ? 'bad' : 'ok'">{{ passwordMessage }}</p>
            </form>

            <p v-if="notice" :class="noticeError ? 'bad' : 'ok'">{{ notice }}</p>

            <section>
                <h2>Feeds</h2>
                <table>
                    <thead><tr><th>Feed</th><th>State</th><th>Connected</th><th>Last spot</th><th>Last error</th></tr></thead>
                    <tbody>
                        <tr v-for="feed in status.feeds" :key="feed.name">
                            <td>{{ feed.name }}</td>
                            <td :class="feed.connected ? 'ok' : 'bad'">{{ feed.connected ? "connected" : "down" }}</td>
                            <td>{{ when(feed.connectedAt) }}</td>
                            <td>{{ when(feed.lastSpotAt) }}</td>
                            <td>{{ feed.lastError ?? "" }}</td>
                        </tr>
                        <tr v-if="status.feeds.length === 0"><td colspan="5">No feeds have reported in yet.</td></tr>
                    </tbody>
                </table>
            </section>

            <section>
                <h2>Spots</h2>
                <p>
                    {{ status.spots.total.toLocaleString() }} stored,
                    {{ status.spots.lastDay.toLocaleString() }} in the last day
                    <span v-for="source in status.spots.bySource" :key="source.source"> · {{ source.source }} {{ source.count.toLocaleString() }}</span>
                </p>
            </section>

            <section>
                <h2>Sessions</h2>
                <table>
                    <thead><tr><th>Callsign</th><th>Band</th><th>Mode</th><th>Grid</th><th>Opened</th><th>Last heard</th><th>Closed</th><th>Spots</th><th>Reporters</th><th>Furthest</th><th>Best SNR</th></tr></thead>
                    <tbody>
                        <template v-for="session in status.sessions" :key="session.id">
                            <tr class="clickable" :class="{ selected: detail?.id === session.id }" @click="toggleSession(session.id)">
                                <td>{{ session.callsign }}</td>
                                <td>{{ session.band }}</td>
                                <td>{{ session.mode }}</td>
                                <td>{{ session.grid ?? "" }}</td>
                                <td>{{ when(session.openedAt) }}</td>
                                <td>{{ when(session.lastHeardAt) }}</td>
                                <td>{{ session.closedAt ? when(session.closedAt) : "live" }}</td>
                                <td>{{ session.spotCount }}</td>
                                <td>{{ session.reporterCount }}</td>
                                <td>{{ session.furthestKm ? `${Math.round(session.furthestKm).toLocaleString()} km ${session.furthestReporter}` : "" }}</td>
                                <td>{{ session.bestSnr != null ? `${session.bestSnr} dB ${session.bestSnrReporter}` : "" }}</td>
                            </tr>
                            <tr v-if="detail?.id === session.id">
                                <td colspan="11" class="detail">
                                    <p class="muted">
                                        Latest {{ detail.spots.length }} spots
                                        <span v-if="detail.slackMessageTs"> · Slack message {{ detail.slackMessageTs }} in {{ detail.slackChannelId }}</span>
                                    </p>
                                    <table>
                                        <thead><tr><th>Heard</th><th>Source</th><th>Reporter</th><th>Grid</th><th>Country</th><th>Frequency</th><th>SNR</th><th>Speed</th><th>Distance</th><th>Comment</th></tr></thead>
                                        <tbody>
                                            <tr v-for="spot in detail.spots" :key="spot.id">
                                                <td>{{ when(spot.heardAt) }}</td>
                                                <td>{{ spot.source }}</td>
                                                <td>{{ spot.reporter }}</td>
                                                <td>{{ spot.reporterGrid ?? "" }}</td>
                                                <td>{{ spot.reporterCountry ?? "" }}</td>
                                                <td>{{ (spot.frequencyHz / 1000000).toFixed(3) }} MHz</td>
                                                <td>{{ spot.snr != null ? `${spot.snr} dB` : "" }}</td>
                                                <td>{{ spot.wpm != null ? `${spot.wpm} wpm` : "" }}</td>
                                                <td>{{ spot.distanceKm != null ? `${Math.round(spot.distanceKm).toLocaleString()} km` : "" }}</td>
                                                <td>{{ spot.comment ?? "" }}</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </td>
                            </tr>
                        </template>
                        <tr v-if="status.sessions.length === 0"><td colspan="11">No sessions yet.</td></tr>
                    </tbody>
                </table>
            </section>

            <section>
                <h2>Invites</h2>
                <form class="row" @submit.prevent="createInvite">
                    <input v-model="inviteEmail" type="email" placeholder="Invite someone by email" required />
                    <button type="submit" :disabled="busy">Send invite</button>
                </form>
                <div class="row">
                    <select v-model="inviteFilter" @change="loadInvites">
                        <option value="">All</option>
                        <option v-for="option in inviteStatuses" :key="option" :value="option">{{ option }}</option>
                    </select>
                    <input v-model="inviteSearch" placeholder="Search email or IP" @keyup.enter="loadInvites" />
                    <button type="button" @click="loadInvites">Search</button>
                    <span class="muted">{{ invites.length }} shown</span>
                </div>
                <table>
                    <thead><tr><th>Requested</th><th>Email</th><th>IP</th><th>Location</th><th>Flag</th><th>Source</th><th>Status</th><th>Decided by</th><th>Via</th><th>Decided</th><th></th></tr></thead>
                    <tbody>
                        <tr v-for="invite in invites" :key="invite.id" :class="{ pending: invite.status === 'Pending' }">
                            <td>{{ when(invite.createdAt) }}</td>
                            <td>{{ invite.email }}</td>
                            <td>{{ invite.ip }}</td>
                            <td :title="[invite.isp, invite.proxy ? 'proxy' : '', invite.hosting ? 'datacenter' : '', invite.mobile ? 'mobile' : ''].filter(Boolean).join(' · ')">
                                {{ [invite.city, invite.region, invite.country].filter(Boolean).join(", ") }}
                                <span v-if="invite.proxy || invite.hosting" class="bad"> ⚠</span>
                            </td>
                            <td :class="flagClass(invite.flag)" :title="invite.flagMessage ?? ''">{{ invite.flag ?? "" }}</td>
                            <td>{{ invite.source }}</td>
                            <td :class="statusClass(invite.status)" :title="invite.error ?? ''">{{ invite.status }}</td>
                            <td>{{ invite.decidedBy ?? "" }}</td>
                            <td>{{ invite.decisionSource ?? "" }}</td>
                            <td>{{ when(invite.decidedAt) }}</td>
                            <td class="actions">
                                <template v-if="invite.status === 'Pending'">
                                    <button :disabled="busy" @click="decideInvite(invite, true)">Approve</button>
                                    <button :disabled="busy" @click="decideInvite(invite, false)">Decline</button>
                                </template>
                            </td>
                        </tr>
                        <tr v-if="invites.length === 0"><td colspan="11">No invites match.</td></tr>
                    </tbody>
                </table>
            </section>

            <section>
                <h2>Watch list</h2>
                <form class="row" @submit.prevent="addWatch">
                    <input v-model="newWatch.callsign" placeholder="Callsign" required />
                    <input v-model="newWatch.grid" placeholder="Grid (optional)" />
                    <input v-model="newWatch.slackUserId" placeholder="Slack user id (optional)" />
                    <button type="submit" :disabled="busy">Add</button>
                </form>
                <table>
                    <thead><tr><th>Callsign</th><th>Grid</th><th>Slack user</th><th>Added by</th><th>Added</th><th></th></tr></thead>
                    <tbody>
                        <tr v-for="watch in status.watches" :key="watch.callsign">
                            <td>{{ watch.callsign }}</td>
                            <template v-if="editing?.callsign === watch.callsign">
                                <td><input v-model="editing.grid" placeholder="Grid" /></td>
                                <td><input v-model="editing.slackUserId" placeholder="Slack user id" /></td>
                                <td>{{ watch.addedBy }}</td>
                                <td>{{ when(watch.createdAt) }}</td>
                                <td class="actions">
                                    <button :disabled="busy" @click="saveWatch">Save</button>
                                    <button @click="editing = null">Cancel</button>
                                </td>
                            </template>
                            <template v-else>
                                <td>{{ watch.grid ?? "" }}</td>
                                <td>{{ watch.slackUserId ?? "" }}</td>
                                <td>{{ watch.addedBy }}</td>
                                <td>{{ when(watch.createdAt) }}</td>
                                <td class="actions">
                                    <button @click="editing = { callsign: watch.callsign, grid: watch.grid ?? '', slackUserId: watch.slackUserId ?? '' }">Edit</button>
                                    <button :disabled="busy" @click="removeWatch(watch.callsign)">Remove</button>
                                </td>
                            </template>
                        </tr>
                        <tr v-if="status.watches.length === 0"><td colspan="6">Nobody is being watched.</td></tr>
                    </tbody>
                </table>
            </section>

            <section>
                <h2>Users</h2>
                <form class="row" @submit.prevent="addUser">
                    <input v-model="newUser.email" type="email" placeholder="Email" autocomplete="off" required />
                    <input v-model="newUser.password" type="password" placeholder="Password" autocomplete="new-password" required />
                    <button type="submit" :disabled="busy">Add admin</button>
                </form>
                <table>
                    <thead><tr><th>Email</th><th>Created</th><th>Locked until</th><th></th></tr></thead>
                    <tbody>
                        <tr v-for="account in users" :key="account.id">
                            <td>{{ account.email }}</td>
                            <td>{{ when(account.createdAt) }}</td>
                            <td>{{ account.lockoutEnd && new Date(account.lockoutEnd) > new Date() ? when(account.lockoutEnd) : "" }}</td>
                            <td class="actions">
                                <template v-if="resetting?.id === account.id">
                                    <input v-model="resetting.password" type="password" placeholder="New password" autocomplete="new-password" />
                                    <button :disabled="busy" @click="resetPassword">Set</button>
                                    <button @click="resetting = null">Cancel</button>
                                </template>
                                <template v-else>
                                    <button @click="resetting = { id: account.id, password: '' }">Reset password</button>
                                    <button :disabled="busy || account.email === user.email" @click="deleteUser(account)">Delete</button>
                                </template>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </section>

            <section>
                <h2>Jobs</h2>
                <p>
                    enqueued {{ status.jobs.enqueued }} · processing {{ status.jobs.processing }} · scheduled {{ status.jobs.scheduled }}
                    · succeeded {{ status.jobs.succeeded }} · failed {{ status.jobs.failed }}
                </p>
                <table>
                    <thead><tr><th>Job</th><th>Cron</th><th>Last run</th><th>Next run</th><th>State</th><th>Error</th></tr></thead>
                    <tbody>
                        <tr v-for="job in status.jobs.recurring" :key="job.id">
                            <td>{{ job.id }}</td>
                            <td>{{ job.cron }}</td>
                            <td>{{ when(job.lastExecution) }}</td>
                            <td>{{ when(job.nextExecution) }}</td>
                            <td :class="job.lastJobState === 'Failed' ? 'bad' : ''">{{ job.lastJobState ?? "" }}</td>
                            <td>{{ job.error ?? "" }}</td>
                        </tr>
                    </tbody>
                </table>
            </section>
        </template>

        <p v-else>Loading...</p>
    </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import axios, { AxiosError } from "axios";

const RefreshIntervalMs = 30_000;

interface User {
    email: string;
    roles: string[];
}

interface Feed {
    name: string;
    connected: boolean;
    connectedAt: string | null;
    lastSpotAt: string | null;
    lastError: string | null;
}

interface Session {
    id: string;
    callsign: string;
    band: string;
    mode: string;
    grid: string | null;
    openedAt: string;
    lastHeardAt: string;
    closedAt: string | null;
    spotCount: number;
    reporterCount: number;
    furthestKm: number | null;
    furthestReporter: string | null;
    bestSnr: number | null;
    bestSnrReporter: string | null;
}

interface Spot {
    id: number;
    source: string;
    reporter: string;
    reporterGrid: string | null;
    reporterCountry: string | null;
    frequencyHz: number;
    snr: number | null;
    wpm: number | null;
    distanceKm: number | null;
    comment: string | null;
    heardAt: string;
}

interface SessionDetail {
    id: string;
    slackChannelId: string | null;
    slackMessageTs: string | null;
    spots: Spot[];
}

interface Invite {
    id: string;
    email: string;
    ip: string;
    source: string;
    status: string;
    flag: string | null;
    flagMessage: string | null;
    city: string | null;
    region: string | null;
    country: string | null;
    isp: string | null;
    proxy: boolean;
    hosting: boolean;
    mobile: boolean;
    createdAt: string;
    decidedAt: string | null;
    decidedBy: string | null;
    decisionSource: string | null;
    error: string | null;
}

interface Watch {
    callsign: string;
    grid: string | null;
    slackUserId: string | null;
    addedBy: string;
    createdAt: string;
}

interface Account {
    id: string;
    email: string;
    createdAt: string;
    lockoutEnd: string | null;
    accessFailedCount: number;
}

interface RecurringJob {
    id: string;
    cron: string;
    lastExecution: string | null;
    nextExecution: string | null;
    lastJobState: string | null;
    error: string | null;
}

interface Status {
    feeds: Feed[];
    watches: Watch[];
    sessions: Session[];
    spots: { total: number; lastDay: number; bySource: { source: string; count: number }[] };
    jobs: { enqueued: number; processing: number; scheduled: number; succeeded: number; failed: number; recurring: RecurringJob[] };
}

const user = ref<User | null>(null);
const status = ref<Status | null>(null);
const users = ref<Account[]>([]);
const invites = ref<Invite[]>([]);
const inviteStatuses = ["Pending", "Approved", "Declined", "AlreadyInvited", "Failed"];
const inviteFilter = ref("");
const inviteSearch = ref("");
const inviteEmail = ref("");
const detail = ref<SessionDetail | null>(null);
const refreshedAt = ref<Date | null>(null);
const email = ref("");
const password = ref("");
const error = ref("");
const busy = ref(false);
const notice = ref("");
const noticeError = ref(false);
const showPassword = ref(false);
const currentPassword = ref("");
const newPassword = ref("");
const confirmPassword = ref("");
const passwordMessage = ref("");
const passwordError = ref(false);
const newWatch = ref({ callsign: "", grid: "", slackUserId: "" });
const editing = ref<{ callsign: string; grid: string; slackUserId: string } | null>(null);
const newUser = ref({ email: "", password: "" });
const resetting = ref<{ id: string; password: string } | null>(null);

let timer: number | undefined;

function when(value: string | null | undefined): string {
    return value ? new Date(value).toLocaleString() : "";
}

function describe(failure: unknown, fallback: string): string {
    const data = (failure as AxiosError).response?.data as { errors?: string[] } | undefined;
    return data?.errors?.join(" ") ?? fallback;
}

function report(message: string, isError = false) {
    notice.value = message;
    noticeError.value = isError;
}

async function run(action: () => Promise<void>, fallback: string) {
    busy.value = true;
    try {
        await action();
    } catch (failure) {
        report(describe(failure, fallback), true);
    } finally {
        busy.value = false;
    }
}

async function loadInvites() {
    const response = await axios.get<Invite[]>("/api/v0/admin/invites", {
        params: { status: inviteFilter.value || undefined, search: inviteSearch.value || undefined },
    });
    invites.value = response.data;
}

function flagClass(flag: string | null): string {
    return flag === "Green" ? "ok" : flag === "Red" ? "bad" : flag === "Yellow" ? "warn" : "";
}

function statusClass(status: string): string {
    return status === "Approved" ? "ok" : status === "Declined" || status === "Failed" ? "bad" : status === "Pending" ? "warn" : "";
}

async function refresh() {
    try {
        const [statusResponse, usersResponse] = await Promise.all([
            axios.get<Status>("/api/v0/admin/status"),
            axios.get<Account[]>("/api/v0/admin/users"),
            loadInvites(),
        ]);
        status.value = statusResponse.data;
        users.value = usersResponse.data;
        refreshedAt.value = new Date();
        if (detail.value) {
            await loadSession(detail.value.id);
        }
    } catch (failure) {
        if ((failure as AxiosError).response?.status === 401) {
            stopAutoRefresh();
            user.value = null;
            status.value = null;
        }
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

async function loadSession(id: string) {
    const response = await axios.get<SessionDetail>(`/api/v0/admin/sessions/${id}`);
    detail.value = response.data;
}

async function toggleSession(id: string) {
    if (detail.value?.id === id) {
        detail.value = null;
        return;
    }
    await run(() => loadSession(id), "Could not load the session.");
}

async function login() {
    error.value = "";
    await run(async () => {
        try {
            const response = await axios.post<User>("/api/v0/auth/login", { email: email.value, password: password.value });
            user.value = response.data;
            password.value = "";
            await refresh();
            startAutoRefresh();
        } catch (failure) {
            error.value = (failure as AxiosError).response?.status === 401 ? "Wrong email or password." : "Login failed.";
        }
    }, "Login failed.");
}

async function changePassword() {
    passwordMessage.value = "";
    passwordError.value = false;
    if (newPassword.value !== confirmPassword.value) {
        passwordMessage.value = "New passwords do not match.";
        passwordError.value = true;
        return;
    }

    busy.value = true;
    try {
        await axios.post("/api/v0/auth/password", { currentPassword: currentPassword.value, newPassword: newPassword.value });
        passwordMessage.value = "Password changed.";
        currentPassword.value = "";
        newPassword.value = "";
        confirmPassword.value = "";
    } catch (failure) {
        passwordMessage.value = describe(failure, "Could not change password.");
        passwordError.value = true;
    } finally {
        busy.value = false;
    }
}

async function addWatch() {
    await run(async () => {
        const response = await axios.post<Watch[]>("/api/v0/admin/watches", newWatch.value);
        if (status.value) {
            status.value.watches = response.data;
        }
        report(`Added ${newWatch.value.callsign.toUpperCase()}.`);
        newWatch.value = { callsign: "", grid: "", slackUserId: "" };
    }, "Could not add the callsign.");
}

async function saveWatch() {
    const edit = editing.value;
    if (!edit) {
        return;
    }
    await run(async () => {
        const response = await axios.put<Watch[]>(`/api/v0/admin/watches/${edit.callsign}`, edit);
        if (status.value) {
            status.value.watches = response.data;
        }
        report(`Updated ${edit.callsign}.`);
        editing.value = null;
    }, "Could not update the callsign.");
}

async function removeWatch(callsign: string) {
    if (!window.confirm(`Stop watching ${callsign}?`)) {
        return;
    }
    await run(async () => {
        const response = await axios.delete<Watch[]>(`/api/v0/admin/watches/${callsign}`);
        if (status.value) {
            status.value.watches = response.data;
        }
        report(`Removed ${callsign}.`);
    }, "Could not remove the callsign.");
}

async function createInvite() {
    await run(async () => {
        const response = await axios.post<{ result: string }>("/api/v0/admin/invites", { email: inviteEmail.value });
        report(response.data.result === "AlreadyInvited" ? `${inviteEmail.value} was already invited.` : `Invited ${inviteEmail.value}.`);
        inviteEmail.value = "";
        await loadInvites();
    }, "Could not send the invite.");
}

async function decideInvite(invite: Invite, approve: boolean) {
    if (!approve && !window.confirm(`Decline ${invite.email}?`)) {
        return;
    }
    await run(async () => {
        await axios.post(`/api/v0/admin/invites/${invite.id}/${approve ? "approve" : "decline"}`);
        report(`${approve ? "Approved" : "Declined"} ${invite.email}.`);
        await loadInvites();
    }, "Could not update the invite.");
}

async function addUser() {
    await run(async () => {
        const response = await axios.post<Account[]>("/api/v0/admin/users", newUser.value);
        users.value = response.data;
        report(`Added ${newUser.value.email}.`);
        newUser.value = { email: "", password: "" };
    }, "Could not add the user.");
}

async function resetPassword() {
    const reset = resetting.value;
    if (!reset) {
        return;
    }
    await run(async () => {
        await axios.post(`/api/v0/admin/users/${reset.id}/password`, { newPassword: reset.password });
        report("Password reset.");
        resetting.value = null;
    }, "Could not reset the password.");
}

async function deleteUser(account: Account) {
    if (!window.confirm(`Delete ${account.email}?`)) {
        return;
    }
    await run(async () => {
        const response = await axios.delete<Account[]>(`/api/v0/admin/users/${account.id}`);
        users.value = response.data;
        report(`Deleted ${account.email}.`);
    }, "Could not delete the user.");
}

async function logout() {
    stopAutoRefresh();
    await axios.post("/api/v0/auth/logout");
    user.value = null;
    status.value = null;
    detail.value = null;
}

onMounted(async () => {
    try {
        const response = await axios.get<User>("/api/v0/auth/me");
        user.value = response.data;
        await refresh();
        startAutoRefresh();
    } catch {
        user.value = null;
    }
});

onUnmounted(stopAutoRefresh);
</script>

<style lang="scss" scoped>
.admin {
    font-family: system-ui, sans-serif;
    max-width: 1200px;
    margin: 0 auto;
    padding: 1rem;

    header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        flex-wrap: wrap;

        nav {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            flex-wrap: wrap;
        }
    }

    h1 {
        font-size: 1.4rem;
    }

    h2 {
        font-size: 1.1rem;
        margin: 1.5rem 0 0.5rem;
    }

    table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.9rem;

        th,
        td {
            text-align: left;
            padding: 0.35rem 0.5rem;
            border-bottom: 1px solid #ddd;
            white-space: nowrap;
        }

        td.detail {
            white-space: normal;
            background: #f7f7f7;
            padding: 0.75rem;
        }
    }

    tr.clickable {
        cursor: pointer;

        &:hover,
        &.selected {
            background: #f0f4ff;
        }
    }

    .stack {
        max-width: 320px;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        margin-top: 1.5rem;

        label {
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }
    }

    .row {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-bottom: 0.75rem;
    }

    .actions {
        display: flex;
        gap: 0.35rem;
    }

    .muted {
        color: #666;
        font-size: 0.85rem;
    }

    .ok {
        color: #1a7f37;
    }

    .bad {
        color: #b3261e;
    }

    .warn {
        color: #9a6700;
    }

    tr.pending {
        background: #fffbe6;
    }
}
</style>
