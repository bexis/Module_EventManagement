<script lang="ts">
import { onMount } from 'svelte';
import type { SvelteComponent } from 'svelte';
import { page } from '$app/stores';

import * as eventModel from '../../../../models/eventModels';
import {
  Page,
  Table,
  pageContentLayoutType
} from '@bexis2/bexis2-core-ui';
import * as dataCaller from '../../../../services/eventResultCaller';
import { writable } from 'svelte/store';
import type { TableConfig } from '@bexis2/bexis2-core-ui';
import { goto } from '$app/navigation';
import tableActions from '../../../../components/tableActionsEventRegs.svelte';
	import Fa from 'svelte-fa';
	import { faPlus, faArrowLeft} from '@fortawesome/free-solid-svg-icons';

const tableStore = writable<any[]>([]);
$: id = $page.params.id;

async function reload() {
  const newData = await dataCaller.getEvents();
  tableStore.set(Array.isArray(newData) ? newData : []);
}

let table= {
		 id: 'evebntregistrationresults',
		 data: writable<any[]>([]),
		resizable: 'both',
		rowHeight: 70,
		exportable: true,
        optionsComponent: tableActions as unknown as typeof SvelteComponent
	 } as TableConfig<(any)>;

onMount(async () => {
  const data = await dataCaller.getEventResults(id);
  let parsed = [];
  if (data && data.jsonFiles) {
    try {
      parsed = JSON.parse(data.jsonFiles);
    } catch (e) {
      console.error("Fehler beim Parsen von jsonFiles:", e);
    }
  }

  // 1. Spaltennamen aus dem ersten Element extrahieren
  let columns: { key: string, title: string }[] = [];
  if (parsed.length > 0 && parsed[0].registration) {
    parsed[0].registration.forEach(section => {
      section.entries.forEach(entry => {
        columns.push({ key: entry.key, title: entry.title });
      });
    });
  }

  // 2. Daten in flache Objekte umwandeln
  const rows = parsed.map(item => {
    const row: Record<string, any> = {};
    item.registration.forEach(section => {
      section.entries.forEach(entry => {
        row[entry.key] = entry.value;
      });
    });
    return row;
  });

  // 3. TableConfig anpassen
  table.columns = columns.map(col => ({
    field: col.key,
    title: col.title,
    sortable: true
  }));
  table.data.set(rows);
});

function handleTableAction(e: CustomEvent<{ type: string, row: any }>) {
  const { type, row } = e.detail;
  if (type === 'EDIT') goto(`/eventregistration/edit/${row.id}`);
  if (type === 'DELETE') handleDelete(row);
  //if (type === 'RESEND') handleResend(row);
}

function handleDelete(row) {
console.log('Delete row:', row.id);
  if (confirm(`Event "${row.name}" wirklich löschen?`)) {
    dataCaller.deleteEvent(row.id).then(() => reload());
  }
}

function back() {
		goto("/results");
	}

</script>

<Page>
  <div class="flex justify-start mb-4">
    <button
      title="back"
      class="btn variant-filled-warning"
      on:click={() => back()}
    >
      <Fa icon={faArrowLeft} />
    </button>
  </div>

   <div class="table table-compact w-full">
    <Table config={table}  on:action={e => handleTableAction(e)}/>
  </div>
</Page>