using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BExIS.Security.Entities.Objects;
using Vaiona.Web.Mvc.Models;
using Vaiona.Web.Extensions;
using BExIS.Emm.Services.Event;
using BExIS.Emm.Entities.Event;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Objects;
using BExIS.Security.Entities.Authorization;
using Vaiona.Web.Mvc.Modularity;
using BExIS.App.Bootstrap.Attributes;
using BExIS.UI.Helpers;

namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventController : Controller
    {
       
        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEvents()
        {
            using (EventManager eManger = new EventManager())
            using (var eventRegistrationManager = new EventRegistrationManager())
            {
                List<EventListModel> model = new List<EventListModel>();
                List<Event> data = eManger.GetAllEvents().ToList();

                foreach (Event e in data)
                {
                    EventListModel m = new EventListModel(e);
                    List<EventRegistration> eventRegistrations = eventRegistrationManager.GetAllRegistrationsByEvent(e.Id);
                    //if (eventRegistrations.Count > 0)
                    //    m.InUse = true;
                    //else
                    //    m.InUse = false;

                    model.Add(m);
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        #region Create, Edit Delete ans Save Event

        [JsonNetFilter]
        [HttpPost]
        public JsonResult Create(EventModel model)
        {
            if (model == null) return Json(false);

            using (EventManager eManager = new EventManager())
            {
                if (model.Id == 0)
                {
                    Event newEvent = eManager.CreateEvent(model.JsonFile, model.Name, model.EventDate, model.ImportantInformation, model.Location, model.MailInformation, model.SelectedEventLanguage, model.StartDate, model.Deadline, model.ParticipantsLimitation, model.WaitingList, model.WaitingListLimitation, model.EditAllowed, model.Closed, model.LogInPassword, model.EmailBCC, model.EmailCC, model.EmailReply, model.JsonKeyEmail, model.JsonKeyFirstName, model.JsonKeyLastName, null);

                    eManager.UpdateEvent(newEvent);

                    //add security
                    using (var groupManager = new GroupManager())
                    using (var entityTypeManager = new EntityManager())
                    using (EntityPermissionManager pManager = new EntityPermissionManager())
                    {
                        Entity entityType = entityTypeManager.FindByName("Event");
                        var settings = ModuleManager.GetModuleSettings("emm");
                        string[] eventAdminGroups = settings.GetValueByKey("EventAdminGroups").ToString().Split(',');

                        if (eventAdminGroups != null && eventAdminGroups.Length > 0)
                        {
                            foreach (var g in eventAdminGroups)
                            {
                                int fullRights = (int)RightType.Read + (int)RightType.Write + (int)RightType.Delete + (int)RightType.Grant;
                                var group = groupManager.FindByNameAsync(g).Result;
                                if (group != null)
                                {
                                    if (pManager.GetRightsAsync(group.Id, entityType.Id, newEvent.Id).Result == 0)
                                        pManager.CreateAsync(group.Id, entityType.Id, newEvent.Id, fullRights);
                                }
                            }
                        }
                    }
                }
            }

            return Json(new { success = true, id = 0 });
        }


        [JsonNetFilter]
        [HttpPost]
        public JsonResult Update(EventModel model)
        {
            if (model == null) return Json(false);
            if(model.Id != 0)
            {
                using (EventManager eManager = new EventManager())
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
                    e.JsonKeyEmail = model.JsonKeyEmail;
                    e.JsonKeyFirstName = model.JsonKeyFirstName;
                    e.JsonKeyLastName = model.JsonKeyLastName;
                    e.EmailCC = model.EmailCC;
                    e.EmailBCC = model.EmailBCC;
                    e.EmailReply = model.EmailReply;
                    e.Data = model.JsonFile;

                    eManager.UpdateEvent(e);

                }
               
            }
            return Json(new { success = true, id = model.Id });
        }


        [JsonNetFilter]
        [HttpGet]
        public JsonResult Get(long id)
        {
            using (EventManager eManager = new EventManager())
            {
                Event e = eManager.GetEventById(id);
                EventModel model = new EventModel(e);

                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [JsonNetFilter]
        [HttpPost]
        public JsonResult Delete(long id)
        {
            using (EventManager eManger = new EventManager())
            {
                eManger.DeleteEvent(eManger.GetEventById(id));
            }

            return Json(new { success = true, id = id });
        }

        #endregion

        }
    }