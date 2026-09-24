<template>
    <v-card border>
        <v-card-title class="d-flex align-center text-body-1">
            Watched callsigns
            <v-spacer />
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openAdd">Add callsign</v-btn>
        </v-card-title>

        <v-data-table :headers="headers" :items="watches" item-value="callsign" :items-per-page="25">
            <template #[`item.callsign`]="{ item }">
                <span class="font-weight-medium">{{ item.callsign }}</span>
            </template>
            <template #[`item.grid`]="{ item }">
                <v-chip v-if="item.grid" size="small" variant="tonal">{{ item.grid }}</v-chip>
                <span v-else class="text-medium-emphasis">looked up</span>
            </template>
            <template #[`item.slackUserId`]="{ item }">
                <span v-if="item.slackUserId">{{ item.slackUserId }}</span>
                <span v-else class="text-medium-emphasis">unclaimed</span>
            </template>
            <template #[`item.createdAt`]="{ item }">{{ when(item.createdAt) }}</template>
            <template #[`item.actions`]="{ item }">
                <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEdit(item)" />
                <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="confirmRemove(item)" />
            </template>
            <template #no-data>
                <div class="py-8 text-center text-medium-emphasis">Nobody is being watched yet.</div>
            </template>
        </v-data-table>
    </v-card>

    <v-dialog v-model="dialog" max-width="460">
        <v-card>
            <v-card-title>{{ editing ? `Edit ${form.callsign}` : "Add a callsign" }}</v-card-title>
            <v-card-text class="d-flex flex-column ga-4">
                <v-text-field
                    v-if="!editing"
                    v-model="form.callsign"
                    label="Callsign"
                    autofocus
                    @keyup.enter="save"
                />
                <v-text-field
                    v-model="form.grid"
                    label="Grid square"
                    hint="Leave empty to look it up automatically"
                    persistent-hint
                    @keyup.enter="save"
                />
                <v-text-field
                    v-model="form.slackUserId"
                    label="Slack user id"
                    hint="Alerts mention this person"
                    persistent-hint
                    @keyup.enter="save"
                />
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="dialog = false">Cancel</v-btn>
                <v-btn color="primary" :loading="busy" @click="save">Save</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>

    <v-dialog v-model="removing" max-width="420">
        <v-card>
            <v-card-title>Stop watching {{ target?.callsign }}?</v-card-title>
            <v-card-text class="text-medium-emphasis">
                Spots already recorded stay in the log.
            </v-card-text>
            <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="removing = false">Cancel</v-btn>
                <v-btn color="error" :loading="busy" @click="remove">Remove</v-btn>
            </v-card-actions>
        </v-card>
    </v-dialog>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { api, errorMessage, Watch } from "@/api/admin";
import { useAdminData } from "@/composables/useAdminData";
import { useNotice } from "@/composables/useNotice";
import { when } from "@/composables/useFormat";

const { status } = useAdminData();
const { notify } = useNotice();

const headers = [
    { title: "Callsign", key: "callsign" },
    { title: "Grid", key: "grid" },
    { title: "Slack user", key: "slackUserId" },
    { title: "Added by", key: "addedBy" },
    { title: "Added", key: "createdAt" },
    { title: "", key: "actions", sortable: false, align: "end" as const },
];

const dialog = ref(false);
const removing = ref(false);
const editing = ref(false);
const busy = ref(false);
const target = ref<Watch | null>(null);
const form = ref({ callsign: "", grid: "", slackUserId: "" });

const watches = computed(() => status.value?.watches ?? []);

function applyWatches(updated: Watch[]) {
    if (status.value) {
        status.value.watches = updated;
    }
}

function openAdd() {
    editing.value = false;
    form.value = { callsign: "", grid: "", slackUserId: "" };
    dialog.value = true;
}

function openEdit(watch: Watch) {
    editing.value = true;
    form.value = {
        callsign: watch.callsign,
        grid: watch.grid ?? "",
        slackUserId: watch.slackUserId ?? "",
    };
    dialog.value = true;
}

function confirmRemove(watch: Watch) {
    target.value = watch;
    removing.value = true;
}

async function save() {
    busy.value = true;
    try {
        applyWatches(
            editing.value
                ? await api.updateWatch(form.value.callsign, form.value)
                : await api.addWatch(form.value),
        );
        notify(`${editing.value ? "Updated" : "Added"} ${form.value.callsign.toUpperCase()}.`);
        dialog.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not save the callsign."), "error");
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
        applyWatches(await api.removeWatch(target.value.callsign));
        notify(`Removed ${target.value.callsign}.`);
        removing.value = false;
    } catch (failure) {
        notify(errorMessage(failure, "Could not remove the callsign."), "error");
    } finally {
        busy.value = false;
    }
}
</script>
