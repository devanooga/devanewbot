<template>
    <v-card border>
        <v-card-title class="d-flex align-center text-body-1">
            Admin accounts
            <v-spacer />
            <v-btn color="primary" prepend-icon="mdi-account-plus" @click="openAdd">Add admin</v-btn>
        </v-card-title>

        <v-data-table :headers="headers" :items="users" :loading="loading" item-value="id">
            <template #[`item.email`]="{ item }">
                {{ item.email }}
                <v-chip v-if="item.email === user?.email" size="x-small" variant="tonal" class="ml-2">
                    you
                </v-chip>
            </template>
            <template #[`item.createdAt`]="{ item }">{{ when(item.createdAt) }}</template>
            <template #[`item.lockoutEnd`]="{ item }">
                <v-chip v-if="lockedOut(item)" size="small" color="warning" variant="tonal">
                    locked until {{ when(item.lockoutEnd) }}
                </v-chip>
                <span v-else class="text-medium-emphasis">active</span>
            </template>
            <template #[`item.actions`]="{ item }">
                <v-btn size="small" variant="text" @click="openReset(item)">Reset password</v-btn>
                <v-btn
                    icon="mdi-delete"
                    size="small"
                    variant="text"
                    color="error"
                    :disabled="item.email === user?.email"
                    @click="confirmDelete(item)"
                />
            </template>
        </v-data-table>
    </v-card>

    <v-dialog v-model="addDialog" max-width="460">
        <v-card>
            <v-card-title>Add an admin</v-card-title>
            <v-card-text class="d-flex flex-column ga-4">
                <v-text-field v-model="form.email" label="Email" type="email" autofocus />
                <v-text-field v-model="form.password" label="Password" type="password" autocomplete="new-password" />
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="addDialog = false">Cancel</v-btn>
                <v-btn color="primary" :loading="busy" @click="add">Create</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>

    <v-dialog v-model="resetDialog" max-width="460">
        <v-card>
            <v-card-title>Reset password for {{ target?.email }}</v-card-title>
            <v-card-text>
                <v-text-field
                    v-model="newPassword"
                    label="New password"
                    type="password"
                    autocomplete="new-password"
                    autofocus
                />
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="resetDialog = false">Cancel</v-btn>
                <v-btn color="primary" :loading="busy" @click="reset">Set password</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>

    <v-dialog v-model="deleteDialog" max-width="420">
        <v-card>
            <v-card-title>Delete {{ target?.email }}?</v-card-title>
            <v-card-text class="text-medium-emphasis">They lose access immediately.</v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="deleteDialog = false">Cancel</v-btn>
                <v-btn color="error" :loading="busy" @click="remove">Delete</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import { Account, api, errorMessage } from "@/api/admin";
import { useAuth } from "@/composables/useAuth";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";

const { user } = useAuth();
const { notify } = useNotice();

const headers = [
    { title: "Email", key: "email" },
    { title: "Created", key: "createdAt" },
    { title: "Status", key: "lockoutEnd" },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const users = ref<Account[]>([]);
const target = ref<Account | null>(null);
const form = ref({ email: "", password: "" });
const newPassword = ref("");
const addDialog = ref(false);
const resetDialog = ref(false);
const deleteDialog = ref(false);
const loading = ref(false);
const busy = ref(false);

function lockedOut(account: Account): boolean {
    return account.lockoutEnd !== null && new Date(account.lockoutEnd) > new Date();
}

function openAdd() {
    form.value = { email: "", password: "" };
    addDialog.value = true;
}

function openReset(account: Account) {
    target.value = account;
    newPassword.value = "";
    resetDialog.value = true;
}

function confirmDelete(account: Account) {
    target.value = account;
    deleteDialog.value = true;
}

async function load() {
    loading.value = true;
    try {
        users.value = await api.users();
    } catch (failure) {
        notify(errorMessage(failure, "Could not load accounts."), "error");
    } finally {
        loading.value = false;
    }
}

async function add() {
    busy.value = true;
    try {
        users.value = await api.createUser(form.value.email, form.value.password);
        notify(`Added ${form.value.email}.`);
        addDialog.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not add the account."), "error");
    } finally {
        busy.value = false;
    }
}

async function reset() {
    if (!target.value) {
        return;
    }

    busy.value = true;
    try {
        await api.resetUserPassword(target.value.id, newPassword.value);
        notify(`Password reset for ${target.value.email}.`);
        resetDialog.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not reset the password."), "error");
    } finally {
        busy.value = false;
    }
}

async function remove() {
    if (!target.value) {
        return;
    }

    busy.value = true;
    try {
        users.value = await api.deleteUser(target.value.id);
        notify(`Deleted ${target.value.email}.`);
        deleteDialog.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not delete the account."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(load);
</script>
