# Event Management Module (EMM)
Customizable module to manage event registrations (e.g. workshops, meetings).


1. [Features](#Features)
2. [How to use / Workflow](#how_to)
    1. [Event creation](#event_creation)
    2. [Event registration](#event_reg)
3. [Installation](#install)

## Features<a name="features"></a>
- registration form is fully flexible
- no user account in BExIS needed for registration
- deadline for registration
- participants limitation / waiting list
- option to update registration before the deadline by participants 
- notification and overview for event organizer
- registration secured by password

## How to use / Workflow<a name="how_to"></a>

### Event creation<a name="event_creation"></a>
1. Create XSD for event 
   - *FirstName* and *LastName* are required fields (will be used for salutation in emails)
2. Upload the XSD (Admin->Manage Metadate Structure) and select "Event" as Class
3. Create / Adjust JavAScript file to add customized field checkes (e.g. departure date is not before arrival)
4. Create event (Admin->Manage Event - Create new Event)
   - Provide a meaningful name for the event. The event name should contain the time (at least the year)
   - *Start date* defines the begin of the registration
   - *Deadline* defines the end of the registration (and for edits)
   - *Participants limitation* maximum number of allowed participents (if the number is exeeded registration is still allowed, but persons are on the waiting list)
   - *Allow edit* If true, participants can change registration details until deadline
   - *Event password* the password which is requried to register for the event
   - *JavaScriptFile* upload the js file with rules (need to fit defined XSD elements)
   - *CC/BCC email addresses (split by ,)* list of email adresses which should recieve registration information
   - *Reply to mail address* mail adress for the reply  
   
### Event registration<a name="event_reg"></a>


## Installation <a name="install"></a>
This module requires seed data.
- Entity type: "Event"

https://github.com/bexis/Module_EventManagement/blob/master/Helper/EMMSeedDataGenerator.cs
