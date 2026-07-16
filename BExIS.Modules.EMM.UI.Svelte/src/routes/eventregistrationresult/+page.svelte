<script lang="ts">
import { onMount } from 'svelte';
import type { SvelteComponent } from 'svelte';
import * as eventModel from '../../models/eventModels';
import {
  Page,
  Table
} from '@bexis2/bexis2-core-ui';
import * as dataCaller from '../../services/eventResultCaller';
import { writable } from 'svelte/store';
import type { TableConfig } from '@bexis2/bexis2-core-ui';
import { goto } from '$app/navigation';
import tableActions from '../../components/tableActionResults.svelte';


let tableStore = writable<eventModel.EventListItem[]>([]);

onMount(async () => {
  const data = await dataCaller.getEvents();
  tableStore.set(Array.isArray(data) ? data : []);
});

async function reload() {
  const newData = await dataCaller.getEvents();
  tableStore.set(Array.isArray(newData) ? newData : []);
}

function handleShow(row) {
  goto('/emm/eventregistrationresult/show', {
	state: {
		id: row.id
	}});
}

function handleDelete(row) {
  console.log('Delete row:', row.id);
  if (confirm(`Really delete event: "${row.name}"?`)) {
    dataCaller.deleteEvent(row.id).then(() => reload());
  }
}

function handleClear(row) {
  console.log('Clear row:', row.id);
  if (confirm(`Really clear event: "${row.name}"?`)) {
    dataCaller.clearEvent(row.id).then(() => reload());
  }
}

function handleTableAction(e: CustomEvent<{ type: string, row: any }>) {
  const { type, row } = e.detail;
  if (type === 'SHOW') handleShow(row);
  if (type === 'DELETE') handleDelete(row);
  if (type === 'CLEAR') handleClear(row);
}


let table: TableConfig<eventModel.EventListItem> = {
  id: 'events',
  data: tableStore,
  optionsComponent: tableActions as unknown as typeof SvelteComponent
};

</script>

<Page>
   <div class="table table-compact w-full">
    <Table config={table}  on:action={e => handleTableAction(e)}/>
  </div>
</Page>   