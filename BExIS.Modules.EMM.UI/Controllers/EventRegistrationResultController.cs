using BExIS.App.Bootstrap.Attributes;
using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.Wizard;
using BExIS.Dim.Entities.Export.GBIF;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.IO.Transform.Output;
using BExIS.Modules.EMM.UI.Helper;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Authorization;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Objects;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Utilities;
using BExIS.UI.Helpers;
using BExIS.Xml.Helpers;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;
using Entry = BExIS.Modules.EMM.UI.Models.Entry;

namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventRegistrationResultController : Controller
    {
        private readonly UserManager _userManager;

        public EventRegistrationResultController(UserManager userManager)
        {
            _userManager = userManager;
        }

        public ActionResult Index()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Citation Tool", this.Session.GetTenant());
            string module = "EMM";

            ViewData["app"] = SvelteHelper.GetApp(module);
            ViewData["start"] = SvelteHelper.GetStart(module);

            return View();
        }

        public ActionResult Show()
        {
            string module = "EMM";

            ViewData["app"] = SvelteHelper.GetApp(module);
            ViewData["start"] = SvelteHelper.GetStart(module);

            return View();
        }



        #region Show Event Registration Results

        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEvents()
        {
            using (EventManager eManger = new EventManager())
            {
                List<Event> allEvents = eManger.GetAllEvents().ToList();

                List<EventResultListModel> availableEvents = new List<EventResultListModel>();

                allEvents.ForEach(e => availableEvents.Add(new EventResultListModel(e)));

                return Json(availableEvents, JsonRequestBehavior.AllowGet);
            }
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEventRegistrations(long id)
        {
            using (EventManager eManger = new EventManager())
            using (EventRegistrationManager eventRegistrationManager = new EventRegistrationManager())
            {
                var e = eManger.GetEventById(id);

                var registrations = eventRegistrationManager
                        .GetAllRegistrationsByEvent(id)
                        .Select(r =>
                            {
                                var root = JObject.Parse(r.Data);

                            // Neues Feld "id" auf Root-Ebene hinzufügen
                            root["id"] = r.Id;

                            return root.ToString(); // JSON mit Root-ID
                            });
                var merged = mergeForTable(registrations);

                EventRegistrationsModel model = new EventRegistrationsModel();
                model.EventId = id;
                model.JsonFiles = merged;

                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetWaitingListRegistrations(long id)
        {
            using (EventManager eManger = new EventManager())
            using (EventRegistrationManager eventRegistrationManager = new EventRegistrationManager())
            {
                var e = eManger.GetEventById(id);
                var registrations = eventRegistrationManager.GetAllWaitingListRegsByEvent(id).Select(r => r.Data);
                if (registrations.Count() > 0)
                {
                    var merged = mergeForTable(registrations);
                    EventRegistrationsModel model = new EventRegistrationsModel();
                    model.EventId = id;
                    model.JsonFiles = merged;

                    return Json(model, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult Delete(long id)
        {
             using (EventRegistrationManager erManager = new EventRegistrationManager())
             using (var eventManager = new EventManager())
             {
                EventRegistration reg = erManager.EventRegistrationRepo.Get(a => a.Id == id).FirstOrDefault();
                if (reg != null)
                {
                    reg.Deleted = true;
                    erManager.UpdateEventRegistration(reg);
                    MoveFromWaitingList(reg.Event.Id);
                }

                string url = Request.Url.GetLeftPart(UriPartial.Authority);
                string email = "";

                if (reg.Person != null)
                {
                    User user = _userManager.FindByIdAsync(reg.Person.Id).Result;
                    email = user.Email;
                }
                else
                {
                    JsonEventModel model = (JsonEventModel)JsonConvert.DeserializeObject(reg.Data);

                    EmailStructure emailStructure = new EmailStructure();
                    emailStructure = EmailHelper.ReadFile(reg.Event.EventLanguage);
                    email = model.Registration[1].Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
                }

                EmailHelper.SendEmailNotification("deleted", email, "", reg.Data, reg.Event, url);
            }

            return Json(new { success = true, id = id });
        }

        /// <summary>
        /// delete event with all registrations
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns></returns>
        [JsonNetFilter]
        [HttpGet]
        public JsonResult DeleteAll(long id)
        {
            using (EventManager eManger = new EventManager())
            using (EventRegistrationManager eventRegistrationManager = new EventRegistrationManager())
            {
                var e = eManger.GetEventById(id);

                var registrations = eventRegistrationManager.GetAllRegistrationsByEvent(id);
                if(registrations.Count()> 0)
                    registrations.ForEach(a => eventRegistrationManager.DeleteEventRegistration(a));

                eManger.DeleteEvent(eManger.GetEventById(id));

                return Json(true, JsonRequestBehavior.AllowGet);
            }
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult ResendNotification(long id)
        {
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (EventManager eventManager = new EventManager())
            {
                var registration = erManager.EventRegistrationRepo.Get(a => a.Id == id).FirstOrDefault();

                var e = eventManager.GetEventById(registration.Event.Id);
                Resend(registration.Data, e);
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult MoveFromWaitingList(long id, long eventId)
        {
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (EventManager eventManager = new EventManager())
            {
                var registration = erManager.EventRegistrationRepo.Get(a => a.Id == id).FirstOrDefault();
                if (registration.WaitingList == true)
                    registration.WaitingList = false;

                erManager.UpdateEventRegistration(registration);

                var e = eventManager.GetEventById(eventId);
                SendNotification(registration.Data, e);

            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// clear, that means delete all registrations from one event
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns></returns>
        [JsonNetFilter]
        [HttpGet]
        public JsonResult Clear(long id)
        {
            using (var eventRegistrationManager = new EventRegistrationManager())
            using (var eventManager = new EventManager())
            {
                //delete first all registrations
                List<EventRegistration> eventRegistrations = eventRegistrationManager.GetAllRegistrationsByEvent(id);
                eventRegistrations.ForEach(a => eventRegistrationManager.DeleteEventRegistration(a));

                var e = eventManager.GetEventById(id);
                if (e.Closed == true)
                {
                    e.Closed = false;
                    eventManager.UpdateEvent(e);
                }
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private void Resend(string data, Event e)
        {

            var model = JsonConvert.DeserializeObject<Dictionary<string, List<Registration>>>(data)["registration"];

            EmailStructure emailStructure = new EmailStructure();
            emailStructure = EmailHelper.ReadFile(e.EventLanguage);
            string email = model[0].Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
            string ref_id = EmailHelper.GetRefIdFromEmail(email);
            string url = Request.Url.GetLeftPart(UriPartial.Authority);
            EmailHelper.SendEmailNotification("resend", email, ref_id, data, e, url);
        }


        private string mergeForTable(IEnumerable<string> jsons)
        {
            if (jsons == null) throw new ArgumentNullException(nameof(jsons));

            var items = new List<JObject>();

            foreach (var s in jsons)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;

                var token = JToken.Parse(s); // erkennt Objekt vs. Array
                switch (token)
                {
                    case JObject obj:
                        items.Add(obj);
                        break;
                    case JArray arr:
                        foreach (var t in arr.OfType<JObject>())
                            items.Add(t);
                        break;
                    default:
                        // Primitive o.ä. ignorieren; alternativ: Ausnahme werfen
                        break;
                }
            }

            // Spalten vereinheitlichen (Union aller Property-Namen)
            var allProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in items)
                foreach (var p in o.Properties()) allProps.Add(p.Name);

            foreach (var o in items)
                foreach (var name in allProps)
                    if (o[name] == null) o[name] = JValue.CreateNull();

            // Als flaches Array für die UI zurückgeben
            return new JArray(items).ToString(Newtonsoft.Json.Formatting.None);
        }

        private void SendNotification(string data, Event e)
        {
            // todo: add not allowed / log in info to mail

            EmailStructure emailStructure = new EmailStructure();
            emailStructure = EmailHelper.ReadFile(e.EventLanguage);

            JsonEventModel model = (JsonEventModel)JsonConvert.DeserializeObject(data);
            string first_name = model.Registration[1].Entries.Where(a => a.Title == emailStructure.lableFirstName).FirstOrDefault().Value;  
            string last_name = model.Registration[1].Entries.Where(a => a.Title == emailStructure.lableLastname).FirstOrDefault().Value;
            string email = model.Registration[1].Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;

            string url = Request.Url.GetLeftPart(UriPartial.Authority);

            string mail_message = "";
            string subject = emailStructure.removeFromWaitingListSubject + e.Name;

            string body = emailStructure.bodyTitle + first_name + " " + last_name + ", " + "<br/><br/>" +
                emailStructure.removeFromWaitingList1 + "<br/><br/>" +
                 emailStructure.bodyClosing + "<br/>" +
                 emailStructure.bodyClosingName;


            using (var es = new EmailService())
            {

                // If no explicit Reply to mail is set use the SystemEmail
                string replyTo = "";
                if (String.IsNullOrEmpty(e.EmailReply))
                {
                    replyTo = ConfigurationManager.AppSettings["SystemEmail"];
                }
                else
                {
                    replyTo = e.EmailReply;
                }

                es.Send(
                    subject,
                    body,
                    new List<string> { email }, // to
                    new List<string> { e.EmailCC }, // CC 
                    new List<string> { ConfigurationManager.AppSettings["SystemEmail"], e.EmailBCC }, // Allways send BCC to SystemEmail + additional set 
                    new List<string> { replyTo }
                    );
            }
        }

        #endregion

        private void MoveFromWaitingList(long eventId)
        {
            string url = Request.Url.GetLeftPart(UriPartial.Authority);

            using (var erManager = new EventRegistrationManager())
            using (var eventManager = new EventManager())
            {
                int countWaitingList = erManager.GetAllWaitingListRegsByEvent(eventId).Count;
                if (countWaitingList > 0)
                {
                    var reg = erManager.GetLatestWaitingListEntry(eventId);
                    reg.WaitingList = false;
                    erManager.UpdateEventRegistration(reg);
                    var e = eventManager.GetEventById(eventId);
                    string email = "";
                    if (reg.Person != null)
                        email = reg.Person.Email;
                    else
                    {
                        JsonEventModel model = (JsonEventModel)JsonConvert.DeserializeObject(reg.Data);

                        EmailStructure emailStructure = new EmailStructure();
                        emailStructure = EmailHelper.ReadFile(reg.Event.EventLanguage);
                        email = model.Registration[1].Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
                    }


                    //change Sataus if event if there is again space on waiting list
                    if ((countWaitingList <= e.WaitingListLimitation) && e.Closed == true)
                    {
                        e.Closed = false;
                        eventManager.UpdateEvent(e);
                    }

                    EmailHelper.SendEmailNotification("remove_from_waiting_list", email, "", reg.Data, reg.Event, url);

                }
            }
        }

    }
}
