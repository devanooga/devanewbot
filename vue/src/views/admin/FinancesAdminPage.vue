<template>
    <v-card border>
        <v-tabs v-model="tab" color="primary">
            <v-tab value="import" prepend-icon="mdi-file-upload">Import</v-tab>
            <v-tab value="ledger" prepend-icon="mdi-format-list-bulleted">Transactions</v-tab>
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
                        for any date range, export it as a CSV file and pick it here. Only
                        {{ data.accounts.map((a) => a.name).join(" and ") || "tracked accounts" }} are read; nothing
                        changes until you apply.
                    </p>
                    <div class="d-flex ga-3 align-center flex-wrap">
                        <v-btn color="primary" prepend-icon="mdi-file-delimited" :loading="previewing" @click="picker?.click()">
                            Choose CSV
                        </v-btn>
                        <span v-if="fileName" class="text-medium-emphasis">{{ fileName }}</span>
                        <input ref="picker" type="file" accept=".csv,text/csv" hidden @change="pickFile" />
                    </div>
                </v-card-text>

                <template v-if="preview">
                    <v-card-text class="pt-0">
                        <div class="d-flex ga-2 flex-wrap mb-2">
                            <v-chip color="success" variant="tonal">{{ count("New") }} new</v-chip>
                            <v-chip color="warning" variant="tonal">{{ count("Changed") }} changed</v-chip>
                            <v-chip variant="tonal">{{ count("Unchanged") }} already up to date</v-chip>
                            <v-chip v-if="preview.missing.length" color="error" variant="tonal">
                                {{ preview.missing.length }} no longer in QuickBooks
                            </v-chip>
                        </div>
                        <div class="text-caption text-medium-emphasis">
                            <span v-if="preview.from">Covers {{ preview.from }} to {{ preview.to }}.</span>
                            <span v-if="preview.skippedBeforeOpening">
                                {{ preview.skippedBeforeOpening }} lines before the opening date skipped.
                            </span>
                            <span v-if="preview.skippedAccounts.length">
                                Not tracked: {{ preview.skippedAccounts.join(", ") }}.
                            </span>
                        </div>
                    </v-card-text>

                    <v-data-table
                        v-if="pending.length"
                        v-model="selectedKeys"
                        :headers="previewHeaders"
                        :items="pending"
                        item-value="key"
                        show-select
                        :items-per-page="50"
                        density="compact"
                    >
                        <template #[`item.status`]="{ item }">
                            <v-chip size="small" variant="tonal" :color="item.status === 'New' ? 'success' : 'warning'">
                                {{ item.status }}
                            </v-chip>
                            <div v-if="item.changes.length" class="text-caption text-medium-emphasis">
                                {{ item.changes.join(", ") }}
                            </div>
                        </template>
                        <template #[`item.payee`]="{ item }">
                            <div>{{ item.payee ?? item.description }}</div>
                            <div v-if="item.payee && item.description" class="text-caption text-medium-emphasis">
                                {{ item.description }}
                            </div>
                        </template>
                        <template #[`item.category`]="{ item }">
                            <div>{{ item.category }}</div>
                            <div class="text-caption text-medium-emphasis">{{ item.account }}</div>
                        </template>
                        <template #[`item.amount`]="{ item }">{{ money(item.amount) }}</template>
                    </v-data-table>

                    <template v-if="preview.missing.length">
                        <v-card-text>
                            <div class="text-body-2 mb-1">In the ledger but not in this export</div>
                            <div class="text-caption text-medium-emphasis mb-2">
                                Probably deleted or changed in QuickBooks. Ticked ones get hidden from the public page.
                            </div>
                            <v-checkbox
                                v-for="missing in preview.missing"
                                :key="missing.id"
                                v-model="hideIds"
                                :value="missing.id"
                                density="compact"
                                hide-details
                                :label="`${missing.date} · ${missing.payee ?? missing.description} · ${money(missing.amount)} · ${missing.account}`"
                            />
                        </v-card-text>
                    </template>

                    <v-card-actions>
                        <v-spacer />
                        <v-btn variant="text" @click="reset">Discard</v-btn>
                        <v-btn
                            color="primary"
                            variant="flat"
                            :loading="applying"
                            :disabled="selectedKeys.length === 0 && hideIds.length === 0"
                            @click="apply"
                        >
                            Apply {{ selectedKeys.length }} {{ selectedKeys.length === 1 ? "change" : "changes" }}
                            <template v-if="hideIds.length">and hide {{ hideIds.length }}</template>
                        </v-btn>
                    </v-card-actions>
                </template>
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
                        <div v-if="item.payee && item.description" class="text-caption text-medium-emphasis">
                            {{ item.description }}
                        </div>
                        <div v-if="item.hiddenAt" class="text-caption text-warning">
                            Hidden by {{ item.hiddenBy }} on {{ when(item.hiddenAt) }}
                        </div>
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

            <v-window-item value="setup">
                <v-card-text>
                    <div class="text-body-1 mb-1">Tracked accounts</div>
                    <div class="text-caption text-medium-emphasis mb-3">
                        Names must match QuickBooks exactly. The public ledger starts at each opening date, from the
                        opening balance.
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
                    <div class="text-body-1 mb-1">Donor names</div>
                    <div class="text-caption text-medium-emphasis mb-3">
                        Money in from anyone not switched on here shows publicly as "Individual donor".
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
    api,
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

