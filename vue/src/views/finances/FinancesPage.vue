<template>
    <PublicPage title="Finances" :loading="loading" :failed="failed">
        <section class="card hero">
            <div>
                <div class="muted label">Cash on hand</div>
                <div class="hero-figure">{{ money(cashOnHand) }}</div>
                <div v-if="asOf" class="muted small">as of {{ longDate(asOf) }}</div>
            </div>
            <ul class="accounts">
                <li v-for="account in accounts" :key="account.name">
                    <span class="muted">{{ account.name }}</span>
                    <span class="figure-small">{{ money(account.balance) }}</span>
                </li>
            </ul>
        </section>

        <div class="charts">
            <section class="card pad">
                <h2>Money in and out by year</h2>
                <InOutColumns :groups="yearly" />
            </section>
            <section class="card pad">
                <h2>Balance over time</h2>
                <BalanceLine :points="monthlyBalance" />
            </section>
        </div>

        <nav class="years" aria-label="Period">
            <button
                v-for="option in yearOptions"
                :key="option.label"
                type="button"
                :class="{ active: year === option.value }"
                :aria-pressed="year === option.value"
                @click="year = option.value"
            >
                {{ option.label }}
            </button>
        </nav>

        <div class="tiles">
            <div class="card tile">
                <div class="muted label">Starting balance</div>
                <div class="figure">{{ money(period.starting) }}</div>
            </div>
            <div class="card tile">
                <div class="muted label">Money in</div>
                <div class="figure">{{ money(period.in) }}</div>
            </div>
            <div class="card tile">
                <div class="muted label">Money out</div>
                <div class="figure">{{ money(period.out) }}</div>
            </div>
            <div class="card tile">
                <div class="muted label">Ending balance</div>
                <div class="figure">{{ money(period.ending) }}</div>
            </div>
        </div>

        <div class="charts">
            <section class="card pad">
                <h2>Where it came from</h2>
                <CategoryBars :items="sources" series="in" />
            </section>
            <section class="card pad">
                <h2>Where it went</h2>
                <CategoryBars :items="spending" series="out" />
            </section>
        </div>

        <section class="card donations">
            <h2 class="pad table-title">Donations</h2>
            <p class="pad rule muted">
                Donors are named for a one-time gift over $50, more than $600 in a year, or on request, per our
                <a href="https://www.devanooga.com/code-of-conduct/#privacy-donations">code of conduct</a>.
            </p>
            <table>
                <thead>
                    <tr>
                        <th>Year</th>
                        <th class="amount">Gifts</th>
                        <th class="amount">One-time / monthly</th>
                        <th class="amount">Given</th>
                        <th class="amount">In-kind</th>
                        <th class="amount">Fees</th>
                        <th class="amount">Received</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="row in donationYears" :key="row.year">
                        <td>{{ row.year }}</td>
                        <td class="amount">{{ row.count }}</td>
                        <td class="amount">{{ row.oneTime }} / {{ row.monthly }}</td>
                        <td class="amount">{{ money(row.given) }}</td>
                        <td class="amount">{{ money(row.inKind) }}</td>
                        <td class="amount">{{ money(row.fees) }}</td>
                        <td class="amount">{{ money(row.received) }}</td>
                    </tr>
                    <tr v-if="donationYears.length === 0">
                        <td colspan="7" class="muted">No donations in this period.</td>
                    </tr>
                </tbody>
            </table>
            <template v-if="namedDonations.length">
                <h3 class="pad named-title">Named donors</h3>
                <table class="named">
                    <tbody>
                        <tr v-for="donation in namedDonations" :key="donation.id">
                            <td class="date">{{ donation.date }}</td>
                            <td>
                                <div>{{ donation.donor }}</div>
                                <div class="muted small">
                                    {{ [donation.inKind ? "In-kind" : "", donation.note, donation.namedBecause].filter(Boolean).join(" · ") }}
                                </div>
                            </td>
                            <td class="amount">{{ money(donation.amount) }}</td>
                        </tr>
                    </tbody>
                </table>
            </template>
        </section>

        <section class="card">
            <h2 class="pad table-title">Transactions</h2>
            <table>
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Description</th>
                        <th>Category</th>
                        <th class="amount">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="transaction in periodTransactions" :key="transaction.id" :class="{ transfer: transaction.kind === 'Transfer' }">
                        <td class="date">{{ transaction.date }}</td>
                        <td>
                            <div>{{ transaction.payee ?? transaction.description }}</div>
                            <div v-if="transaction.payee && transaction.description" class="muted small">
                                {{ transaction.description }}
                            </div>
                        </td>
                        <td class="muted">
                            <div>{{ transaction.kind === "Transfer" ? "Transfer between our accounts" : transaction.category }}</div>
                            <div class="small">{{ transaction.account }}</div>
                        </td>
                        <td class="amount">{{ money(transaction.amount) }}</td>
                    </tr>
                    <tr v-if="periodTransactions.length === 0">
                        <td colspan="4" class="muted">Nothing recorded for this period.</td>
                    </tr>
                </tbody>
            </table>
        </section>
    </PublicPage>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import axios from "axios";
