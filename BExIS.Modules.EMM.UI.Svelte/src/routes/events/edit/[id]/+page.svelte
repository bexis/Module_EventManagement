<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import EventForm from '../../../../components/EventForm.svelte';
  import type { EditEvent } from '../../../../models/eventModels';
  import * as dataCaller from '../../../../services/eventCaller';
      import { goto } from '$app/navigation';
 

  let event: EditEvent = {
  id: 0,
  name: '',
  eventDate: '',
  importantInformation: '',
  location: '',
  mailInformation: '',
  selectedEventLanguage: '',
  jsonFile: '',
  startDate: '',
  deadline: '',
  participantsLimitation: 0,
  waitingList: false,
  waitingListLimitation: 0,
  editAllowed: false,
  closed: false,
  logInPassword: '',
  emailBCC: '',
  emailCC: '',
  emailReply: '',
  javaScriptPath: '',
  inUse: false,
  editAccess: '',
  jsonsKeys: [],
  jsonKeyEmail: '',
  jsonKeyFirstName: '',
  jsonKeyLastName: ''
};
  let languages = [ { id: 1, text: "English" }, { id: 2, text: "German" } ];
  let target = languages[0];
  let selectedFile: File | null = null;
  let loading = false;

  // Event-ID aus URL holen
  import { get } from 'svelte/store';
  let eventId = get(page).params.id;
  $: target = languages.find(l => l.text === event.selectedEventLanguage) ?? languages[0];

 onMount(async () => {
  loading = true;
  const loadedEvent = await dataCaller.getEvent(eventId);
  console.log('API Response:', loadedEvent);

  if (loadedEvent) {
  event = { ...loadedEvent };
  if (event.startDate) {
    event.startDate = new Date(event.startDate).toISOString().slice(0, 10);
  }
  if (event.deadline) {
    event.deadline = new Date(event.deadline).toISOString().slice(0, 10);
  }
  if (!event.selectedEventLanguage || event.selectedEventLanguage === null) {
    event.selectedEventLanguage = languages[0].text;
  }
  target = languages.find(l => l.text === event.selectedEventLanguage) ?? languages[0];
}
  loading = false;
});

  function onFileChange(file: File) {
    selectedFile = file;
    event.jsonFile = null;
  }

  async function handleSubmit() {
    if (selectedFile) {
      event.jsonFile = await selectedFile.text();
    }
    await dataCaller.updateEvent(event);
   

    goto('/events');
  }
</script>

{#if !loading}
  <EventForm
    {event}
    {languages}
    {target}
    {selectedFile}
    {loading}
    onFileChange={onFileChange}
    onSubmit={handleSubmit}
  />
{:else}
  <p>Lade Eventdaten ...</p>
{/if}