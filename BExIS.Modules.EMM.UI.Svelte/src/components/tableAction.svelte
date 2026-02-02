<script lang="ts">
	export let row;
	export let dispatchFn;

	const handleRegister = () => dispatchFn({ type: 'REGISTER', row });
	const handleEdit = () => dispatchFn({ type: 'EDIT', row });
	const handleDelete = () => dispatchFn({ type: 'DELETE', row });
</script>

{#if !row.alreadyRegistered}
	<button 
		class="btn btn-sm variant-filled-secondary" 
		on:click|preventDefault={handleRegister}>
		Register
	</button>
{:else}
  <div class="flex gap-2">
    {#if row.editAllowed}
    <button title="edit"
      class="btn btn-sm variant-filled-secondary" 
      on:click|preventDefault={handleEdit}>
      Edit
    </button>
    {/if}
    <button title="delete"
      class="btn btn-sm variant-filled-error"
      on:click|preventDefault={handleDelete}>
      Delete
    </button>
  </div>
{/if}
