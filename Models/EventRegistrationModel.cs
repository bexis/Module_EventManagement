﻿using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Xml;

namespace BExIS.Web.Shell.Areas.EMM.Models
{
    public class EventRegistrationModel
    {
        public EventModel Event { get; set; }

        public string Participants { get; set; }

        public int NumberOfRegistration { get; set; }

        public bool EditAllowed { get; set; }

        public bool AlreadyRegistered { get; set; }

        public EventRegistrationModel()
        {
            Event = new EventModel();
        }

        public EventRegistrationModel(Event e)
        {
            Event = new EventModel(e);
            EditAllowed = e.EditAllowed;

            EventRegistrationManager erManger = new EventRegistrationManager();
            NumberOfRegistration = erManger.GetAllRegistrationsByEvent(e.Id).Count();
        }
    }

    public class LogInToEventModel
    {

        public long EventId { get; set; }


        [Display(Name = "Login to event")]
        public string LogInName { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string LogInPassword { get; set; }

        public bool Edit { get; set; }


        public LogInToEventModel()
        {

        }

        public LogInToEventModel(long id)
        {
            EventId = id;
        }

    }

    public class EventRegistrationResultModel
    {
        public long EventId { get; set; }
        public XmlDocument Form { get; set; }


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