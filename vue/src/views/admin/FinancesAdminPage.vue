<template>
    <v-card border>
        <v-tabs v-model="tab" color="primary">
            <v-tab value="import" prepend-icon="mdi-file-upload">QuickBooks import</v-tab>
            <v-tab value="ledger" prepend-icon="mdi-format-list-bulleted">Transactions</v-tab>
            <v-tab value="donations" prepend-icon="mdi-hand-heart">Donations</v-tab>
            <v-tab value="setup" prepend-icon="mdi-bank">Accounts &amp; donors</v-tab>
            <v-spacer />
            <v-btn variant="text" class="align-self-center mr-2" href="/finances" target="_blank" append-icon="mdi-open-in-new">
                Public page
            </v-btn>
        </v-tabs>
        <v-divider />

        <v-window v-model="tab">
            <v-window-item value="import">
                <v-card-text>
                    <p class="text-medium-emphasis mb-4">
                        In QuickBooks, run <strong>Reports → Accountant &amp; Taxes → Transaction Detail by Account</strong>
                        for any date range and export it as CSV. Only
                        {{ data.accounts.map((a) => a.name).join(" and ") || "tracked accounts" }} are read.
                    </p>
                    <v-btn color="primary" prepend-icon="mdi-file-delimited" :loading="ledgerImport.loading" @click="ledgerPicker?.click()">
                        Choose CSV
                    </v-btn>
                    <span v-if="ledgerImport.fileName" class="ml-3 text-medium-emphasis">{{ ledgerImport.fileName }}</span>
                    <input ref="ledgerPicker" type="file" accept=".csv,text/csv" hidden @change="previewLedger" />
                </v-card-text>
                <ImportDiff
                    v-if="ledgerPreview"
                    v-model:selected="ledgerImport.selected"
                    v-model:hidden="ledgerImport.hidden"
                    :rows="ledgerRows"
                    :missing="ledgerMissing"
                    source="QuickBooks"
                    :note="ledgerNote"
                    :applying="ledgerImport.applying"
                    @apply="applyLedger"
                    @discard="resetLedger"
                />
            </v-window-item>

            <v-window-item value="ledger">
                <v-card-title class="d-flex align-center flex-wrap ga-3 text-body-1">
                    <v-text-field v-model="search" label="Search" prepend-inner-icon="mdi-magnify" style="max-width: 280px" />
                    <v-spacer />
                    <v-switch v-model="showHidden" label="Show hidden" color="primary" density="compact" hide-details />
                </v-card-title>
                <v-data-table
                    :headers="ledgerHeaders"
                    :items="ledger"
                    :search="search"
                    :loading="loading"
                    :row-props="rowProps"
                    item-value="id"
                    :items-per-page="50"
                >
                    <template #[`item.payee`]="{ item }">
                        <div>{{ item.payee ?? item.description }}</div>
                        <div v-if="item.payee && item.description" class="text-caption text-medium-emphasis">{{ item.description }}</div>
                        <div v-if="item.hiddenAt" class="text-caption text-warning">Hidden by {{ item.hiddenBy }} on {{ when(item.hiddenAt) }}</div>
                    </template>
                    <template #[`item.category`]="{ item }">
                        <div>{{ item.category }}</div>
                        <div class="text-caption text-medium-emphasis">{{ item.account }}</div>
                    </template>
                    <template #[`item.amount`]="{ item }">
                        <span class="text-no-wrap">{{ money(item.amount) }}</span>
                    </template>
                    <template #[`item.actions`]="{ item }">
                        <v-btn
                            size="small"
                            variant="text"
                            :icon="item.hiddenAt ? 'mdi-eye' : 'mdi-eye-off'"
                            :aria-label="item.hiddenAt ? 'Unhide' : 'Hide'"
                            v-tooltip="item.hiddenAt ? 'Show on the public page again' : 'Hide from the public page'"
                            @click="setHidden(item, !item.hiddenAt)"
                        />
                    </template>
                    <template #no-data>
                        <div class="py-8 text-center text-medium-emphasis">Nothing imported yet.</div>
                    </template>
                </v-data-table>
            </v-window-item>

            <v-window-item value="donations">
                <v-card-text>
                    <p class="text-medium-emphasis mb-4">
                        Upload a Donorbox donations export, or the direct-donations file for gifts that didn't go through
                        Donorbox (Id, Date, Donor, Amount, Type as Direct or In-kind, Public, Note). Emails, addresses and
                        card details are never stored. The public page names donors for a one-time gift over $50, more
                        than $600 in a year, or when Public is yes.
                    </p>
                    <v-btn color="primary" prepend-icon="mdi-file-delimited" :loading="donationImport.loading" @click="donationPicker?.click()">
                        Choose CSV
                    </v-btn>
                    <v-btn variant="text" class="ml-2" prepend-icon="mdi-download" href="/api/v0/admin/finances/donations/direct.csv">
                        Direct-donations file
                    </v-btn>
                    <span v-if="donationImport.fileName" class="ml-3 text-medium-emphasis">{{ donationImport.fileName }}</span>
                    <input ref="donationPicker" type="file" accept=".csv,text/csv" hidden @change="previewDonations" />
                </v-card-text>
                <ImportDiff
                    v-if="donationPreview"
                    v-model:selected="donationImport.selected"
                    v-model:hidden="donationImport.hidden"
                    :rows="donationRows"
                    :missing="donationMissing"
                    source="Donorbox"
                    :applying="donationImport.applying"
                    @apply="applyDonations"
                    @discard="resetDonations"
                />
                <v-divider />
                <v-data-table
                    :headers="donationHeaders"
                    :items="data.donations"
                    :loading="loading"
                    :row-props="rowProps"
                    item-value="id"
                    :items-per-page="50"
                >
                    <template #[`item.donatedAt`]="{ item }">{{ item.donatedAt.slice(0, 10) }}</template>
                    <template #[`item.donor`]="{ item }">
                        <div>{{ item.donor }}</div>
                        <div class="text-caption text-medium-emphasis">
                            {{ item.inKind ? "In-kind" : item.recurring ? "Monthly" : "One-time" }} · {{ item.source }}
                            <span v-if="item.note"> · {{ item.note }}</span>
                            <span v-if="item.anonymousRequested"> · asked to be anonymous</span>
                        </div>
                    </template>
                    <template #[`item.namedBecause`]="{ item }">
                        <v-chip v-if="item.namedBecause" size="small" color="primary" variant="tonal">{{ item.namedBecause }}</v-chip>
                        <span v-else class="text-medium-emphasis">Individual donor</span>
                    </template>
                    <template #[`item.amount`]="{ item }">
                        <div class="text-no-wrap">{{ money(item.amount) }}</div>
                        <div v-if="item.fee" class="text-caption text-medium-emphasis text-no-wrap">{{ money(item.fee) }} fee</div>
                    </template>
                    <template #[`item.actions`]="{ item }">
                        <div class="d-flex ga-1 justify-end align-center">
                            <v-switch
                                :model-value="item.namedOnRequest"
                                color="primary"
                                density="compact"
                                hide-details
                                aria-label="Named at the donor's request"
                                v-tooltip="'Named at the donor\'s request'"
                                @update:model-value="setNamed(item, Boolean($event))"
                            />
                            <v-btn
                                size="small"
                                variant="text"
                                :icon="item.hiddenAt ? 'mdi-eye' : 'mdi-eye-off'"
                                :aria-label="item.hiddenAt ? 'Unhide' : 'Hide'"
                                @click="setDonationHidden(item, !item.hiddenAt)"
                            />
                        </div>
                    </template>
                    <template #no-data>
                        <div class="py-8 text-center text-medium-emphasis">No donations imported yet.</div>
                    </template>
                </v-data-table>
            </v-window-item>

            <v-window-item value="setup">
                <v-card-text>
                    <div class="text-body-1 mb-1">Tracked accounts</div>
                    <div class="text-caption text-medium-emphasis mb-3">
                        Names must match QuickBooks. The public ledger starts at each opening date, from the opening balance.
                    </div>
                    <v-table density="comfortable">
                        <thead>
                            <tr>
                                <th>Account</th>
                                <th>Opening date</th>
                                <th>Opening balance</th>
                                <th />
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="account in data.accounts" :key="account.id">
                                <td>{{ account.name }}</td>
                                <td>{{ account.openingDate }}</td>
                                <td>{{ money(account.openingBalance) }}</td>
                                <td class="text-right">
                                    <v-btn size="small" variant="text" icon="mdi-pencil" aria-label="Edit" @click="editAccount(account)" />
                                </td>
                            </tr>
                        </tbody>
                    </v-table>
                    <v-btn class="mt-2" variant="tonal" prepend-icon="mdi-plus" @click="editAccount(null)">Track another account</v-btn>
                </v-card-text>

                <v-divider />

                <v-card-text>
                    <div class="text-body-1 mb-1">Names on deposits</div>
                    <div class="text-caption text-medium-emphasis mb-3">
                        QuickBooks deposit names not switched on here show publicly as "Individual donor".
                    </div>
                    <v-switch
                        v-for="payee in data.payees"
                        :key="payee.id"
                        :model-value="payee.isPublic"
                        :label="payee.name"
                        color="primary"
                        density="compact"
                        hide-details
                        @update:model-value="setPublic(payee, Boolean($event))"
                    />
                </v-card-text>
            </v-window-item>
        </v-window>
    </v-card>

    <v-dialog v-model="accountDialog" max-width="480">
        <v-card>
            <v-card-title>{{ accountForm.id ? accountForm.name : "Track an account" }}</v-card-title>
            <v-card-text class="d-flex flex-column ga-4">
                <v-text-field v-if="!accountForm.id" v-model="accountForm.name" label="QuickBooks account name" />
                <v-text-field v-model="accountForm.openingDate" label="Opening date" type="date" />
                <v-text-field v-model.number="accountForm.openingBalance" label="Opening balance" type="number" prefix="$" />
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="accountDialog = false">Cancel</v-btn>
                <v-btn color="primary" :loading="busy" :disabled="!accountForm.name.trim() || !accountForm.openingDate" @click="saveAccount">
                    Save
                </v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import {
    AdminDonation,
    api,
    DonationImportPreview,
    errorMessage,
    FinanceAccount,
    FinanceData,
    FinanceImportPreview,
    FinancePayee,
    FinanceTransaction,
} from "@/api/admin";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";
import { money } from "@/components/charts/scale";
import ImportDiff, { DiffRow } from "@/components/ImportDiff.vue";

