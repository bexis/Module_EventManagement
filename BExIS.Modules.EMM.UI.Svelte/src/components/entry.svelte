<script lang="ts">
	import { ReadEntryModel } from '../models/eventregistrationModel';
	import {
		TextInput,
		CodeEditor,
		NumberInput,
		MultiSelect,
		helpStore
	} from '@bexis2/bexis2-core-ui';
	import { SlideToggle } from '@skeletonlabs/skeleton';
	import Fa from 'svelte-fa';
	import { faAdd, faTrash } from '@fortawesome/free-solid-svg-icons';

	export let entry: ReadEntryModel;
	export let isChild = false;
	export let validationErrors: string[] = [];

	let JSONValue: string;
	let initialJSONValue: string;

	$: hasError = validationErrors.includes(entry.key);

	if (entry.type === 'JSON') {
		initialJSONValue = JSON.stringify(entry.value, null, 2);
		JSONValue = initialJSONValue;
	}

	$: onChange(JSONValue);

	function onChange(value: string) {
		if (typeof value !== 'undefined') {
			try {
				entry.value = JSON.parse(value);
			} catch {
				console.log('error');
			}
		}
	}

	function removeItem(index) {
		if (Object.values(entry.value).length > 1) {
			entry.value.splice(index, 1);
			entry.value = entry.value;
		}
	}

	function addItem() {
		entry.value = [
			...entry.value,
			new ReadEntryModel({
				key: entry.value[0].key,
				title: entry.value[0].title,
				type: entry.value[0].type,
				value: '',
				description: '',
				required: entry.value[0].required
			})
		];
	}
</script>

<div class="pb-10">
	<div class:rounded-md={hasError} class:border={hasError} class:border-red-500={hasError} class:p-2={hasError}>
		{#if entry.options && entry.options.length >= 1}
			<div id={entry.key} on:mouseover={() => helpStore.show(entry.key)}>
				<div class="mb-1 font-medium">
					{entry.title}
					{#if entry.required}
						<span class="text-red-600">*</span>
					{/if}
				</div>

				<MultiSelect
					id={entry.key}
					title=""
					source={entry.options}
					bind:target={entry.value}
					isMulti={false}
				/>
			</div>
		{:else if entry.type.toLowerCase() === 'string'}
			<TextInput
				id={entry.key}
				label={`${entry.title}${entry.required ? ' *' : ''}`}
				bind:value={entry.value}
				on:input
				help={true}
			/>
		{:else if entry.type.toLowerCase().includes('int')}
			<NumberInput
				id={entry.key}
				label={`${entry.title}${entry.required ? ' *' : ''}`}
				bind:value={entry.value}
				on:input
				help={true}
			/>
		{:else if entry.type.toLowerCase() === 'boolean'}
			<div id={entry.key} on:mouseover={() => helpStore.show(entry.key)}>
				<SlideToggle active="bg-primary-500" name="slider-label" size="sm" bind:checked={entry.value}>
					{entry.title}
					{#if entry.required}
						<span class="text-red-600">*</span>
					{/if}
				</SlideToggle>
			</div>
		{:else if entry.type.toLowerCase() === 'json'}
			<div id={entry.key} on:mouseover={() => helpStore.show(entry.key)}>
				<div class="mb-1 font-medium">
					{entry.title}
					{#if entry.required}
						<span class="text-red-600">*</span>
					{/if}
				</div>

				<CodeEditor
					title=""
					id={entry.key}
					initialValue={initialJSONValue}
					actions={false}
					language="json"
					toggle={false}
					bind:value={JSONValue}
					on:save={() => (entry.value = JSON.parse(JSONValue))}
				/>
			</div>
		{:else if entry.type === 'EntryList'}
			<div class="my-3" id={entry.key} on:mouseover={() => helpStore.show(entry.key)}>
				<span class="h3">
					{entry.title}
					{#if entry.required}
						<span class="text-red-600">*</span>
					{/if}
					(key: {entry.key})
				</span>

				{#each Object.values(entry.value) as e, index}
					<div class="flex card p-2">
						<div class="grow">
							<svelte:self entry={e} isChild={true} {validationErrors} />
						</div>

						<div>
							{#if Object.values(entry.value).length > 1}
								<button
									class="btn variant-filled-error flex-none"
									type="button"
									on:click={() => removeItem(index)}
								>
									<Fa icon={faTrash} />
								</button>
							{/if}
						</div>
					</div>
				{/each}

				<button class="btn variant-filled-primary" type="button" on:click={addItem}>
					<Fa icon={faAdd} />
				</button>
			</div>
		{/if}
	</div>

	{#if hasError}
		<div class="text-red-600 text-sm mt-1">This field is required.</div>
	{/if}

	{#if isChild}
		<TextInput label="Description" bind:value={entry.description} on:input />
	{/if}
</div>