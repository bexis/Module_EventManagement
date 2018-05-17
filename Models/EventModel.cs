using BExIS.Emm.Entities.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace BExIS.Web.Shell.Areas.EMM.Models
{
    public class EventModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public XmlDocument Schema { get; set; }

        public string SchemaFileName { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime Deadline { get; set; }

        public int ParticipantsLimitation { get; set; }

        public bool EditAllowed { get; set; }

        public bool EditMode { get; set; }

        public string LogInName { get; set; }

        public string LogInPassword { get; set; }

        public bool InUse { get; set; }

        public bool DeleteAccess { get; set; }

        public bool EditAccess { get; set; }

        public List<ListItem> MetadataStructureList { get; set; }

        public long MetadataStructureId { get; set; }

        public EventModel()
        {
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
            StartDate = eEvent.StartDate;
            Deadline = eEvent.Deadline;
            ParticipantsLimitation = eEvent.ParticipantsLimitation;

            //if (eEvent.ParticipantsLimitation == 0)
            //    ParticipantsLimitation = "no limitation";
            //else
            //    ParticipantsLimitation = eEvent.ParticipantsLimitation.ToString();

            EditAllowed = eEvent.EditAllowed;
            LogInName = eEvent.LogInName;
            LogInPassword = eEvent.LogInPassword;

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
    }
}