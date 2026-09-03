<script lang="ts">
import { onMount } from 'svelte';
import type { SvelteComponent } from 'svelte';
import { page } from '$app/state';
import {Page, Table} from '@bexis2/bexis2-core-ui';
import * as dataCaller from '../../../services/eventResultCaller';
import { writable } from 'svelte/store';
import type { TableConfig } from '@bexis2/bexis2-core-ui';
import { goto } from '$app/navigation';
import tableActions from '../../../components/tableActionsEventRegs.svelte';
import tableActionsWaitingList from '../../../components/tableActionsWaitingList.svelte';
import Fa from 'svelte-fa';
import { faArrowLeft} from '@fortawesome/free-solid-svg-icons';
import type { Columns } from '@bexis2/bexis2-core-ui';

const tableStore = writable<any[]>([]);
const id = Number(page.url.searchParams.get('id'));

// Typdefinition für die Struktur der Daten
interface RegistrationEntry { key: string; title: string; value: any; }
interface RegistrationSection { entries: RegistrationEntry[]; }
interface ParsedItem { registration?: RegistrationSection[]; [key: string]: any; }

const hiddenColumnKeys = new Set(['id', 'refId']);

async function reload() {
  const newData = await dataCaller.getEvents();
  tableStore.set(Array.isArray(newData) ? newData : []);
}

let table= {
		 id: 'eventregistrationresults',
		 data: writable<any[]>([]),
		resizable: 'both',
		rowHeight: 50,
		exportable: true,
    optionsComponent: tableActions as unknown as typeof SvelteComponent
	 } as TableConfig<(any)>;

   let table2= {
		 id: 'eventwaitinglistresults',
		 data: writable<any[]>([]),
		resizable: 'both',
		rowHeight: 50,
		exportable: true,
    optionsComponent: tableActionsWaitingList as unknown as typeof SvelteComponent
	 } as TableConfig<(any)>;

onMount(async () => {
  const regs = await dataCaller.getEventResults(id);
  const waitingList = await  dataCaller.getEventWaitingListResults(id);
  console.log("WaitingList0", waitingList);
  if(waitingList)
  {
    console.log("WaitingList", waitingList);
    let parsed: ParsedItem[] = [];
    if (waitingList && waitingList.jsonFiles) {
      try {
        parsed = JSON.parse(waitingList.jsonFiles) as ParsedItem[];
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
  (item.registration ?? []).forEach(section => {
    section.entries.forEach(entry => {
      row[entry.key] = entry.value;
    });
  });
  // ID aus dem Ursprungsobjekt übernehmen
  if (typeof item.id !== "undefined") {
    row.id = item.id;
  }
  return row;
});

    // 3. TableConfig anpassen
    table2.columns = columns.filter(({ key }) => !hiddenColumnKeys.has(key)).reduce((acc, col) => {
      acc[col.key] = {
        header: col.title,      // Titel der Spalte
        // weitere Optionen nach Bedarf, z.B. sortable:
        disableSorting: false
      };
      return acc;
    }, {
      id: { exclude: true },
      refId: { exclude: true }
    } as Columns);

    table2.columns = columns.filter(({ key }) => !hiddenColumnKeys.has(key)).reduce((acc, col) => {
      acc[col.key] = {
        header: col.title,
        disableSorting: false
      };
      return acc;
    }, {
      id: { exclude: true },
      refId: { exclude: true }
    } as Columns);

    table2.data.set(rows);
  }
   
  let parsed: ParsedItem[] = [];
  if (regs && regs.jsonFiles) {
    try {
      parsed = JSON.parse(regs.jsonFiles) as ParsedItem[];
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
  (item.registration ?? []).forEach(section => {
    section.entries.forEach(entry => {
      row[entry.key] = entry.value;
    });
  });
  // ID aus dem Ursprungsobjekt übernehmen
  if (typeof item.id !== "undefined") {
    row.id = item.id;
  }
   if (typeof item.refId !== "undefined") {
    row.refId = item.refId;
  }
  return row;
});

  // 3. TableConfig anpassen
  table.columns = columns.filter(({ key }) => !hiddenColumnKeys.has(key)).reduce((acc, col) => {
    acc[col.key] = {
      header: col.title,
      disableSorting: false
    };
    return acc;
  }, {
    id: { exclude: true },
    refId: { exclude: true }
  } as Columns);

  table.data.set(rows);
});

function handleTableAction(e: CustomEvent<{ type: string, row: any }>) {
  const { type, row } = e.detail;
  if (type === 'EDIT') goto(`/emm/eventregistration/edit/?id=${id}&ref_id=${row.refId}`);
  if (type === 'DELETE') handleDelete(row);
  if (type === 'MOVE') handleMove(row);
  if (type === 'RESEND') handleResend(row);
}

function handleDelete(row) {
console.log('Delete row:', row.id);
  if (confirm(`Really delete event registration with id: "${row.name}"?`)) {
    dataCaller.deleteRegistration(row.id).then(() => reload());
  }
}

function handleMove(row) {
  console.log('Move row:', row.id);
  if (confirm(`Really move event registration with id: "${row.name}"?`)) {
    dataCaller.moveEventRegistration(row.id).then(() => reload());
  }
}

function handleResend(row) {
  console.log('Resend row:', row.id);
  dataCaller.Resend(row.id).then(() => reload());
}

function back() {
		goto("/emm/eventregistrationresult");
	}

</script>

<Page>
  <div class="w-full max-w-7xl p-5 space-y-5 border-y border-solid border-surface-500">
    <h1 class="h1">Event Results</h1>
  </div>
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
  <div class="h3 h-9">Waiting List</div>
   <div class="table table-compact w-full">
    <Table config={table2}  on:action={e => handleTableAction(e)}/>
  </div>
</Page>