<script lang="ts">
  import { TextInput, TextArea, NumberInput, Dropdown, DateInput } from '@bexis2/bexis2-core-ui';
  import { SlideToggle } from '@skeletonlabs/skeleton';
  import type { EditEvent } from '../models/eventModels';
  import { Page, pageContentLayoutType, MultiSelect } from '@bexis2/bexis2-core-ui';
  import { onMount, onDestroy } from 'svelte';
  import { EditorState } from '@codemirror/state';
  import { EditorView, basicSetup } from 'codemirror';
  import { json } from '@codemirror/lang-json';
  import Fa from 'svelte-fa';
	import { faSave, faXmark } from '@fortawesome/free-solid-svg-icons';

  export let event: EditEvent;
 export let languages: string[] = [];

  export let selectedFile: File | null = null;
  export let loading: boolean = false;
  export let onFileChange: (file: File) => void = () => {};
  export let onSubmit: () => void = () => {};
  export let onCancel: () => void = () => {};

  let jsonEditorContainer: HTMLDivElement;
  let jsonEditor: EditorView | null = null;
  let jsonError = '';

  

 function getJsonString(): string {
    if (!event.jsonFile) {
      return '';
    }

    return typeof event.jsonFile === 'string'
      ? event.jsonFile
      : JSON.stringify(event.jsonFile, null, 2);
  }

function validateJson(value: string): void {
    if (!value.trim()) {
      jsonError = '';
      return;
    }

    try {
      JSON.parse(value);
      jsonError = '';
    } catch (error) {
      jsonError =
        error instanceof Error
          ? `Ungültiges JSON: ${error.message}`
          : 'Das JSON ist nicht gültig.';
    }
  }

 function updateJsonEditor(value: string): void {
    if (!jsonEditor) {
      return;
    }

    const currentValue = jsonEditor.state.doc.toString();

    if (currentValue === value) {
      return;
    }

    jsonEditor.dispatch({
      changes: {
        from: 0,
        to: jsonEditor.state.doc.length,
        insert: value
      }
    });
  }

  async function handleFileChange(file: File): Promise<void> {
    // Übergibt nur die ausgewählte Datei an die übergeordnete Seite.
    onFileChange(file);

    let fileContent: string;

    try {
      fileContent = await file.text();
    } catch (error) {
      jsonError =
        error instanceof Error
          ? `Die Datei konnte nicht gelesen werden: ${error.message}`
          : 'Die ausgewählte Datei konnte nicht gelesen werden.';
      return;
    }

    try {
      const parsedJson = JSON.parse(fileContent);
      const formattedJson = JSON.stringify(parsedJson, null, 2);

      // WICHTIG:
      // Der JSON-String wird direkt in event.jsonFile gespeichert.
      event.jsonFile = formattedJson;

      jsonError = '';

      updateJsonEditor(formattedJson);
    } catch (error) {
      jsonError =
        error instanceof Error
          ? `Ungültiges JSON: ${error.message}`
          : 'Die ausgewählte Datei enthält kein gültiges JSON.';

      event.jsonFile = fileContent;
      updateJsonEditor(fileContent);
    }
  }

   onMount(() => {
    const initialJson = getJsonString();

    validateJson(initialJson);

    jsonEditor = new EditorView({
      parent: jsonEditorContainer,

      state: EditorState.create({
        doc: initialJson,

        extensions: [
          basicSetup,
          json(),
          EditorView.lineWrapping,

          // ==========================================
          // WICHTIG:
          // Jede Editoränderung wird direkt in
          // event.jsonFile gespeichert.
          // ==========================================
          EditorView.updateListener.of((update) => {
            if (!update.docChanged) {
              return;
            }

            const value = update.state.doc.toString();

            event.jsonFile = value;

            validateJson(value);
          }),

          EditorView.theme({
            '&': {
              minHeight: '300px',
              maxHeight: '500px',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              backgroundColor: '#f9fafb'
            },

            '&.cm-focused': {
              outline: '2px solid #2563eb',
              outlineOffset: '1px'
            },

            '.cm-scroller': {
              minHeight: '300px',
              maxHeight: '500px',
              overflow: 'auto'
            },

            '.cm-content': {
              fontFamily: 'Consolas, Monaco, "Courier New", monospace',
              fontSize: '14px',
              padding: '8px'
            },

            '.cm-gutters': {
              backgroundColor: '#f3f4f6',
              borderRight: '1px solid #d1d5db'
            }
          })
        ]
      })
    });
  });

  onDestroy(() => {
    if (jsonEditor) {
      jsonEditor.destroy();
      jsonEditor = null;
    }
  });


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
      required
    />
    <TextInput
      label="Location"
      placeholder="Location"
      bind:value={event.location}
      required
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
    <Dropdown
      id="eventLanguage"
      title="Language"
      bind:target= {event.selectedEventLanguage}
      source={languages}
      required={true}
    />

    <!-- <label for="eventLanguage">Language</label>
    <select
      id="eventLanguage"
      bind:value={event.selectedEventLanguage}
      required = {true}
    >
      {#each languages as lang}
        <option value={lang.text}>{lang.text}</option>
      {/each}
    </select> -->
	
    <DateInput id="deadline" label="Deadline" required={true} bind:value={event.deadline} />
    <DateInput id="startdate" label="Start date" required={true} bind:value={event.startDate} />
    <NumberInput
      label="Participants limitation"
      placeholder="Enter max participants"
      bind:value={event.participantsLimitation}
      min={0}
    />
    <SlideToggle name="allowWaitingList" bind:checked={event.waitingList} on:change>
      Allow waiting list
    </SlideToggle>
    <NumberInput
      label="Waiting list limitation"
      placeholder="Waiting list limitation"
      bind:value={event.waitingListLimitation}
      min={0}
    />
    <SlideToggle name="allowEdit" bind:checked={event.editAllowed} on:change>
      Allow edit
    </SlideToggle>
    <TextInput
      label="Event password"
      placeholder="Event password"
      bind:value={event.logInPassword}
      required
    />

     <div>
        <label for="jsonfile">
          JSON File
        </label>

        <input
          id="jsonfile"
          type="file"
          accept=".json,application/json"
          on:change={(e) => {
            const input = e.currentTarget;

            if (input.files?.[0]) {
              handleFileChange(input.files[0]);
            }
          }}
        />
      </div>

      {#if selectedFile}
        <p class="text-sm">
          Datei ausgewählt:
          <strong>{selectedFile.name}</strong>
        </p>
      {/if}

     <div class="mt-2">
        <div class="mb-2">
          <strong>Aktuelles JSON:</strong>
        </div>

        <div
          bind:this={jsonEditorContainer}
          class="json-editor"
        ></div>

        {#if jsonError}
          <p class="mt-2 text-sm text-red-600">
            {jsonError}
          </p>
        {:else if event.jsonFile}
          <p class="mt-2 text-sm text-green-600">
            Das JSON ist gültig.
          </p>
        {/if}
      </div>

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

    <div class="flex-none text-end">
      <button
        class="btn variant-filled-warning h-9 w-16 shadow-md"
        type="button"
        on:click={onCancel}
      >  
        <Fa icon={faXmark} />
      </button>
      <button class="btn variant-filled-primary h-9 w-16 shadow-md" type="submit">
        <Fa icon={faSave} />
      </button>
      
    </div>


  </div>
</form>
</Page>