<script lang="ts">
import Entry from '../../../../components/entry.svelte';
import * as dataCaller from '../../../../services/eventRegistrationCaller';
import {
  Page
} from '@bexis2/bexis2-core-ui';
import Fa from 'svelte-fa';
import { faSave, faXmark } from '@fortawesome/free-solid-svg-icons';
import { page } from '$app/stores';
import { goto } from '$app/navigation';

 const eventId = Number($page.params.id);

let validationErrors: string[] = [];

function isEmpty(value: any) {
	return (
		value === undefined ||
		value === null ||
		value === '' ||
		(typeof value === 'string' && value.trim() === '') ||
		(Array.isArray(value) && value.length === 0)
	);
}

function collectValidationErrorsFromEntry(entry: any, errors: string[]) {
	if (entry.required === true && isEmpty(entry.value)) {
		errors.push(entry.key);
	}

	if (entry.type === 'EntryList' && Array.isArray(entry.value)) {
		for (const childEntry of entry.value) {
			collectValidationErrorsFromEntry(childEntry, errors);
		}
	}
}

function validateRegistration(registrationData: any): boolean {
	const errors: string[] = [];

	for (const group of registrationData.registration) {
		for (const entry of group.entries) {
			collectValidationErrorsFromEntry(entry, errors);
		}
	}

	validationErrors = errors;

	return errors.length === 0;
}

async function handleSave(registrationData: any) {
	if (!validateRegistration(registrationData)) return;

	await dataCaller.saveEventRegistration({
		eventId,
		jsonFile: JSON.stringify(registrationData)
	});

	goto('/emm/eventregistration');
}

	function handleCancel() {
		goto('/emm/eventregistration');
	}


</script>

<Page>
	{#await dataCaller.getEventRegistration(eventId)}
		<div id="spinner">... loading ...</div>
	{:then data}
		{@const registrationData = JSON.parse(data.jsonFile)}

		<div class="p-6 space-y-6">
			<div class="rounded-xl shadow-md border p-5 bg-white">
				<h1 class="text-2xl font-bold mb-4">{data.name}</h1>

				<div class="grid grid-cols-2 gap-4">
					<div>
						<div class="text-sm text-gray-500">Date</div>
						<div class="font-medium">{data.date}</div>
					</div>

					<div>
						<div class="text-sm text-gray-500">Location</div>
						<div class="font-medium">{data.location}</div>
					</div>

					<div>
						<div class="text-sm text-gray-500">Language</div>
						<div class="font-medium">{data.language}</div>
					</div>
				</div>

				{#if data.importantInformation}
					<div class="mt-5 p-4 rounded-lg bg-yellow-50 border border-yellow-200">
						<div class="font-semibold mb-1">Important Information</div>
						<div>{data.importantInformation}</div>
					</div>
				{/if}
			</div>

				<form on:submit|preventDefault={() => handleSave(registrationData)}>
				{#each registrationData.registration as group}
					<div class="rounded-xl shadow-md border p-5 bg-white mb-5">
						<h2 class="text-xl font-semibold mb-4">{group.title}</h2>

						{#each group.entries as entry}
							<Entry {entry} {validationErrors} />
						{/each}
					</div>
				{/each}

				<div class="py-5 text-right">
					<button class="btn variant-filled-primary h-9 w-16 shadow-md" type="submit">
						<Fa icon={faSave} />
					</button>
				</div>
			</form>
		</div>
	{:catch error}
		<div id="spinner">{error.message}</div>
	{/await}
</Page>