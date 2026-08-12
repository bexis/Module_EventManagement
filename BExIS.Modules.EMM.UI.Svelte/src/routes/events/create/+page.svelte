<script lang="ts">
  import EventForm from '../../../components/EventForm.svelte';
  import type { EditEvent } from '../../../models/eventModels';
  import * as dataCaller from '../../../services/eventCaller';
  import { goto } from '$app/navigation';
  import { notificationStore, notificationType } from '@bexis2/bexis2-core-ui';

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

  function onFileChange(file: File) {
    selectedFile = file;
    event.jsonFile = "";
  }

  function onCancel() {
    goto('/emm/events');
  }

  async function handleSubmit() {
    if (selectedFile) {
      event.jsonFile = await selectedFile.text();
    }
    dataCaller.saveEvent(event)
      .then(() => {
        notificationStore.showNotification({
          notificationType: notificationType.success,
          message: `Event "${event.name}" saved successfully.`
        });
        goto('/emm/events');
      })
      .catch((error) => {
        // Handle error, e.g., show an error message
        console.error('Error saving event:', error);
      }); 
  }
</script>

<EventForm
  {event}
  {languages}
  {selectedFile}
  {loading}
  onFileChange={onFileChange}
  onSubmit={handleSubmit}
  onCancel={onCancel}
/>