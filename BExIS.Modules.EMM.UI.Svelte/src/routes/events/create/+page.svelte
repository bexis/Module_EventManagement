<script lang="ts">
  import EventForm from '../../../components/EventForm.svelte';
  import type { EditEvent } from '../../../models/eventModels';
  import * as dataCaller from '../../../services/eventCaller';
  // ... weitere Imports und Logik ...
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
    event.JsonFile = null;
  }

  async function handleSubmit() {
    if (selectedFile) {
      event.JsonFile = await selectedFile.text();
    }
    dataCaller.saveEvent(event)
      .then(() => {
        // Handle success, e.g., navigate to event list or show a success message
        console.log('Event saved successfully');
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
  {target}
  {selectedFile}
  {loading}
  onFileChange={onFileChange}
  onSubmit={handleSubmit}
/>