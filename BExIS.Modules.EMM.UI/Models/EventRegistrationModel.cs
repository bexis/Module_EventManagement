using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml;
using Vaiona.Utils.Cfg;

namespace BExIS.Modules.EMM.UI.Models
{
    public class EventRegListModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Deadline { get; set; }
        public string Participants { get; set; }
        //public bool EditAllowed { get; set; }
        public bool AlreadyRegistered { get; set; } 

        public bool Closed { get; set; }
        //public bool Deleted { get; set; }

        public EventRegListModel(Event e)
        {
            Id = e.Id;
            Name = e.Name;
            Deadline = e.Deadline.ToString("dd.MM.yyyy");
            //EditAllowed = e.EditAllowed;
            Closed = e.Closed;
            AlreadyRegistered = false;
            Participants = e.ParticipantsLimitation == 0 ? "no limitation" : e.ParticipantsLimitation.ToString();
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

    public class EventRegistrationLoadModel
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Location { get; set; }
        public string Language { get; set; }
        public string ImportantInformation { get; set; }
        public string JsonFile { get; set; }
    }

    ///// <summary>
    ///// Old-> deletd if new works!!!! The EventRegistrationModel represent all information which are needed to handel a event registration.
    ///// </summary>
    ///// <remarks></remarks>
    //public class EventRegistrationModel
    //{
    //    public EventModel Event { get; set; }

    //    /// <summary>
    //    /// Number of participants limitation
    //    /// </summary>
    //    public string Participants { get; set; }

    //    /// <summary>
    //    /// Number of already registered participants
    //    /// </summary>
    //    public int NumberOfRegistration { get; set; }

    //    /// <summary>
    //    /// Number of already registered participants on waiting list
    //    /// </summary>
    //    public int NrOfRegistrationWaitingList { get; set; }

    //    /// <summary>
    //    /// edit allowed by user
    //    /// </summary>
    //    public bool EditAllowed { get; set; }

    //    /// <summary>
    //    /// user already registered, this will find out via user email
    //    /// </summary>
    //    public bool AlreadyRegistered { get; set; }

    //    /// <summary>
    //    /// is registration deleted by user
    //    /// </summary>
    //    public bool Deleted { get; set; }

    //    /// <summary>
    //    ///true if ParticipantsLimitation and WaitingListLimitation is reached
    //    /// </summary>
    //    public bool Closed { get; set; }

    //    /// <summary>
    //    ///Already registered RefId
    //    /// </summary>
    //    public string AlreadyRegisteredRefId { get; set; }

    //    /// <summary>
    //    ///
    //    /// </summary>
    //    //public string Message { get; set; } 

    //    public EventRegistrationModel()
    //    {
    //        Event = new EventModel();
    //    }

    //    public EventRegistrationModel(Event e)
    //    {
    //        Event = new EventModel(e);
    //        EditAllowed = e.EditAllowed;

    //    }
    //}

    public class DefaultEventInformation
    {
        //event default values
        public string EventName { get; set; }
        public string Date { get; set; }
        public string Language { get; set; }
        public string ImportantInformation { get; set; }

        public string Location { get; set; }

        public string Eventid { get; set; }

        public long RegistrationId { get; set; }

        //public string XPathToEmail { get; set; }
        //public string XPathToFirstName { get; set; }
        //public string XPathToLastName { get; set; }

        public string Email { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }

        public string Data { get; set; }

        public DefaultEventInformation()
        {

        }

    }

    //public class UpdateEventModel
    //{
    //    [JsonProperty("description")]
    //    public string Description { get; set; }

    //    [JsonProperty("entries")]
    //    public List<Entry> Entries { get; set; }

    //    [JsonProperty("name")]
    //    public string Name { get; set; }

    //    public static UpdateEventModel Convert(JsonEventModel eventModel)
    //    {
    //        return new UpdateEventModel()
    //        {
    //            Name = eventModel.Name,
    //            Description = eventModel.Description,
    //            Entries = eventModel.Entries
    //        };
    //    }
    //}

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