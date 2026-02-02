using BExIS.Emm.Entities.Event;
using System.Collections.Generic;
using System.Data;
using System.Xml;

namespace BExIS.Modules.EMM.UI.Models
{
    public class EventRegListModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Deadline { get; set; }
        public string Participants { get; set; }
        public bool EditAllowed { get; set; }
        public bool AlreadyRegistered { get; set; } 
        public int NumberOfRegistration { get; set; }
        public bool Closed { get; set; }
        public bool Deleted { get; set; }

        public EventRegListModel(Event e)
        {
            Id = e.Id;
            Name = e.Name;
            Deadline = e.Deadline.ToString("dd.MM.yyyy");
            EditAllowed = e.EditAllowed;
            Closed = e.Closed;
            AlreadyRegistered = false;
            Participants = e.ParticipantsLimitation == 0 ? "no limitation" : e.ParticipantsLimitation.ToString();
        }
    }

    public class EventResultListModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Closed { get; set; }

        public EventResultListModel(Event e)
        {
            Id = e.Id;
            Name = e.Name;
            Closed = e.Closed;
        }

    }

    public class EventRegistrationModel
    {
        public long EventId { get; set; }
        public string JsonFile { get; set; }

        public EventRegistrationModel()
        {           

        }
    }

    public class EventRegistrationsModel
    {
        public long EventId { get; set; }
        public string JsonFiles { get; set; }

        public EventRegistrationsModel()
        {

        }
    }


    public class EventRegistrationLoadModel
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Location { get; set; }
        public string Language { get; set; }
        public string ImportantInformation { get; set; }
        public string JsonFile { get; set; }
    }

    public class EventRegistrationResultModel
    {
        public long EventId { get; set; }
        public XmlDocument Form { get; set; }
        public DataTable Results { get; set; }
        public DataTable WaitingListResults { get; set; }
        public bool UserHasRights { get; set; }

        public EventRegistrationResultModel()
        {
            UserHasRights = false;
        }
    }

    public class EventRegistrationFilterModel
    {
        public string Status { get; set; }
        public List<EventFilterItem> EventFilterItems { get; set; }

        public EventRegistrationFilterModel()
        {

        }
    }

    public class EventFilterItem
    {
        public long Id { get; set; }
        public string Name { get; set; }
        //public bool Closed { get; set; }
        public bool Selected { get; set; }

        public EventFilterItem()
        {

        }

        public EventFilterItem(Event eEvent)
        {
            Id = eEvent.Id;
            Name = eEvent.Name;

            //if (eEvent.Deadline < DateTime.Now)
            //    Closed = true;
        }
    }
}