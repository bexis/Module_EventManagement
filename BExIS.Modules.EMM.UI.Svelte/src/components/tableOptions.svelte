<script lang="ts">
	import Fa from 'svelte-fa';
	import { faPlus, faEye, faPen, faTrash } from '@fortawesome/free-solid-svg-icons';

	export let row;
	export let dispatchFn;

	   const eventDispatchFn = (type: string) => {
		console.log('Dispatching event:', type, 'for row:', row);
		return dispatchFn({ type, row });

	};

	const buttons = [
		// {
		// 	icon: faPlus,
		// 	color: 'variant-filled-primary',
		// 	type: 'CREATE'
		// },
		// {
		// 	icon: faEye,
		// 	color: 'variant-filled-secondary',
		// 	type: 'READ'
		// },
		{
			icon: faPen,
			color: 'variant-filled-warning',
			type: 'UPDATE'
		},
		{
			icon: faTrash,
			color: 'variant-filled-error',
			type: 'DELETE'
		}
	];
</script>

<div class="flex gap-2 w-min">
	{#each buttons as button}
		<button title="{button.type.toLowerCase()}"
			class={`btn btn-sm btn-icon rounded-md ${button.color}`}
			on:click|preventDefault={() => eventDispatchFn(button.type)}
		>
			<Fa icon={button.icon} />
		</button>
	{/each}
</div>