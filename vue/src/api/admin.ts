import axios from "axios";

export interface CurrentUser {
    email: string;
    roles: string[];
    hasPassword: boolean;
    slack: { userId: string; name: string | null } | null;
}

export interface ChannelBan {
    id: string;
    userId: string;
    channelId: string;
    reason: string;
    bannedBy: string;
    bannedAt: string;
    expiresAt: string | null;
    liftedEarly: boolean;
    active: boolean;
}

export interface ModerationAction {
    id: string;
    occurredAt: string;
    kind: string;
    source: string;
    action: string;
    reason: string;
    administrator: string;
    administratorSlackUserId: string | null;
    targetSlackUserId: string | null;
    channelId: string | null;
    removedMessageText: string | null;
    hiddenAt: string | null;
    hiddenBy: string | null;
}

export interface ModerationActionInput {
    action: string;
    reason: string;
    occurredAt: string | null;
}

export interface AuthoredMessage {
    channelId: string;
    channel: string;
    ts: string;
    postedAt: string;
    text: string;
    permalink: string | null;
}

export interface RecentMessages {
    author: string;
    authorIsAdmin: boolean;
    messages: AuthoredMessage[];
}

export interface Removal {
    userId: string;
    messages: { channelId: string; ts: string; text: string }[];
    reason: string;
    deactivate: boolean;
}

export interface RemovalResult {
    removed: number;
    deactivated: boolean;
    failures: string[];
}

export type FinanceKind = "Income" | "Expense" | "Transfer";

export interface FinanceTransaction {
    id: string;
    externalKey: string;
    date: string;
    account: string;
    kind: FinanceKind;
    payee: string | null;
    description: string | null;
    category: string;
    amount: number;
    hiddenAt: string | null;
    hiddenBy: string | null;
}

export interface FinanceAccount {
    id: string;
    name: string;
    openingDate: string;
    openingBalance: number;
}

export interface FinancePayee {
    id: string;
    name: string;
    isPublic: boolean;
}

export interface FinanceData {
    accounts: FinanceAccount[];
    payees: FinancePayee[];
    transactions: FinanceTransaction[];
}

export interface FinanceImportRow {
    key: string;
    status: "New" | "Changed" | "Unchanged";
    date: string;
    account: string;
    kind: FinanceKind;
    payee: string | null;
    description: string | null;
    category: string;
    amount: number;
    changes: string[];
}

export interface FinanceImportPreview {
    from: string | null;
    to: string | null;
    rows: FinanceImportRow[];
    missing: FinanceTransaction[];
    skippedAccounts: string[];
    skippedBeforeOpening: number;
}

export const ModerationKinds = ["RemovedMessage", "Deactivated", "ChannelBan", "ChannelBanLifted", "Other"];

export interface SlackPerson {
    id: string;
    handle: string;
    name: string;
}

export interface SlackChannel {
    id: string;
    name: string;
}

export interface NewBan {
    userId: string;
    channelId: string;
    reason: string;
    expiresOn: string | null;
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
    state: string;
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

export interface InviteDetail extends Invite {
    locationJson: string | null;
    slackChannelId: string | null;
    slackMessageTs: string | null;
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
    providers: () => axios.get<{ slack: boolean }>("/api/v0/auth/providers").then((r) => r.data),
    unlinkSlack: () => axios.delete<CurrentUser>("/api/v0/auth/slack").then((r) => r.data),
    bans: () => axios.get<ChannelBan[]>("/api/v0/admin/bans").then((r) => r.data),
    slackDirectory: () =>
        axios
            .get<{ people: SlackPerson[]; channels: SlackChannel[] }>("/api/v0/admin/bans/directory")
            .then((r) => r.data),
    createBan: (ban: NewBan) => axios.post<ChannelBan[]>("/api/v0/admin/bans", ban).then((r) => r.data),
    liftBan: (id: string) => axios.post<ChannelBan[]>(`/api/v0/admin/bans/${id}/lift`).then((r) => r.data),
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
    invite: (id: string) => axios.get<InviteDetail>(`/api/v0/admin/invites/${id}`).then((r) => r.data),
    createInvite: (email: string) =>
        axios.post<{ result: string }>("/api/v0/admin/invites", { email }).then((r) => r.data),
    decideInvite: (id: string, approve: boolean) =>
        axios.post(`/api/v0/admin/invites/${id}/${approve ? "approve" : "decline"}`),

    logs: (afterSequence: number, level: string, search: string) =>
        axios
            .get<LogPage>("/api/v0/admin/logs", { params: { afterSequence, level, search: search || undefined } })
            .then((r) => r.data),

    moderation: (kind: string, search: string) =>
        axios
            .get<ModerationAction[]>("/api/v0/admin/moderation", {
                params: { kind: kind || undefined, search: search || undefined },
            })
            .then((r) => r.data),
    createModeration: (input: ModerationActionInput) =>
        axios.post<ModerationAction[]>("/api/v0/admin/moderation", input).then((r) => r.data),
    updateModeration: (id: string, input: ModerationActionInput) =>
        axios.put<ModerationAction[]>(`/api/v0/admin/moderation/${id}`, input).then((r) => r.data),
    recentMessages: (userId: string, hours: number) =>
        axios
            .get<RecentMessages>("/api/v0/admin/moderation/messages", { params: { userId, hours } })
            .then((r) => r.data),
    removeMessages: (removal: Removal) =>
        axios.post<RemovalResult>("/api/v0/admin/moderation/removals", removal).then((r) => r.data),
    hideModeration: (id: string, hidden: boolean) =>
        axios
            .post<ModerationAction[]>(`/api/v0/admin/moderation/${id}/${hidden ? "hide" : "unhide"}`)
            .then((r) => r.data),

    finances: () => axios.get<FinanceData>("/api/v0/admin/finances").then((r) => r.data),
    previewFinances: (csv: string) =>
        axios.post<FinanceImportPreview>("/api/v0/admin/finances/preview", { csv }).then((r) => r.data),
    applyFinances: (csv: string, applyKeys: string[], hideIds: string[]) =>
        axios
            .post<{ added: number; updated: number; hidden: number }>("/api/v0/admin/finances/apply", {
                csv,
                applyKeys,
                hideIds,
            })
            .then((r) => r.data),
    addFinanceAccount: (account: { name: string; openingDate: string; openingBalance: number }) =>
        axios.post<FinanceData>("/api/v0/admin/finances/accounts", account).then((r) => r.data),
    updateFinanceAccount: (id: string, account: { openingDate: string; openingBalance: number }) =>
        axios.put<FinanceData>(`/api/v0/admin/finances/accounts/${id}`, account).then((r) => r.data),
    setPayeePublic: (id: string, isPublic: boolean) =>
        axios.put<FinanceData>(`/api/v0/admin/finances/payees/${id}`, { isPublic }).then((r) => r.data),
    hideFinance: (id: string, hidden: boolean) =>
        axios
            .post<FinanceData>(`/api/v0/admin/finances/${id}/${hidden ? "hide" : "unhide"}`)
            .then((r) => r.data),

    users: () => axios.get<Account[]>("/api/v0/admin/users").then((r) => r.data),
    createUser: (email: string, password: string) =>
        axios.post<Account[]>("/api/v0/admin/users", { email, password }).then((r) => r.data),
    resetUserPassword: (id: string, newPassword: string) =>
        axios.post(`/api/v0/admin/users/${id}/password`, { newPassword }),
    deleteUser: (id: string) => axios.delete<Account[]>(`/api/v0/admin/users/${id}`).then((r) => r.data),
};
