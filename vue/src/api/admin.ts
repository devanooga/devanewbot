import axios from "axios";

export interface CurrentUser {
    email: string;
    roles: string[];
}

export interface Feed {
    name: string;
    connected: boolean;
    connectedAt: string | null;
    lastSpotAt: string | null;
    lastError: string | null;
}

export interface Session {
    id: string;
    callsign: string;
    band: string;
    mode: string;
    grid: string | null;
    openedAt: string;
    lastHeardAt: string;
    closedAt: string | null;
    suppressed: boolean;
    spotCount: number;
    reporterCount: number;
    furthestKm: number | null;
    furthestReporter: string | null;
    bestSnr: number | null;
    bestSnrReporter: string | null;
}

export interface Spot {
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

export interface SessionDetail {
    id: string;
    callsign: string;
    band: string;
    mode: string;
    grid: string | null;
    openedAt: string;
    lastHeardAt: string;
    closedAt: string | null;
    slackChannelId: string | null;
    slackMessageTs: string | null;
    spots: Spot[];
}

export interface Watch {
    callsign: string;
    grid: string | null;
    slackUserId: string | null;
    addedBy: string;
    createdAt: string;
}

export interface Invite {
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
    decidedBySlackUserId: string | null;
    decisionSource: string | null;
    error: string | null;
}

export interface Account {
    id: string;
    email: string;
    createdAt: string;
    lockoutEnd: string | null;
    accessFailedCount: number;
}

export interface RecurringJob {
    id: string;
    cron: string;
    lastExecution: string | null;
    nextExecution: string | null;
    lastJobState: string | null;
    error: string | null;
}

export interface Status {
    problems: string[];
    feeds: Feed[];
    watches: Watch[];
    sessions: Session[];
    spots: { total: number; lastDay: number; bySource: { source: string; count: number }[] };
    jobs: {
        enqueued: number;
        processing: number;
        scheduled: number;
        succeeded: number;
        failed: number;
        recurring: RecurringJob[];
    };
}

export interface LogEntry {
    sequence: number;
    timestampUtc: string;
    level: string;
    category: string;
    message: string;
    exception: string | null;
}

export interface LogPage {
    entries: LogEntry[];
    newest: number;
    buffered: number;
    missed: number;
}

export const LogLevels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

export const InviteStatuses = ["Pending", "Approved", "Declined", "AlreadyInvited", "Failed"];

export function errorMessage(failure: unknown, fallback: string): string {
    const response = (failure as { response?: { data?: { errors?: string[] }; status?: number } }).response;
    return response?.data?.errors?.join(" ") ?? fallback;
}

export function isUnauthorized(failure: unknown): boolean {
    return (failure as { response?: { status?: number } }).response?.status === 401;
}

export const api = {
    me: () => axios.get<CurrentUser>("/api/v0/auth/me").then((r) => r.data),
    login: (email: string, password: string) =>
        axios.post<CurrentUser>("/api/v0/auth/login", { email, password }).then((r) => r.data),
    logout: () => axios.post("/api/v0/auth/logout"),
    changePassword: (currentPassword: string, newPassword: string) =>
        axios.post("/api/v0/auth/password", { currentPassword, newPassword }),

    status: () => axios.get<Status>("/api/v0/admin/status").then((r) => r.data),
    session: (id: string) => axios.get<SessionDetail>(`/api/v0/admin/sessions/${id}`).then((r) => r.data),
    reannounce: (id: string) => axios.post(`/api/v0/admin/sessions/${id}/reannounce`),

    addWatch: (watch: { callsign: string; grid: string; slackUserId: string }) =>
        axios.post<Watch[]>("/api/v0/admin/watches", watch).then((r) => r.data),
    updateWatch: (callsign: string, watch: { grid: string; slackUserId: string }) =>
        axios.put<Watch[]>(`/api/v0/admin/watches/${callsign}`, watch).then((r) => r.data),
    removeWatch: (callsign: string) =>
        axios.delete<Watch[]>(`/api/v0/admin/watches/${callsign}`).then((r) => r.data),

    invites: (status: string, search: string) =>
        axios
            .get<Invite[]>("/api/v0/admin/invites", {
                params: { status: status || undefined, search: search || undefined },
            })
            .then((r) => r.data),
    createInvite: (email: string) =>
        axios.post<{ result: string }>("/api/v0/admin/invites", { email }).then((r) => r.data),
    decideInvite: (id: string, approve: boolean) =>
        axios.post(`/api/v0/admin/invites/${id}/${approve ? "approve" : "decline"}`),

    logs: (afterSequence: number, level: string, search: string) =>
        axios
            .get<LogPage>("/api/v0/admin/logs", { params: { afterSequence, level, search: search || undefined } })
            .then((r) => r.data),

    users: () => axios.get<Account[]>("/api/v0/admin/users").then((r) => r.data),
    createUser: (email: string, password: string) =>
        axios.post<Account[]>("/api/v0/admin/users", { email, password }).then((r) => r.data),
    resetUserPassword: (id: string, newPassword: string) =>
        axios.post(`/api/v0/admin/users/${id}/password`, { newPassword }),
    deleteUser: (id: string) => axios.delete<Account[]>(`/api/v0/admin/users/${id}`).then((r) => r.data),
};
