<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/state';
  import EventForm from '../../../components/EventForm.svelte';
  import type { EditEvent } from '../../../models/eventModels';
  import * as dataCaller from '../../../services/eventCaller';
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
  let languages: string[] = ['English', 'German'];

  let selectedFile: File | null = null;
  let loading = false;

  const eventId = Number(page.url.searchParams.get('id'));
  $: target = languages.find(l => l === event.selectedEventLanguage) ?? languages[0];

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
    event.selectedEventLanguage = languages[0];
  }
  target = languages.find(l => l === event.selectedEventLanguage) ?? languages[0];
}
  loading = false;
});

  function onFileChange(file: File) {
    selectedFile = file;
    event.jsonFile = '';
  }

    function onCancel() {
    goto('/emm/events');
  }

  async function handleSubmit() {
     console.log(event.selectedEventLanguage);
    await dataCaller.updateEvent(event);
   

    goto('/emm/events');
  }
</script>

{#if !loading}
  <EventForm
  {event}
  {languages}
  {selectedFile}
  {loading}
  onFileChange={onFileChange}
  onSubmit={handleSubmit}
   onCancel={onCancel}
/>
{:else}
  <p>Lade Eventdaten ...</p>
{/if}