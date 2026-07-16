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
let showPasswordModal = false;
let passwordInput = '';
let pendingRow: { id: number; [key: string]: any } | null = null;
let passwordError = '';


async function reload() {
  const newData = await dataCaller.getEvents();
  tableStore.set(Array.isArray(newData) ? newData : []);
}

function handleTableAction(e: CustomEvent<{ type?: string, row: any }>) {
  const { type, row } = e.detail;
  if (!row) return;

  if (type === 'REGISTER') {
  pendingRow = row;
  showPasswordModal = true;
  passwordInput = '';
  passwordError = '';
  return;
}
  else  if  (type === 'EDIT') {
  goto('/emm/eventregistration/edit', {
	state: {
		id: row.id
	}});
  } else if (type === 'DELETE') {
    if (confirm(`Really delete registration for "${row.name}"?`)) {

      dataCaller.deleteEventRegistration(row.id);
      reload();
    }
  }
}


async function checkPassword() {
  // Beispiel: Passwort ist "demo"
  if(pendingRow) {
    let pass = await dataCaller.getEventRegistrationPassword(pendingRow.id);
    if (passwordInput === pass) {
      showPasswordModal = false;
      goto('/emm/eventregistration/create', { state: { id: pendingRow.id } });
    } else {
      passwordError = 'Wrong password!';
    }
  }
}

let table: TableConfig<eventregistrationModel.EventListItem> = {
  id: 'metadatatable',
  data: tableStore,
  optionsComponent: tableAction as unknown as typeof SvelteComponent,
  columns: {
			name: {
				header: 'Name'
			},
			deadline: {
				header: 'Deadline'
				
			},
			participants: {
				header: 'Participants'
			},
      alreadyRegistered: {
        exclude: true
      },
      editAllowed: {
        exclude: true
      }
		}
};

onMount(async () => {
  const data = await dataCaller.getEvents();
  tableStore.set(Array.isArray(data) ? data : []);
});

const link = [...document.querySelectorAll('a')]
	.find(a => a.textContent?.trim() === 'Event Registration');

console.log({
	attribute: link?.getAttribute('href'),
	resolved: link?.href,
	baseURI: document.baseURI
});


</script>

<Page help={true} title="Manage Events">
  <div class="table table-compact w-full">
    <Table config={table} 
  on:action={e => handleTableAction(e)}
/>
  </div>

  {#if showPasswordModal}
  <div class="fixed inset-0 bg-black bg-opacity-30 flex items-center justify-center z-50">
    <div class="bg-white p-6 rounded shadow-lg w-80">
      <h2 class="text-lg font-bold mb-2">Enter password</h2>
      <input
        type="password"
        class="input input-bordered w-full mb-2"
        bind:value={passwordInput}
        placeholder="Password"
        on:keydown={(e) => e.key === 'Enter' && checkPassword()}
      />
      {#if passwordError}
        <div class="text-red-600 text-sm mb-2">{passwordError}</div>
      {/if}
      <div class="flex justify-end gap-2">
        <button class="btn" on:click={() => { showPasswordModal = false; }}>Cancel</button>
        <button class="btn btn-primary" on:click={checkPassword}>OK</button>
      </div>
    </div>
  </div>
{/if}

</Page>