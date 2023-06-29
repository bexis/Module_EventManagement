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
using BExIS.Emm.Services.Event;
using BExIS.IO;
using System.IO;
using System.Xml;
using BExIS.Emm.Entities.Event;
using System.Text;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Xml.Helpers;
using Vaiona.Utils.Cfg;
using System;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Objects;
using BExIS.Security.Entities.Authorization;

namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventController : Controller
    {

        public ActionResult EventManager()
        {

            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Manage Events", this.Session.GetTenant());

            using (EventManager eManger = new EventManager())
            using (var eventRegistrationManager = new EventRegistrationManager())
            {

                List<EventModel> model = new List<EventModel>();
                List<Event> data = eManger.GetAllEvents().ToList();

                foreach (Event e in data)
                {
                    EventModel m = new EventModel(e);
                    List<EventRegistration> eventRegistrations = eventRegistrationManager.GetAllRegistrationsByEvent(e.Id);
                    if (eventRegistrations.Count > 0)
                        m.InUse = true;
                    else
                        m.InUse = false;

                    model.Add(m);
                }

                //data.ToList().ForEach(r => model.Add(new EventModel(r)));

                return View("EventManager", model);
            }
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
            using (EventManager eManger = new EventManager())
            {
                EventModel model = new EventModel(eManger.GetEventById(id));
                model.MetadataStructureList = GetMetadataStructureList();
                model.EditMode = true;

                return View("EditEvent", model);
            }
        }

        public ActionResult Delete(long id)
        {
            using (EventManager eManger = new EventManager())
            {
                eManger.DeleteEvent(eManger.GetEventById(id));
            }

            return RedirectToAction("EventManager");
        }

        [HttpPost]
        public ActionResult Save(EventModel model, HttpPostedFileBase file)
        {
            XmlDocument schema = new XmlDocument();
            string schemaFileName = "";

            /** Event Validation -------------------------------- **/

            if (model.Name == null)
                ModelState.AddModelError("Name", "Name is required.");

            if (model.LogInPassword == null)
                ModelState.AddModelError("LogInPassword", "Login password is required.");

            if (model.StartDate > model.Deadline)
                ModelState.AddModelError("StartDate", "Start date needs to be before deadline.");

            if (model.EventDate == null)
                ModelState.AddModelError("EventDate", "Event time period is required.");

            if (model.SelectedEventLanguage == null)
                ModelState.AddModelError("EventLanguage", "Event language is required.");

            if (model.ImportantInformation == null)
                ModelState.AddModelError("ImportantInformation", "Important information is required.");
            
            if(model.ParticipantsLimitation == 0 && model.WaitingList == true)
                ModelState.AddModelError("WaitingList", "You don't need a waiting list if there is no participants limitation.");

            if (model.WaitingListLimitation > 0 && model.WaitingList == false)
                ModelState.AddModelError("WaitingListLimitation", "You don't need a waiting list limitation if you dont use the waiting list.");


            //check if schema file is uploaded
            //if (attachments ==null &&model.Id == 0)
            //    ModelState.AddModelError("Schema", "Schema is required.");

            /** Event Validation End -------------------------------- **/

            if (ModelState.IsValid)
            {
                using (MetadataStructureManager mManager = new MetadataStructureManager())
                using (EventManager eManager = new EventManager())
                {
                    MetadataStructure ms = mManager.Repo.Get(model.MetadataStructureId);

                    if (model.Id == 0)
                    {
                        Event newEvent = eManager.CreateEvent(model.Name, model.EventDate, model.ImportantInformation, model.Location, model.MailInformation, model.SelectedEventLanguage, model.StartDate, model.Deadline, model.ParticipantsLimitation, model.WaitingList,model.WaitingListLimitation, model.EditAllowed, model.Closed, model.LogInPassword, model.EmailBCC, model.EmailCC, model.EmailReply, ms, null);

                        newEvent = SaveFile(file, newEvent, eManager);
                        eManager.UpdateEvent(newEvent);

                        //add security
                        using (var groupManager = new GroupManager())
                        using (var entityTypeManager = new EntityManager())
                        using (EntityPermissionManager pManager = new EntityPermissionManager())
                        {
                            Entity entityType = entityTypeManager.FindByName("Event");
                            string[] eventAdminGroups = Helper.Settings.get("EventAdminGroups").ToString().Split(',');
                            if (eventAdminGroups != null && eventAdminGroups.Length > 0)
                            {
                                foreach(var g in eventAdminGroups)
                                {
                                    int fullRights = (int)RightType.Read + (int)RightType.Write + (int)RightType.Delete + (int)RightType.Grant;
                                    var group = groupManager.FindByNameAsync(g).Result;
                                    if (group != null)
                                    {
                                        if (pManager.GetRights(group.Id, entityType.Id, newEvent.Id) == 0)
                                            pManager.Create(group.Id, entityType.Id, newEvent.Id, fullRights);
                                    }

                                }
                            }
                        }
                    }
                    else
                    {
                        Event e = eManager.GetEventById(model.Id);
                        e.Name = model.Name;
                        e.EventDate = model.EventDate;
                        e.ImportantInformation = model.ImportantInformation;
                        e.MailInformation = model.MailInformation;
                        e.Location = model.Location;
                        e.EventLanguage = model.SelectedEventLanguage;
                        e.StartDate = model.StartDate;
                        e.Deadline = model.Deadline;
                        e.ParticipantsLimitation = model.ParticipantsLimitation;
                        e.WaitingList = model.WaitingList;
                        e.WaitingListLimitation = model.WaitingListLimitation;
                        e.EditAllowed = model.EditAllowed;
                        e.Closed = model.Closed;
                        e.LogInPassword = model.LogInPassword;
                        e.LogInPassword = model.LogInPassword;
                        e.EmailCC = model.EmailCC;
                        e.EmailBCC = model.EmailBCC;
                        e.EmailReply = model.EmailReply;
                        e.MetadataStructure = ms;

                        e = SaveFile(file, e, eManager);
                        eManager.UpdateEvent(e);
                    }

                    return RedirectToAction("EventManager");
                }
            }
            else
                model.MetadataStructureList = GetMetadataStructureList();
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

        private Event SaveFile(HttpPostedFileBase file, Event e, EventManager eManager)
        {
            string filename = "ext.js";
            string path = Path.Combine(AppConfiguration.DataPath, "MetadataStructures", e.MetadataStructure.Id.ToString(), filename);



            if (System.IO.File.Exists(path) && file != null && file.ContentLength > 0)
            {
                var deletedFilePath = Path.Combine(AppConfiguration.DataPath, "EMM\\Deleted JS Files");
                BExIS.IO.FileHelper.CreateDicrectoriesIfNotExist(deletedFilePath);

                var des = deletedFilePath + "\\" + Path.GetFileName(path);

                // Check if file already exists in the "Deleted Files" folder and rename the file if yes.
                if (System.IO.File.Exists(des))
                {
                    des = deletedFilePath + "\\" + new Random().Next(1, 1000) + "_" + Path.GetFileName(path);
                }

                System.IO.File.Move(path, des);
            }

            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    file.SaveAs(path);
                    e.JavaScriptPath = path;
                }
                catch { }
            }

            return e;
        }

        public List<ListItem> GetMetadataStructureList()
        {
            MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
            XmlDatasetHelper xmlDatasetHelper = new XmlDatasetHelper();
            try
            {

                IEnumerable<MetadataStructure> metadataStructureList = metadataStructureManager.Repo.Get();

                List<ListItem> temp = new List<ListItem>();

                foreach (MetadataStructure metadataStructure in metadataStructureList)
                {
                    if (xmlDatasetHelper.IsActive(metadataStructure.Id) &&
                        xmlDatasetHelper.HasEntityType(metadataStructure.Id, "BExIS.Emm.Entities.Event.Event"))
                    {
                        string title = metadataStructure.Name;

                        temp.Add(new ListItem(metadataStructure.Id, title));
                    }
                }

                return temp.OrderBy(p => p.Name).ToList();
            }
            finally
            {
                metadataStructureManager.Dispose();
            }
        }

        #endregion
    }
}