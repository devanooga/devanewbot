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
            <v-switch v-model="showHidden" label="Show hidden" color="primary" density="compact" hide-details />
            <v-btn variant="tonal" prepend-icon="mdi-message-minus" to="/admin/moderation/remove">Remove messages</v-btn>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreate">Log an action</v-btn>
        </v-card-title>

        <v-data-table
            :headers="headers"
            :items="visibleActions"
            :row-props="rowProps"
            :loading="loading"
            item-value="id"
            :items-per-page="25"
            show-expand
        >
            <template #[`item.occurredAt`]="{ item }">{{ when(item.occurredAt) }}</template>
            <template #[`item.action`]="{ item }">
                <InlineText class="wrap" :text="item.action" />
                <div class="text-caption text-medium-emphasis">{{ kindLabel(item.kind) }} · {{ sourceLabel(item.source) }}</div>
                <div v-if="item.hiddenAt" class="text-caption text-warning">
                    Hidden from the public log by {{ item.hiddenBy }} on {{ when(item.hiddenAt) }}
                </div>
            </template>
            <template #[`item.reason`]="{ item }">
                <InlineText class="wrap" :text="item.reason" />
            </template>
            <template #[`item.actions`]="{ item }">
                <div class="d-flex ga-1 justify-end">
                    <v-btn size="small" variant="text" icon="mdi-pencil" aria-label="Edit" @click="openEdit(item)" />
                    <v-btn
                        v-if="item.hiddenAt"
                        size="small"
                        variant="text"
                        icon="mdi-eye"
                        aria-label="Unhide"
                        v-tooltip="'Show on the public log again'"
                        :loading="busy && target?.id === item.id"
                        @click="unhide(item)"
                    />
                    <v-btn
                        v-else
                        size="small"
                        variant="text"
                        icon="mdi-eye-off"
                        aria-label="Hide"
                        v-tooltip="'Hide from the public log'"
                        @click="confirmHide(item)"
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

    <v-dialog v-model="hiding" max-width="420">
        <v-card>
            <v-card-title>Hide this entry?</v-card-title>
            <v-card-text class="text-medium-emphasis">
                It comes off the public log but stays here, and you can unhide it later.
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="hiding = false">Cancel</v-btn>
                <v-btn color="warning" :loading="busy" @click="hide">Hide</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
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

const sourceLabels: Record<string, string> = {
    Slack: "from Slack",
    Admin: "from the admin panel",
    Imported: "imported from the old log",
};

const kindOptions = [{ title: "All", value: "" }, ...ModerationKinds.map((k) => ({ title: kindLabels[k], value: k }))];

const actions = ref<ModerationAction[]>([]);
const kind = ref("");
const search = ref("");
const loading = ref(false);
const busy = ref(false);
const editing = ref(false);
const hiding = ref(false);
const showHidden = ref(false);
const target = ref<ModerationAction | null>(null);
const form = reactive({ action: "", reason: "", occurredAt: "" });

const visibleActions = computed(() =>
    showHidden.value ? actions.value : actions.value.filter((action) => !action.hiddenAt),
);

function rowProps({ item }: { item: ModerationAction }) {
    return item.hiddenAt ? { class: "hidden-entry" } : {};
}

function kindLabel(value: string): string {
    return kindLabels[value] ?? value;
}

function sourceLabel(value: string): string {
    return sourceLabels[value] ?? value;
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

function confirmHide(action: ModerationAction) {
    target.value = action;
    hiding.value = true;
}

async function setHidden(action: ModerationAction, hidden: boolean) {
    target.value = action;
    busy.value = true;
    try {
        actions.value = await api.hideModeration(action.id, hidden);
        notify(hidden ? "Hidden from the public log." : "Back on the public log.");
        hiding.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not update the entry."), "error");
    } finally {
        busy.value = false;
    }
}

async function hide() {
    if (target.value) {
        await setHidden(target.value, true);
    }
}

async function unhide(action: ModerationAction) {
    await setHidden(action, false);
}

onMounted(load);
</script>

<style scoped>
.wrap {
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}

:deep(.hidden-entry) {
    opacity: 0.6;
}

.removed {
    font-family: monospace;
    font-size: 0.85rem;
}
</style>
