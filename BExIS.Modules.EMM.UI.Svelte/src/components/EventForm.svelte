<script lang="ts">
  import { TextInput, TextArea, NumberInput, DropdownKVP, DateInput } from '@bexis2/bexis2-core-ui';
  import { SlideToggle } from '@skeletonlabs/skeleton';
    import type { EditEvent } from '../models/eventModels';
  import { Page, pageContentLayoutType, MultiSelect } from '@bexis2/bexis2-core-ui';
  export let event: EditEvent;
  export let languages: { id: number; text: string }[] = [];

  export let selectedFile: File | null = null;
  export let loading: boolean = false;
  export let onFileChange: (file: File) => void = () => {};
  export let onSubmit: () => void = () => {};
  	import Fa from 'svelte-fa';
	import { faSave } from '@fortawesome/free-solid-svg-icons';



 
</script>

<Page >

<form on:submit|preventDefault={onSubmit}>
  
  <div class="grid grid-cols-1 gap-4">
    <TextInput
      label="Name"
      placeholder="Enter event name"
      bind:value={event.name}
      required
    />
    <TextInput
      label="Event time period and time"
      placeholder="Event time period and time"
      bind:value={event.eventDate}
    />
    <TextInput
      label="Location"
      placeholder="Location"
      bind:value={event.location}
    />
    <TextArea
      label="Important information"
      placeholder="Important information"
      bind:value={event.importantInformation}
    />
    <TextArea
      label="Additional Mail information"
      placeholder="Additional Mail information"
      bind:value={event.mailInformation}
    />
    <!-- <DropdownKVP
      id="eventLanguage"
      title="SelectedEventLanguage"
      bind:target
      source={languages}
      required={true}
    /> -->

    <label for="eventLanguage">Language</label>
    <select
      id="eventLanguage"
      bind:value={event.selectedEventLanguage}
      required
    >
      {#each languages as lang}
        <option value={lang.text}>{lang.text}</option>
      {/each}
    </select>
	
    <DateInput id="deadline" label="Deadline" required={true} bind:value={event.deadline} />
    <DateInput id="startdate" label="Start date" required={true} bind:value={event.startDate} />
    <NumberInput
      label="Participants limitation"
      placeholder="Enter max participants"
      bind:value={event.participantsLimitation}
    />
    <SlideToggle name="allowWaitingList" bind:checked={event.waitingList} on:change>
      Allow waiting list
    </SlideToggle>
    <NumberInput
      label="Waiting list limitation"
      placeholder="Waiting list limitation"
      bind:value={event.waitingListLimitation}
    />
    <SlideToggle name="allowEdit" bind:checked={event.editAllowed} on:change>
      Allow edit
    </SlideToggle>
    <TextInput
      label="Event password"
      placeholder="Event password"
      bind:value={event.logInPassword}
    />

    <label for="jsonfile">JSON File</label>
    <input
      id="jsonfile"
      type="file"
      accept=".json"
      on:change={(e) => {
        const input = e.target;
        if (input && 'files' in input && input.files && input.files[0]) {
          onFileChange(input.files[0]);
        }
      }}
    />
    {#if selectedFile}
      <p>Datei ausgewählt: {selectedFile.name}</p>
    {/if}

    {#if event.jsonFile}
      <div class="mt-2 p-2 border rounded bg-gray-50">
        <strong>Aktuelles JSON:</strong>
        <pre style="max-height: 300px; overflow:auto; background: #f8f8f8; border-radius: 4px; padding: 8px;">{typeof event.jsonFile === 'string' ? event.jsonFile : JSON.stringify(event.jsonFile, null, 2)}</pre>
      </div>
    {/if}

    <TextInput
      label="CC email addresses (split by ,)"
      placeholder="CC email addresses (split by ,)"
      bind:value={event.emailCC}
    />
    <TextInput
      label="BCC email addresses (split by ,)"
      placeholder="BCC email addresses (split by ,)"
      bind:value={event.emailBCC}
    />
    <TextInput
      label="Reply to mail address"
      placeholder="Reply to mail address"
      bind:value={event.emailReply}
    />
    <SlideToggle name="closed" bind:checked={event.closed} on:change>
      Closed
    </SlideToggle>

    <div class="flex justify-end mt-4">
 <button class="btn variant-filled-primary h-9 w-16 shadow-md" type="submit">
          <Fa icon={faSave} />
        </button>
      
    </div>


  </div>
</form>
</Page>