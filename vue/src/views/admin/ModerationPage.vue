<template>
    <v-card border>
        <v-card-title class="d-flex align-center flex-wrap ga-3 text-body-1">
            Moderation log
            <v-btn variant="text" size="small" href="/moderation" target="_blank" append-icon="mdi-open-in-new">
                Public page
            </v-btn>
            <v-spacer />
            <v-select v-model="kind" :items="kindOptions" label="Type" style="max-width: 200px" @update:model-value="load" />
            <v-text-field
                v-model="search"
                label="Search"
                prepend-inner-icon="mdi-magnify"
                style="max-width: 240px"
                @keyup.enter="load"
            />
            <v-btn variant="tonal" :loading="loading" @click="load">Search</v-btn>
            <v-btn variant="tonal" prepend-icon="mdi-message-minus" to="/admin/moderation/remove">Remove messages</v-btn>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">Log an action</v-btn>
        </v-card-title>

        <v-data-table
            :headers="headers"
            :items="actions"
            :loading="loading"
            item-value="id"
            :items-per-page="25"
            show-expand
        >
            <template #[`item.occurredAt`]="{ item }">{{ when(item.occurredAt) }}</template>
            <template #[`item.action`]="{ item }">
                <InlineText class="wrap" :text="item.action" />
                <div class="text-caption text-medium-emphasis">{{ kindLabel(item.kind) }} · {{ item.source }}</div>
            </template>
            <template #[`item.reason`]="{ item }">
                <InlineText class="wrap" :text="item.reason" />
            </template>
            <template #[`item.actions`]="{ item }">
                <div class="d-flex ga-1 justify-end">
                    <v-btn size="small" variant="text" icon="mdi-pencil" aria-label="Edit" @click="openEdit(item)" />
                    <v-btn
                        size="small"
                        variant="text"
                        color="error"
                        icon="mdi-delete"
                        aria-label="Delete"
                        @click="confirmDelete(item)"
                    />
                </div>
            </template>
            <template #[`item.data-table-expand`]="{ item, internalItem, isExpanded, toggleExpand }">
                <v-btn
                    v-if="item.removedMessageText !== null"
                    size="small"
                    variant="text"
                    :icon="isExpanded(internalItem) ? 'mdi-chevron-up' : 'mdi-message-text'"
                    aria-label="Removed message"
                    v-tooltip="'Removed message'"
                    @click="toggleExpand(internalItem)"
                />
            </template>
            <template #expanded-row="{ columns, item }">
                <tr>
                    <td :colspan="columns.length" class="py-3">
                        <div class="text-caption text-medium-emphasis mb-1">Removed message (admins only)</div>
                        <div class="wrap removed">{{ item.removedMessageText || "(no text)" }}</div>
                    </td>
                </tr>
            </template>
            <template #no-data>
                <div class="py-8 text-center text-medium-emphasis">Nothing logged.</div>
            </template>
        </v-data-table>
    </v-card>

    <v-dialog v-model="editing" max-width="560">
        <v-card>
            <v-card-title>{{ target ? "Edit entry" : "Log an action" }}</v-card-title>
            <v-card-text class="d-flex flex-column ga-4">
                <v-text-field v-model="form.action" label="What was done" />
                <v-textarea v-model="form.reason" label="Reason" rows="2" auto-grow />
                <v-text-field v-model="form.occurredAt" label="When" type="datetime-local" />
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="editing = false">Cancel</v-btn>
                <v-btn color="primary" :loading="busy" :disabled="!form.action.trim()" @click="save">Save</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>

    <v-dialog v-model="deleting" max-width="420">
        <v-card>
            <v-card-title>Delete this entry?</v-card-title>
            <v-card-text class="text-medium-emphasis">It disappears from the public log too.</v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="deleting = false">Cancel</v-btn>
                <v-btn color="error" :loading="busy" @click="remove">Delete</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from "vue";
import { api, errorMessage, ModerationAction, ModerationKinds } from "@/api/admin";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";
import InlineText from "@/components/InlineText.vue";

const { notify } = useNotice();

const headers = [
    { title: "When", key: "occurredAt", minWidth: "140px" },
    { title: "Action", key: "action", sortable: false, minWidth: "220px" },
    { title: "Reason", key: "reason", sortable: false, minWidth: "180px" },
    { title: "Administrator", key: "administrator" },
    { title: "", key: "actions", sortable: false, align: "end" as const },
    { title: "", key: "data-table-expand" },
];

const kindLabels: Record<string, string> = {
    RemovedMessage: "Removed message",
    Deactivated: "Deactivated",
    ChannelBan: "Channel ban",
    ChannelBanLifted: "Ban lifted",
    Other: "Other",
};

const kindOptions = [{ title: "All", value: "" }, ...ModerationKinds.map((k) => ({ title: kindLabels[k], value: k }))];

const actions = ref<ModerationAction[]>([]);
const kind = ref("");
const search = ref("");
const loading = ref(false);
const busy = ref(false);
const editing = ref(false);
const deleting = ref(false);
const target = ref<ModerationAction | null>(null);
const form = reactive({ action: "", reason: "", occurredAt: "" });

function kindLabel(value: string): string {
    return kindLabels[value] ?? value;
}

function localInput(value: Date): string {
    const offset = value.getTimezoneOffset() * 60_000;
    return new Date(value.getTime() - offset).toISOString().slice(0, 16);
}

async function load() {
    loading.value = true;
    try {
        actions.value = await api.moderation(kind.value, search.value);
    } catch (failure) {
        notify(errorMessage(failure, "Could not load the moderation log."), "error");
    } finally {
        loading.value = false;
    }
}

function openCreate() {
    target.value = null;
    Object.assign(form, { action: "", reason: "", occurredAt: localInput(new Date()) });
    editing.value = true;
}

function openEdit(action: ModerationAction) {
    target.value = action;
    Object.assign(form, {
        action: action.action,
        reason: action.reason,
        occurredAt: localInput(new Date(action.occurredAt)),
    });
    editing.value = true;
}

async function save() {
    busy.value = true;
    const input = {
        action: form.action.trim(),
        reason: form.reason.trim(),
        occurredAt: form.occurredAt ? new Date(form.occurredAt).toISOString() : null,
    };
    try {
        actions.value = target.value
            ? await api.updateModeration(target.value.id, input)
            : await api.createModeration(input);
        notify(target.value ? "Entry updated." : "Action logged.");
        editing.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not save the entry."), "error");
    } finally {
        busy.value = false;
    }
}

function confirmDelete(action: ModerationAction) {
    target.value = action;
    deleting.value = true;
}

async function remove() {
    if (!target.value) {
        return;
    }

    busy.value = true;
    try {
        actions.value = await api.deleteModeration(target.value.id);
        notify("Entry deleted.");
        deleting.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not delete the entry."), "error");
    } finally {
        busy.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.wrap {
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}

.removed {
    font-family: monospace;
    font-size: 0.85rem;
}
</style>
