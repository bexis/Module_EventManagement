<script lang="ts">
	import Fa from 'svelte-fa';
	import { faPen, faTrash, faShareSquare } from '@fortawesome/free-solid-svg-icons';

	export let row;
	export let dispatchFn;

	   const eventDispatchFn = (type: string) => {
		console.log('Dispatching event:', type, 'for row:', row);
		return dispatchFn({ type, row });

	};

	const buttons = [
		{
			icon: faPen,
			color: 'variant-filled-primary',
			type: 'EDIT'
		},
		{
			icon: faTrash,
			color: 'variant-filled-error',
			type: 'DELETE'
		},
		{
			icon: faShareSquare,
			color: 'variant-filled-secondary',
			type: 'RESEND'
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