import PublicPage from "@/components/PublicPage.vue";
import InOutColumns from "@/components/charts/InOutColumns.vue";
import BalanceLine from "@/components/charts/BalanceLine.vue";
import CategoryBars from "@/components/charts/CategoryBars.vue";
import { money } from "@/components/charts/scale";

interface Account {
    name: string;
    openingDate: string;
    openingBalance: number;
    balance: number;
    asOf: string | null;
}

interface Donation {
    id: string;
    date: string;
    donor: string;
    namedBecause: string | null;
    amount: number;
    fee: number;
    net: number;
    recurring: boolean;
    inKind: boolean;
    source: string;
    note: string | null;
}

interface Transaction {
    id: string;
    date: string;
    account: string;
    kind: "Income" | "Expense" | "Transfer";
    payee: string | null;
    description: string | null;
    category: string;
    amount: number;
}

const accounts = ref<Account[]>([]);
const transactions = ref<Transaction[]>([]);
const donations = ref<Donation[]>([]);
const year = ref<string | null>(null);
const loading = ref(true);
const failed = ref(false);

const monthNames = new Intl.DateTimeFormat("en-US", { month: "short", year: "numeric", timeZone: "UTC" });
const longFormat = new Intl.DateTimeFormat("en-US", { dateStyle: "long", timeZone: "UTC" });

const openingTotal = computed(() => accounts.value.reduce((sum, account) => sum + account.openingBalance, 0));
const cashOnHand = computed(() => accounts.value.reduce((sum, account) => sum + account.balance, 0));
const asOf = computed(() => transactions.value[0]?.date ?? null);
const firstYear = computed(() =>
    accounts.value.length ? Math.min(...accounts.value.map((account) => Number(account.openingDate.slice(0, 4)))) : new Date().getFullYear(),
);
const years = computed(() => {
    const lastYear = asOf.value ? Number(asOf.value.slice(0, 4)) : firstYear.value;
    return Array.from({ length: lastYear - firstYear.value + 1 }, (_, i) => String(firstYear.value + i));
});
const yearOptions = computed(() => [
    { label: "All time", value: null },
    ...[...years.value].reverse().map((value) => ({ label: value, value })),
]);

const yearly = computed(() =>
    years.value.map((label) => {
        const within = transactions.value.filter((t) => t.date.startsWith(label));
        return { label, income: incomeOf(within), expenses: expensesOf(within) };
    }),
);

const monthlyBalance = computed(() => {
    if (!accounts.value.length) {
        return [];
    }
    const start = new Date(Date.UTC(firstYear.value, 0, 1));
    const end = asOf.value ? new Date(`${asOf.value}T00:00:00Z`) : start;
    const byMonth = new Map<string, number>();
    for (const t of transactions.value) {
        const month = t.date.slice(0, 7);
        byMonth.set(month, (byMonth.get(month) ?? 0) + t.amount);
    }
    const points = [];
    let balance = openingTotal.value;
    for (const cursor = new Date(start); cursor <= end; cursor.setUTCMonth(cursor.getUTCMonth() + 1)) {
        const month = cursor.toISOString().slice(0, 7);
        balance += byMonth.get(month) ?? 0;
        points.push({ label: monthNames.format(cursor), year: month.slice(0, 4), value: Math.round(balance * 100) / 100 });
    }
    return points;
});

const periodTransactions = computed(() =>
    year.value === null ? transactions.value : transactions.value.filter((t) => t.date.startsWith(year.value as string)),
);

const period = computed(() => {
    const before = year.value === null ? [] : transactions.value.filter((t) => t.date < `${year.value}-01-01`);
    const starting = openingTotal.value + before.reduce((sum, t) => sum + t.amount, 0);
    const net = periodTransactions.value.reduce((sum, t) => sum + t.amount, 0);
    return {
        starting,
        in: incomeOf(periodTransactions.value),
        out: expensesOf(periodTransactions.value),
        ending: starting + net,
    };
});

const periodDonations = computed(() =>
    year.value === null ? donations.value : donations.value.filter((d) => d.date.startsWith(year.value as string)),
);

