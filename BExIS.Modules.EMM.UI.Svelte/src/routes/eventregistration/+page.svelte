<script lang="ts">
import { onMount } from 'svelte';
import * as  eventModels from '../../models/eventModels';
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
    import * as dataCaller  from '../../services/eventCaller';
    import { writable, type Writable } from 'svelte/store';
    import TableOption from '../components/tableOptions.svelte';

    let e: eventModels.EventListItem[] = [];
	let event: eventModels.EventListItem;
    const tableStore = writable<any[]>([]);
    $: events = e;
	$: tableStore.set(e);

    async function reload() {
		e = await dataCaller.getEvents();
	}

</script>

<Page help={true} title="Manage Events">

<div class="table table-compact w-full">
    <Table
			config={{
					id: 'Events',
					data: tableStore,
					optionsComponent: TableOption
                    }}
					
			/>
</div>

</Page>