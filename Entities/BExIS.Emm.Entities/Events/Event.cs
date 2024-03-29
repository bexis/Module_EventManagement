﻿using BExIS.Dlm.Entities.MetadataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vaiona.Entities.Common;

namespace BExIS.Emm.Entities.Event
{
    public class Event : BusinessEntity
    {
        #region Attributes

        public virtual string Name { get; set; }

        public virtual string EventDate { get; set; }

        public virtual string ImportantInformation { get; set; }

        public virtual string Location { get; set; }

        public virtual string MailInformation { get; set; }

        public virtual string EventLanguage { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime Deadline { get; set; }

        public virtual int ParticipantsLimitation { get; set; }

        public virtual bool WaitingList { get; set; }

        public virtual int WaitingListLimitation { get; set; }

        public virtual bool EditAllowed { get; set; }

        public virtual bool Closed { get; set; }

        public virtual string LogInPassword { get; set; }

        public virtual string XPathToEmail { get; set; }
        public virtual string XPathToFirstName { get; set; }
        public virtual string XPathToLastName { get; set; }

        public virtual string EmailBCC { get; set; }

        public virtual string EmailCC { get; set; }

        public virtual string EmailReply { get; set; }

        public virtual string JavaScriptPath { get; set; }
        #endregion

        #region Associations

        public virtual MetadataStructure MetadataStructure { get; set; }

        #endregion


        #region Methods



        #endregion
    }
}
