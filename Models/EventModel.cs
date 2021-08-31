using BExIS.Emm.Entities.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.ComponentModel;

namespace BExIS.Modules.EMM.UI.Models
{
    public class EventModel
    {
        public long Id { get; set; }

        [DisplayName("Event name")]
        public string Name { get; set; }

        [DisplayName("Event time period and time")]
        public string EventDate { get; set; }

        [DisplayName("Important information")]
        public string ImportantInformation { get; set; }

        [DisplayName("Event language")]
        public List<string> EventLanguages { get; set; }

        public string SelectedEventLanguage { get; set; }

        public XmlDocument Schema { get; set; }

        public string SchemaFileName { get; set; }

        [DisplayName("Start date")]
        public DateTime StartDate { get; set; }

        public DateTime Deadline { get; set; }

        [DisplayName("Participants limitation")]
        public int ParticipantsLimitation { get; set; }

        [DisplayName("Allow edit")]
        public bool EditAllowed { get; set; }

        public bool EditMode { get; set; }

        [DisplayName("Event password")]
        public string LogInPassword { get; set; }

        [DisplayName("BCC email addresses (split by ,)")]
        public string EmailBCC { get; set; }

        [DisplayName("CC email addresses (split by ,)")]
        public string EmailCC { get; set; }

        [DisplayName("Reply to mail address")]
        public string EmailReply { get; set; }

        [DisplayName("JavaScript file")]
        public string JavaScriptPath { get; set; }

        public bool InUse { get; set; }

        public bool DeleteAccess { get; set; }

        public bool EditAccess { get; set; }

        [DisplayName("Registration template")]
        public List<ListItem> MetadataStructureList { get; set; }

        public long MetadataStructureId { get; set; }

        public EventModel()
        {
            EventLanguages = new List<string>() { "English", "Deutsch" };
            Deadline = new DateTime();
            StartDate = new DateTime();
            SchemaFileName = "";
            DeleteAccess = true;
            EditAccess = true;
            ParticipantsLimitation = 0;
            MetadataStructureList = new List<ListItem>();
            MetadataStructureId = 0;

        }

        public EventModel(Event eEvent)
        {
            Id = eEvent.Id;
            Name = eEvent.Name;
            EventDate = eEvent.EventDate;
            ImportantInformation = eEvent.ImportantInformation;
            EventLanguages = new List<string>() { "English", "Deutsch" };
            SelectedEventLanguage = eEvent.EventLanguage;
            StartDate = eEvent.StartDate;
            Deadline = eEvent.Deadline;
            ParticipantsLimitation = eEvent.ParticipantsLimitation;

            //if (eEvent.ParticipantsLimitation == 0)
            //    ParticipantsLimitation = "no limitation";
            //else
            //    ParticipantsLimitation = eEvent.ParticipantsLimitation.ToString();

            EditAllowed = eEvent.EditAllowed;
            LogInPassword = eEvent.LogInPassword;

            EmailBCC = eEvent.EmailBCC;
            EmailCC = eEvent.EmailCC;
            EmailReply = eEvent.EmailReply;

            JavaScriptPath = eEvent.JavaScriptPath;


            DeleteAccess = true;
            EditAccess = true;
            MetadataStructureId = eEvent.MetadataStructure.Id;
            MetadataStructureList = new List<ListItem>();

        }
    }

    public class ListItem
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public ListItem(long id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}