using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml;

namespace BExIS.Modules.EMM.UI.Models
{

    /// <summary>
    /// The EventRegistrationModel represent all informatio ehich are needed to handel a event registration.
    /// </summary>
    /// <remarks></remarks>
    public class EventRegistrationModel
    {
        public EventModel Event { get; set; }

        public string Participants { get; set; } //number of participants limitation

        public int NumberOfRegistration { get; set; } //number of already registered participants

        public bool EditAllowed { get; set; } // edit allowed by user

        public bool AlreadyRegistered { get; set; } //user already registered, this will find out via user email

        public bool Deleted { get; set; } //is registration deleted by user

        public string AlreadyRegisteredRefId { get; set; } //Already registered RefId

        public string Message { get; set; } //

        public EventRegistrationModel()
        {
            Event = new EventModel();
        }

        public EventRegistrationModel(Event e)
        {
            Event = new EventModel(e);
            EditAllowed = e.EditAllowed;

            using (EventRegistrationManager erManger = new EventRegistrationManager())
            {
                NumberOfRegistration = erManger.GetAllRegistrationsByEvent(e.Id).Count();
            }
        }
    }

    public class DefaultEventInformation
    {
        //event default values
        public string EventName { get; set; }
        public string Date { get; set; }
        public string Language { get; set; }
        public string ImportantInformation { get; set; }

        public DefaultEventInformation()
        {

        }

    }

    public class LogInToEventModel
    {

        public long EventId { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Event password")]
        public string LogInPassword { get; set; }

        public bool Edit { get; set; }
        public bool ViewOnly { get; set; }

        public string RefId { get; set; }

        public LogInToEventModel()
        {

        }

        public LogInToEventModel(long id, bool view_only = false, string ref_id = null)
        {
            EventId = id;
            ViewOnly = view_only;
            RefId = ref_id;
        }

    }

    public class EventRegistrationResultModel
    {
        public long EventId { get; set; }
        public XmlDocument Form { get; set; }
        public DataTable Results { get; set; }
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