const { notify } = useNotice();

const ledgerHeaders = [
    { title: "Date", key: "date", nowrap: true },
    { title: "Description", key: "payee", minWidth: "220px" },
    { title: "Category", key: "category" },
    { title: "Amount", key: "amount", align: "end" as const },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const donationHeaders = [
    { title: "Date", key: "donatedAt", nowrap: true },
    { title: "Donor", key: "donor", minWidth: "200px" },
    { title: "Public as", key: "namedBecause", sortable: false },
    { title: "Amount", key: "amount", align: "end" as const },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const tab = ref("import");
const data = ref<FinanceData>({ accounts: [], payees: [], transactions: [], donations: [] });
const loading = ref(false);
const busy = ref(false);
const search = ref("");
const showHidden = ref(false);
const ledgerPicker = ref<HTMLInputElement | null>(null);
const donationPicker = ref<HTMLInputElement | null>(null);
const ledgerPreview = ref<FinanceImportPreview | null>(null);
const donationPreview = ref<DonationImportPreview | null>(null);
const ledgerImport = reactive(emptyImport());
const donationImport = reactive(emptyImport());
const accountDialog = ref(false);
const accountForm = reactive({ id: "", name: "", openingDate: "", openingBalance: 0 });

const ledger = computed(() =>
    showHidden.value ? data.value.transactions : data.value.transactions.filter((t) => !t.hiddenAt),
);

const ledgerRows = computed<DiffRow[]>(
    () =>
        ledgerPreview.value?.rows.map((row) => ({
            key: row.key,
            status: row.status,
            changes: row.changes,
            date: row.date,
            title: row.payee ?? row.description ?? row.category,
            subtitle: `${row.category} · ${row.account}`,
            amount: row.amount,
        })) ?? [],
);
const ledgerMissing = computed(
    () =>
        ledgerPreview.value?.missing.map((t) => ({
            id: t.id,
            label: `${t.date} · ${t.payee ?? t.description} · ${money(t.amount)} · ${t.account}`,
        })) ?? [],
);
const ledgerNote = computed(() => {
    const preview = ledgerPreview.value;
    if (!preview) {
        return "";
    }
    return [
        preview.from ? `Covers ${preview.from} to ${preview.to}.` : "",
        preview.skippedBeforeOpening ? `${preview.skippedBeforeOpening} lines before the opening date skipped.` : "",
        preview.skippedAccounts.length ? `Not tracked: ${preview.skippedAccounts.join(", ")}.` : "",
    ]
        .filter(Boolean)
        .join(" ");
});

const donationRows = computed<DiffRow[]>(
    () =>
        donationPreview.value?.rows.map((row) => ({
            key: row.key,
            status: row.status,
            changes: row.changes,
            date: row.donatedAt.slice(0, 10),
            title: row.donor,
            subtitle: [
                row.inKind ? "In-kind" : row.recurring ? "Monthly" : "One-time",
                row.source,
                row.fee ? `${money(row.fee)} fee` : "",
                row.namedOnRequest ? "public" : "",
                row.note ?? "",
            ]
                .filter(Boolean)
                .join(" · "),
            amount: row.amount,
        })) ?? [],
);
const donationMissing = computed(
    () =>
        donationPreview.value?.missing.map((d) => ({
            id: d.id,
            label: `${d.donatedAt.slice(0, 10)} · ${d.donor} · ${money(d.amount)}`,
        })) ?? [],
);

function emptyImport() {
    return { csv: "", fileName: "", loading: false, applying: false, selected: [] as string[], hidden: [] as string[] };
}

function rowProps({ item }: { item: { hiddenAt: string | null } }) {
    return item.hiddenAt ? { class: "hidden-entry" } : {};
}

async function load() {
    loading.value = true;
    try {
        data.value = await api.finances();
    } catch (failure) {
        notify(errorMessage(failure, "Could not load the ledger."), "error");
    } finally {
        loading.value = false;
    }
}

async function readFile(event: Event, state: ReturnType<typeof emptyImport>): Promise<boolean> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = "";
    if (!file) {
        return false;
    }
    state.csv = await file.text();
    state.fileName = file.name;
    return true;
}

async function previewLedger(event: Event) {
    if (!(await readFile(event, ledgerImport))) {
        return;
    }
    ledgerImport.loading = true;
    try {
        ledgerPreview.value = await api.previewFinances(ledgerImport.csv);
        ledgerImport.selected = ledgerRows.value.filter((row) => row.status !== "Unchanged").map((row) => row.key);
        ledgerImport.hidden = [];
    } catch (failure) {
        notify(errorMessage(failure, `Could not read ${ledgerImport.fileName}.`), "error");
        resetLedger();
    } finally {
        ledgerImport.loading = false;
    }
}

async function applyLedger() {
    ledgerImport.applying = true;
    try {
        const result = await api.applyFinances(ledgerImport.csv, ledgerImport.selected, ledgerImport.hidden);
        notify(`Added ${result.added}, updated ${result.updated}, hid ${result.hidden}.`);
        resetLedger();
        await load();
    } catch (failure) {
        notify(errorMessage(failure, "Could not apply the import."), "error");
    } finally {
        ledgerImport.applying = false;
    }
}

function resetLedger() {
    ledgerPreview.value = null;
    Object.assign(ledgerImport, emptyImport());
}

async function previewDonations(event: Event) {
    if (!(await readFile(event, donationImport))) {
        return;
    }
    donationImport.loading = true;
    try {
        donationPreview.value = await api.previewDonations(donationImport.csv);
        donationImport.selected = donationRows.value.filter((row) => row.status !== "Unchanged").map((row) => row.key);
        donationImport.hidden = [];
    } catch (failure) {
        notify(errorMessage(failure, `Could not read ${donationImport.fileName}.`), "error");
        resetDonations();
    } finally {
        donationImport.loading = false;
    }
}

async function applyDonations() {
    donationImport.applying = true;
    try {
        const result = await api.applyDonations(donationImport.csv, donationImport.selected, donationImport.hidden);
        notify(`Added ${result.added}, updated ${result.updated}, hid ${result.hidden}.`);
        resetDonations();
        await load();
    } catch (failure) {
        notify(errorMessage(failure, "Could not apply the import."), "error");
    } finally {
        donationImport.applying = false;
    }
}

function resetDonations() {
    donationPreview.value = null;
    Object.assign(donationImport, emptyImport());
}

async function setHidden(transaction: FinanceTransaction, hidden: boolean) {
    try {
        data.value = await api.hideFinance(transaction.id, hidden);
    } catch (failure) {
        notify(errorMessage(failure, "Could not update it."), "error");
    }
}

async function setDonationHidden(donation: AdminDonation, hidden: boolean) {
    try {
        data.value = await api.hideDonation(donation.id, hidden);
    } catch (failure) {
        notify(errorMessage(failure, "Could not update it."), "error");
    }
}

async function setNamed(donation: AdminDonation, named: boolean) {
    try {
        data.value = await api.setDonationNamed(donation.id, named);
    } catch (failure) {
        notify(errorMessage(failure, "Could not update it."), "error");
    }
}

async function setPublic(payee: FinancePayee, isPublic: boolean) {
    try {
        data.value = await api.setPayeePublic(payee.id, isPublic);
    } catch (failure) {
        notify(errorMessage(failure, "Could not update that name."), "error");
    }
}

function editAccount(account: FinanceAccount | null) {
    Object.assign(accountForm, account ?? { id: "", name: "", openingDate: "", openingBalance: 0 });
    accountDialog.value = true;
}

async function saveAccount() {
    busy.value = true;
    try {
        const values = { openingDate: accountForm.openingDate, openingBalance: Number(accountForm.openingBalance) || 0 };
        data.value = accountForm.id
            ? await api.updateFinanceAccount(accountForm.id, values)
            : await api.addFinanceAccount({ name: accountForm.name.trim(), ...values });
        accountDialog.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not save the account."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
:deep(.hidden-entry) {
    opacity: 0.6;
}
</style>
