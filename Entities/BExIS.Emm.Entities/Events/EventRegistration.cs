using BExIS.Security.Entities.Subjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vaiona.Entities.Common;

namespace BExIS.Emm.Entities.Event
{
    public class EventRegistration : BusinessEntity
    {
        #region Attributes

        public virtual XmlDocument Data { get; set; }

        public virtual bool Deleted { get; set; }

        public virtual string Token { get; set; }


        #endregion

        #region Associations

        public virtual User Person { get; set; }
        public virtual Event Event { get; set; }


        #endregion


        #region Methods



        #endregion
    }
}

