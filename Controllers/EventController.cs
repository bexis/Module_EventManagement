using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Telerik.Web.Mvc;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Entities.Objects;
using Vaiona.Web.Mvc.Models;
using Vaiona.Model;
using Vaiona.Web.Extensions;
using BExIS.Web.Shell.Areas.EMM.Models;
using BExIS.Emm.Services.Event;
using System.IO;
using System.Xml;
using BExIS.Emm.Entities.Event;
using System.Text;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Dlm.Entities.MetadataStructure;

namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventController : Controller
    {
        public ActionResult EventManager()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Manage Events", this.Session.GetTenant());
            EventManager eManger = new EventManager();

            List<EventModel> model = new List<EventModel>();
            List<Event> data = eManger.GetAllEvents().ToList();

            data.ToList().ForEach(r => model.Add(new EventModel(r)));

            return View("EventManager", model);
        }

        [GridAction]
        public ActionResult AllEvents()
        {
            EventManager eManger = new EventManager();

            List<EventModel> model = new List<EventModel>();
            List<Event> data = eManger.GetAllEvents().ToList();

            data.ToList().ForEach(r => model.Add(new EventModel(r)));

            return View("EventManager", new GridModel<EventModel> { Data = model });

        }

        #region Create, Edit Delete ans Save Event

        public ActionResult Create()
        {
            EventModel model = new EventModel();
            model.MetadataStructureList = GetMetadataStructureList();
            return View("EditEvent", model);
        }

        public ActionResult Edit(long id)
        {
            EventManager eManger = new EventManager();
            EventModel model = new EventModel(eManger.GetEventById(id));
            model.MetadataStructureList = GetMetadataStructureList();
            model.EditMode = true;

            return View("EditEvent", model);
        }

        public ActionResult Delete(long id)
        {
            EventManager eManger = new EventManager();
            eManger.DeleteEvent(eManger.GetEventById(id));

            return RedirectToAction("EventManager");
        }

        public ActionResult Save(EventModel model)
        {
            XmlDocument schema = new XmlDocument();
            string schemaFileName = "";

            /** Event Validation -------------------------------- **/

            if (model.Name == null)
                ModelState.AddModelError("Name", "Name is required.");

            if (model.LogInName == null)
                ModelState.AddModelError("LogInName", "Login name is required.");

            if (model.LogInPassword == null)
                ModelState.AddModelError("LogInPassword", "Login password is required.");

            //check if schema file is uploaded
            //if (attachments ==null &&model.Id == 0)
            //    ModelState.AddModelError("Schema", "Schema is required.");

            /** Event Validation End -------------------------------- **/

            if (ModelState.IsValid)
            {
                MetadataStructureManager mManager = new MetadataStructureManager();
                MetadataStructure ms = mManager.Repo.Get(model.MetadataStructureId);
                EventManager eManager = new EventManager();
                if (model.Id == 0)
                    eManager.CreateEvent(model.Name, model.StartDate, model.Deadline, model.ParticipantsLimitation, model.EditAllowed, model.LogInName, model.LogInPassword, ms);
                else
                {
                    Event e = eManager.GetEventById(model.Id);
                    e.Name = model.Name;
                    e.StartDate = model.StartDate;
                    e.Deadline = model.Deadline;
                    e.ParticipantsLimitation = model.ParticipantsLimitation;
                    e.EditAllowed = model.EditAllowed;
                    e.LogInName = model.LogInName;
                    e.LogInPassword = model.LogInPassword;
                    e.MetadataStructure = ms;

                    eManager.UpdateEvent(e);
                }

                return View("EventManager");
            }
            else
                return View("EditEvent", model);

        }

        //public ActionResult DownloadSchemaFile(long id)
        //{
        //    EventManager eManager = new EventManager();
        //    Event e = eManager.GetEventById(id);

        //    byte[] bytes = Encoding.Default.GetBytes(e.Schema.OuterXml);

        //    return File(bytes, "application/octet-stream", e.SchemaFileName);
        //}

        #endregion


        #region helpers

        private List<ListItem> GetMetadataStructureList()
        {
            List<ListItem> tmp = new List<ListItem>();

            MetadataStructureManager metadataStructureManager = new MetadataStructureManager();

            foreach (var item in metadataStructureManager.Repo.Get())
            {
                tmp.Add(new ListItem()
                {
                    Id = item.Id,
                    Name = item.Name
                });

            }
            return tmp;
        }

        #endregion
    }
}