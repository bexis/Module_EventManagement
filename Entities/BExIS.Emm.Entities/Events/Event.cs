using BExIS.Dlm.Entities.MetadataStructure;
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

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime Deadline { get; set; }

        public virtual int ParticipantsLimitation { get; set; }

        public virtual bool EditAllowed { get; set; }

        public virtual string LogInPassword { get; set; }


        #endregion

        #region Associations

        public virtual MetadataStructure MetadataStructure { get; set; }

        #endregion


        #region Methods



        #endregion
    }
}
