<script lang="ts">
import { onMount } from 'svelte';
import type { SvelteComponent } from 'svelte';
import * as eventregistrationModel from '../../models/eventModels';
import {
  Page,
  Table,
  pageContentLayoutType
} from '@bexis2/bexis2-core-ui';
import * as dataCaller from '../../services/eventCaller';
import { writable } from 'svelte/store';
import TableOption from '../../components/tableOptions.svelte';
import type { TableConfig } from '@bexis2/bexis2-core-ui';
import { goto } from '$app/navigation';
import tableOptions from '../../components/tableOptions.svelte';
	import Fa from 'svelte-fa';
	import { faPlus } from '@fortawesome/free-solid-svg-icons';

let tableStore = writable<eventregistrationModel.EventListItem[]>([]);

function handleTableAction(e: CustomEvent<{ type: string, row: any }>) {
  const { type, row } = e.detail;
  if (type === 'UPDATE') handleEdit(row);
  if (type === 'DELETE') handleDelete(row);
}

function handleEdit(row) {
  goto('/emm/events/edit', {
	state: {
		id: row.id
	}});
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

<Page title="Events"
	note="overview of events in the system"
	contentLayoutType={pageContentLayoutType.center}>

  		<div class="grid grid-cols-2 gap-5 my-4 pb-1 border-b border-primary-500">
			<div class="h3 h-9">
				Create new Event
			</div>
			<div class="text-right">
			
          <button
            class="btn variant-filled-secondary shadow-md h-9 w-16"
            title="Create new Event"
            id="create"
            on:click={() => goto('/emm/events/create')}><Fa icon={faPlus} /></button>
			
			</div>
		</div>


  <div class="table table-compact w-full">
    <Table config={table}  on:action={e => handleTableAction(e)}/>
  </div>
</Page>