const donationYears = computed(() => {
    const byYear = new Map<string, Donation[]>();
    for (const d of periodDonations.value) {
        byYear.set(d.date.slice(0, 4), [...(byYear.get(d.date.slice(0, 4)) ?? []), d]);
    }
    return [...byYear.entries()]
        .sort(([a], [b]) => b.localeCompare(a))
        .map(([label, list]) => ({
            year: label,
            count: list.length,
            oneTime: list.filter((d) => !d.recurring).length,
            monthly: list.filter((d) => d.recurring).length,
            given: list.filter((d) => !d.inKind).reduce((sum, d) => sum + d.amount, 0),
            inKind: list.filter((d) => d.inKind).reduce((sum, d) => sum + d.amount, 0),
            fees: list.reduce((sum, d) => sum + d.fee, 0),
            received: list.reduce((sum, d) => sum + d.net, 0),
        }));
});

const namedDonations = computed(() => periodDonations.value.filter((d) => d.namedBecause));

const sources = computed(() =>
    rollUp(
        periodTransactions.value.filter((t) => t.kind === "Income"),
        (t) => t.payee ?? t.category,
    ),
);
const spending = computed(() =>
    rollUp(
        periodTransactions.value.filter((t) => t.kind === "Expense"),
        (t) => t.category,
    ),
);

function incomeOf(list: Transaction[]): number {
    return list.filter((t) => t.kind === "Income").reduce((sum, t) => sum + t.amount, 0);
}

function expensesOf(list: Transaction[]): number {
    return list.filter((t) => t.kind === "Expense").reduce((sum, t) => sum - t.amount, 0);
}

function rollUp(list: Transaction[], key: (t: Transaction) => string) {
    const totals = new Map<string, number>();
    for (const t of list) {
        totals.set(key(t), (totals.get(key(t)) ?? 0) + Math.abs(t.amount));
    }
    return [...totals.entries()]
        .map(([label, value]) => ({ label, value: Math.round(value * 100) / 100 }))
        .sort((a, b) => b.value - a.value);
}

function longDate(value: string): string {
    return longFormat.format(new Date(`${value}T00:00:00Z`));
}

onMounted(async () => {
    try {
        const data = (
            await axios.get<{ accounts: Account[]; transactions: Transaction[]; donations: Donation[] }>("/api/v0/finances")
        ).data;
        accounts.value = data.accounts;
        transactions.value = data.transactions;
        donations.value = data.donations;
    } catch {
        failed.value = true;
    } finally {
        loading.value = false;
    }
});
</script>

<style scoped>
h2 {
    font-size: 15px;
    font-weight: 600;
    margin: 0 0 12px;
}

.pad {
    padding: 16px;
}

.table-title {
    margin: 0;
    padding-bottom: 0;
}

.donations {
    margin-bottom: 16px;
}

.rule {
    margin: 0;
    padding-top: 4px;
    font-size: 13px;
}

.named .date {
    width: 1%;
    padding-right: 24px;
}

.named-title {
    font-size: 14px;
    font-weight: 600;
    margin: 0;
    padding-bottom: 0;
    border-top: 1px solid var(--border);
}

.hero {
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    align-items: flex-end;
    gap: 16px;
    padding: 20px;
    margin-bottom: 16px;
}

.hero-figure {
    font-size: 48px;
    font-weight: 700;
    line-height: 1.1;
}

.accounts {
    list-style: none;
    margin: 0;
    padding: 0;
    display: grid;
    gap: 4px;
    min-width: 240px;
}

.accounts li {
    display: flex;
    justify-content: space-between;
    gap: 16px;
}

.figure-small {
    font-variant-numeric: tabular-nums;
}

.charts {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 16px;
    margin-bottom: 16px;
}

.years {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin: 32px 0 16px;
}

.years button {
    font: inherit;
    font-size: 14px;
    color: var(--text);
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 999px;
    padding: 4px 14px;
    cursor: pointer;
}

.years button.active {
    background: var(--accent);
    border-color: var(--accent);
    color: #fff;
}

.tiles {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    margin-bottom: 16px;
}

.tile {
    padding: 16px;
}

.label,
.small {
    font-size: 13px;
}

.figure {
    font-size: 24px;
    font-weight: 700;
}

.date {
    white-space: nowrap;
}

.amount {
    text-align: right;
    white-space: nowrap;
    font-variant-numeric: tabular-nums;
}

.transfer td {
    color: var(--muted);
}

@media (max-width: 900px) {
    .charts {
        grid-template-columns: 1fr;
    }

    .tiles {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

@media (max-width: 720px) {
    .hero-figure {
        font-size: 40px;
    }

    .date {
        font-weight: 600;
    }

    .amount {
        text-align: left;
    }
}
</style>
