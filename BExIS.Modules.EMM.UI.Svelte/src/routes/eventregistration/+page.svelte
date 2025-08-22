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
import * as dataCaller from '../../services/eventRegistrationCaller';
import { writable } from 'svelte/store';
import type { TableConfig } from '@bexis2/bexis2-core-ui';
import { goto } from '$app/navigation';
import tableAction from '../../components/tableAction.svelte';

let tableStore = writable<eventregistrationModel.EventListItem[]>([]);

function handleTableAction(e: CustomEvent<{ row: any }>) {
  const {  row } = e.detail; 

  if (row) {

    //ToDo: call passwort before
    goto('/eventregistration/create', { state: { id: row.id } });
    
  }
}




let table: TableConfig<eventregistrationModel.EventListItem> = {
  id: 'metadatatable',
  data: tableStore,
  optionsComponent: tableAction as unknown as typeof SvelteComponent
//   columns: {
// 			name: {
// 				header: 'Name'
// 			},
// 			deadline: {
// 				header: 'Deadline'
				
// 			},
// 			participants: {
// 				header: 'Participants'
// 			}
// 		}
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