const { notify } = useNotice();

const previewHeaders = [
    { title: "Status", key: "status" },
    { title: "Date", key: "date", nowrap: true },
    { title: "Description", key: "payee", minWidth: "220px" },
    { title: "Category", key: "category" },
    { title: "Amount", key: "amount", align: "end" as const, nowrap: true },
];

const ledgerHeaders = [
    { title: "Date", key: "date", nowrap: true },
    { title: "Description", key: "payee", minWidth: "220px" },
    { title: "Category", key: "category" },
    { title: "Amount", key: "amount", align: "end" as const },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const tab = ref("import");
const data = ref<FinanceData>({ accounts: [], payees: [], transactions: [] });
const loading = ref(false);
const busy = ref(false);
const search = ref("");
const showHidden = ref(false);
const picker = ref<HTMLInputElement | null>(null);
const fileName = ref("");
const csv = ref("");
const preview = ref<FinanceImportPreview | null>(null);
const previewing = ref(false);
const applying = ref(false);
const selectedKeys = ref<string[]>([]);
const hideIds = ref<string[]>([]);
const accountDialog = ref(false);
const accountForm = reactive({ id: "", name: "", openingDate: "", openingBalance: 0 });

const pending = computed(() => preview.value?.rows.filter((row) => row.status !== "Unchanged") ?? []);
const ledger = computed(() =>
    showHidden.value ? data.value.transactions : data.value.transactions.filter((t) => !t.hiddenAt),
);

function count(status: string): number {
    return preview.value?.rows.filter((row) => row.status === status).length ?? 0;
}

function rowProps({ item }: { item: FinanceTransaction }) {
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

async function pickFile(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = "";
    if (!file) {
        return;
    }

    previewing.value = true;
    try {
        csv.value = await file.text();
        fileName.value = file.name;
        preview.value = await api.previewFinances(csv.value);
        selectedKeys.value = pending.value.map((row) => row.key);
        hideIds.value = [];
    } catch (failure) {
        notify(errorMessage(failure, `Could not read ${file.name}.`), "error");
        reset();
    } finally {
        previewing.value = false;
    }
}

function reset() {
    preview.value = null;
    csv.value = "";
    fileName.value = "";
    selectedKeys.value = [];
    hideIds.value = [];
}

async function apply() {
    applying.value = true;
    try {
        const result = await api.applyFinances(csv.value, selectedKeys.value, hideIds.value);
        notify(`Added ${result.added}, updated ${result.updated}, hid ${result.hidden}.`);
        reset();
        await load();
    } catch (failure) {
        notify(errorMessage(failure, "Could not apply the import."), "error");
    } finally {
        applying.value = false;
    }
}

async function setHidden(transaction: FinanceTransaction, hidden: boolean) {
    try {
        data.value = await api.hideFinance(transaction.id, hidden);
        notify(hidden ? "Hidden from the public page." : "Back on the public page.");
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
