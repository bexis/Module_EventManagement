<script lang="ts">
import { onMount } from 'svelte';
import type { SvelteComponent } from 'svelte';
import * as eventregistrationModel from '../../models/eventModels';
import {
  Page,
  Table,
  ErrorMessage,
  helpStore,
  TablePlaceholder,
  notificationStore,
  notificationType,
  pageContentLayoutType
} from '@bexis2/bexis2-core-ui';
import * as dataCaller from '../../services/eventCaller';
import { writable } from 'svelte/store';
import TableOption from '../../components/tableOptions.svelte';
import type { TableConfig } from '@bexis2/bexis2-core-ui';
import { goto } from '$app/navigation';
import tableOptions from '../../components/tableOptions.svelte';

let tableStore = writable<eventregistrationModel.EventListItem[]>([]);

function handleTableAction(e: CustomEvent<{ type: string, row: any }>) {
  const { type, row } = e.detail;
  if (type === 'UPDATE') handleEdit(row);
  if (type === 'DELETE') handleDelete(row);
}

function handleEdit(row) {
  goto(`/events/edit/${row.id}`);
}

function handleDelete(row) {
console.log('Delete row:', row.id);
  if (confirm(`Event "${row.name}" wirklich löschen?`)) {
    dataCaller.deleteEvent(row.id).then(() => reload());
  }
}

let table: TableConfig<eventregistrationModel.EventListItem> = {
  id: 'metadatatable',
  data: tableStore,
  optionsComponent: tableOptions as unknown as typeof SvelteComponent
};

onMount(async () => {
  const data = await dataCaller.getEvents();
  tableStore.set(Array.isArray(data) ? data : []);
});

async function reload() {
  const newData = await dataCaller.getEvents();
  tableStore.set(Array.isArray(newData) ? newData : []);
}
</script>

<Page help={true} title="Manage Events">
  <div class="table table-compact w-full">
    <Table config={table} id="event-table" class="w-full" 
  on:action={e => handleTableAction(e)}
/>
  </div>
</Page>