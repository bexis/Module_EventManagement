using BExIS.App.Bootstrap.Attributes;
using BExIS.App.Bootstrap.Helpers;
using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dlm.Entities.Party;
using BExIS.Dlm.Services.Party;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.Modules.EMM.UI.Helper;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Utilities;
using BExIS.UI.Helpers;
using BExIS.Utils.Data.MetadataStructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;




namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventRegistrationController : Controller
    {

        public ActionResult Index()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Citation Tool", this.Session.GetTenant());
            string module = "EMM";

            ViewData["app"] = SvelteHelper.GetApp(module);
            ViewData["start"] = SvelteHelper.GetStart(module);

            return View();
        }

        public ActionResult Edit()
        {
            string module = "EMM";

            ViewData["app"] = SvelteHelper.GetApp(module);
            ViewData["start"] = SvelteHelper.GetStart(module);

            return View();
        }

        public ActionResult Create()
        {
            string module = "EMM";

            ViewData["app"] = SvelteHelper.GetApp(module);
            ViewData["start"] = SvelteHelper.GetStart(module);

            return View();
        }


        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEvents(string ref_id = "")
        {
            using (EventManager eManger = new EventManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                List<Event> allEvents = eManger.GetAllEvents().ToList();

                List<EventRegListModel> availableEvents = new List<EventRegListModel>();

                using (EventRegistrationManager erManager = new EventRegistrationManager())
                {
                    User user = BExISAuthorizeHelper.GetUserFromAuthorization(HttpContext);

                    foreach (Event e in allEvents)
                    {
                        DateTime today = DateTime.Now;
                        if (today >= e.StartDate)
                        {
                            EventRegListModel model = new EventRegListModel(e);
                            model.NumberOfRegistration = erManager.GetAllRegistrationsNotDeletedByEvent(e.Id).Count;
                            //model.NrOfRegistrationWaitingList = erManager.GetAllWaitingListRegsByEvent(e.Id).Count;

                            List<EventRegistration> regs = new List<EventRegistration>();

                            if (ref_id.Length > 0)
                            {
                                regs = erManager.GetRegistrationsByRefIdAndEvent(ref_id, e.Id);
                            }
                            else
                            if (user != null)
                            {
                                regs = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
                            }

                            if (regs.Count > 0)
                            {
                                //if there is any registration where deleted == false there is an activ registration for that user 
                                EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
                                if (reg != null)
                                {
                                    model.AlreadyRegistered = true;
                                    model.Deleted = reg.Deleted;
                                }
                                //else there are only one or more deleted registrations and the user is not registered
                                else
                                {
                                    model.AlreadyRegistered = false;
                                    //model.Deleted = true;
                                }
                            }

                            //model.AlreadyRegisteredRefId = ref_id;


                            // Show event if deadline is not over
                            if (today <= e.Deadline.AddDays(1))
                                availableEvents.Add(model);
                        }
                    }

                    return Json(availableEvents, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEventRegistrationJson(long id)
        {
            using (EventManager eManager = new EventManager())
            using (PartyManager partyManager = new PartyManager())
            {
                User user = BExISAuthorizeHelper.GetUserFromAuthorization(HttpContext);

                var e = eManager.GetEventById(id);
                EventRegistrationLoadModel model = new EventRegistrationLoadModel();
                model.Name = e.Name;
                model.Date = e.EventDate;
                model.Location = e.Location;
                model.Language = e.EventLanguage;
                model.ImportantInformation = e.ImportantInformation;
                model.JsonFile = e.Data;

                if (user != null && !string.IsNullOrWhiteSpace(model.JsonFile))
                {
                    var userParty = partyManager.GetPartyByUser(user.Id);
                    JObject json = JObject.Parse(model.JsonFile);

                    foreach (JObject section in json["registration"])
                    {
                        foreach (JObject entry in section["entries"])
                        {
                            switch ((string)entry["key"])
                            {
                                case "firstName":
                                    entry["value"] = userParty.CustomAttributeValues.Where(b => b.CustomAttribute.Name == "FirstName").Select(v => v.Value).FirstOrDefault();
                                    break;

                                case "lastName":
                                    entry["value"] = userParty.CustomAttributeValues.Where(b => b.CustomAttribute.Name == "LastName").Select(v => v.Value).FirstOrDefault();
                                    break;

                                case "email":
                                    entry["value"] = user.Email;
                                    break;
                            }
                        }
                    }

                    model.JsonFile = json.ToString();
                }
               
               

                return Json(model, JsonRequestBehavior.AllowGet);
            } 
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEventPassword(long id)
        {
            using (EventManager eManager = new EventManager())
            {
                var e = eManager.GetEventById(id).LogInPassword;
                return Json(e, JsonRequestBehavior.AllowGet);
            }
        }

        [JsonNetFilter]
        [HttpPost]
        public JsonResult Create(EventRegistrationModel model)
        {
            using (EventManager eManager = new EventManager())
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                string data = JsonStringNormalizer.Normalize(model.JsonFile);
                JObject obj = JObject.Parse(data);

                var e = eManager.EventRepo.Get(model.EventId);

                // get email adress from XML && get ref_id based on email adress
                EmailStructure emailStructure = new EmailStructure();
                emailStructure = EmailHelper.ReadFile(e.EventLanguage);
                var email = obj
                            .Descendants()                      // alle JTokens im Baum
                            .OfType<JObject>()                   // nur Objekte
                            .FirstOrDefault(o => (string)o["key"] == emailStructure.lableEmail.ToLower())?["value"]
                            ?.ToString();
                //string email = model.Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
                string ref_id = EmailHelper.GetRefIdFromEmail(email);

                string notificationType = "";

                string url = Request.Url.GetLeftPart(UriPartial.Authority);

                // Check for logged in user
                User user = BExISAuthorizeHelper.GetUserFromAuthorization(HttpContext);

                CreateNewEventRegistration(e,data, user, email, notificationType, ref_id);

                return Json(new { success = true, id = 0 });
            }
        }

        [JsonNetFilter]
        [HttpGet]
        public JsonResult Get(long id, string ref_id = null)
        {
            using (EventManager eManager = new EventManager())
            using (EventRegistrationManager eventRegistrationManager = new EventRegistrationManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                var reg = new EventRegistration();
                var e = eManager.GetEventById(id);
                User user = BExISAuthorizeHelper.GetUserFromAuthorization(HttpContext);
                if(ref_id != null)
                    reg = eventRegistrationManager.GetRegistrationsByRefIdAndEvent(ref_id, id).FirstOrDefault();

                else if(user != null)
                    reg = eventRegistrationManager.GetRegistrationByUserAndEvent(user.Id, id).FirstOrDefault();
                else
                    return Json(new { success = false, id = 0 });

                EventRegistrationLoadModel model = new EventRegistrationLoadModel();
                model.Name = e.Name;
                model.Date = e.EventDate;
                model.Location = e.Location;
                model.Language = e.EventLanguage;
                model.ImportantInformation = e.ImportantInformation;
                model.JsonFile = reg.Data;
                return Json(model, JsonRequestBehavior.AllowGet);
            }
        }

        [JsonNetFilter]
        [HttpPost]
        public JsonResult Edit(EventRegistrationModel model)
        {
            using (EventManager eManager = new EventManager())
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                string data = JsonStringNormalizer.Normalize(model.JsonFile);
                JObject obj = JObject.Parse(data);

                // Check for logged in user
                User user = BExISAuthorizeHelper.GetUserFromAuthorization(HttpContext);

                var e = eManager.EventRepo.Get(model.EventId);
                var reg = erManager.GetRegistrationByUserAndEvent(user.Id, model.EventId).FirstOrDefault();
                if (reg != null)
                {

                    // get email adress from XML && get ref_id based on email adress
                    EmailStructure emailStructure = new EmailStructure();
                    emailStructure = EmailHelper.ReadFile(e.EventLanguage);
                    var email = obj
                                .Descendants()                      // alle JTokens im Baum
                                .OfType<JObject>()                   // nur Objekte
                                .FirstOrDefault(o => (string)o["key"] == emailStructure.lableEmail.ToLower())?["value"]
                                ?.ToString();

                    reg.Data = data;
                    erManager.UpdateEventRegistration(reg);

                    string url = Request.Url.GetLeftPart(UriPartial.Authority);
                    EmailHelper.SendEmailNotification("updated", email, reg.Token, reg.Data, e, url);

                    return Json(new { success = true, id = 0 });
                }
                else
                    return Json(new { success = false, id = 0 });
            }
        }


        [JsonNetFilter]
        [HttpGet]
        public JsonResult Delete(long id, string ref_id)
        {
            string url = Request.Url.GetLeftPart(UriPartial.Authority);

            using (SubjectManager subManager = new SubjectManager())
            {
                using (EventRegistrationManager erManager = new EventRegistrationManager())
                using (var eventManager = new EventManager())
                {
                    //HttpContext.User.Identity.Name
                    User user = BExISAuthorizeHelper.GetUserFromAuthorization(HttpContext);
                    if (user != null)
                    {
                        List<EventRegistration> regs = erManager.GetRegistrationByUserAndEvent(user.Id, id);
                        EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();

                        if (reg != null)
                        {
                            reg.Deleted = true;
                            erManager.UpdateEventRegistration(reg);
                            MoveFromWaitingList(reg.Event.Id);

                            string email = "";
                            if (user != null)
                                email = user.Email;
                            //else
                            //    email = reg.Data.GetElementsByTagName("Email")[0].InnerText;

                            EmailHelper.SendEmailNotification("deleted", email, ref_id, reg.Data, reg.Event, url);
                        }
                    }
                    else if (ref_id.Length > 0)
                    {
                        List<EventRegistration> regs = erManager.GetRegistrationsByRefIdAndEvent(ref_id, id);
                        EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
                        if (reg != null)
                        {
                            reg.Deleted = true;
                            erManager.UpdateEventRegistration(reg);
                            MoveFromWaitingList(reg.Event.Id);
                            string email = "";
                            if (user != null)
                                email = user.Email;
                            //else
                            //    email = reg.Data.GetElementsByTagName("Email")[0].InnerText;

                            EmailHelper.SendEmailNotification("deleted", email, ref_id, reg.Data, reg.Event, url);
                        }
                    }
                }

                return Json(new { success = true, id = id });
            }
        }


        [JsonNetFilter]
        [HttpPost]
        public JsonResult UserAlreadyRegistered(EventRegistrationModel model)
        {
            using (EventManager eManager = new EventManager())
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            {
                var e = eManager.GetEventById(model.EventId);
                bool registerd = false;
                EmailStructure emailStructure = new EmailStructure();
                emailStructure = EmailHelper.ReadFile(e.EventLanguage);
                string data = JsonStringNormalizer.Normalize(model.JsonFile);
                JObject obj = JObject.Parse(data);
                var email = obj
                                   .Descendants()
                                   .OfType<JObject>()
                                   .FirstOrDefault(o => (string)o["key"] == emailStructure.lableEmail.ToLower())?["value"]
                                   ?.ToString();

                var regs = erManager.GetAllRegistrationsByEvent(model.EventId);
                foreach (var r in regs)
                {
                    JObject objReg = JObject.Parse(r.Data);
                    var emailReg = objReg
                                    .Descendants()                      
                                    .OfType<JObject>()                  
                                    .FirstOrDefault(o => (string)o["key"] == emailStructure.lableEmail.ToLower())?["value"]
                                    ?.ToString();

                    if (emailReg == email && r.Deleted == false)
                        registerd = true;
                    else
                        continue;
                }

                return Json(registerd, JsonRequestBehavior.AllowGet);
            }
        }

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
                    //else
                    //    email = reg.Data.GetElementsByTagName("Email")[0].InnerText;

                    EmailHelper.SendEmailNotification("remove_from_waiting_list", email, "", reg.Data, reg.Event, url);

                }
            }
        }

        private void SendWaitingListNotification(XmlDocument data, Event e)
        {
            // todo: add not allowed / log in info to mail

            EmailStructure emailStructure = new EmailStructure();
            emailStructure = EmailHelper.ReadFile(e.EventLanguage);

            string first_name = data.GetElementsByTagName(emailStructure.lableFirstName)[0].InnerText;
            string last_name = data.GetElementsByTagName(emailStructure.lableLastname)[0].InnerText;
            string email = data.GetElementsByTagName(emailStructure.lableEmail)[0].InnerText;

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

       
        /// <summary>
        /// Create a new event registration
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private void CreateNewEventRegistration(Event e, string data, User user, string email, string notificationType, string ref_id)
        {
            bool waitingList = false;
            using (var erManager = new EventRegistrationManager())
            using(var eventManager = new EventManager())
            { 
                //check Participants Limitation
                if (e.ParticipantsLimitation != 0)
                {
                    int countRegs = erManager.GetNumerOfRegistrationsByEvent(e.Id) + 1;
                    int countWaitingList = erManager.GetAllWaitingListRegsByEvent(e.Id).Count + 1;

                    if (countRegs > e.ParticipantsLimitation)
                    {
                        if(e.WaitingList && !e.Closed)
                        {
                            if(countWaitingList == e.WaitingListLimitation)
                            {
                                e.Closed = true;
                                eventManager.UpdateEvent(e);
                            }
                            
                            notificationType = "succesfully_registered_waiting_list";
                            waitingList = true;
                            
                        }
                        else
                        {
                            e.Closed = true;
                            eventManager.UpdateEvent(e);
                            notificationType = "succesfully_registered";
                        }
                    }
                    else
                    {
                        notificationType = "succesfully_registered";
                    }
                }
                else
                {
                    notificationType = "succesfully_registered";
                }

                // Save registration and send notification
                erManager.CreateEventRegistration(data, e, user, false, ref_id, waitingList, DateTime.Now);

                string url = Request.Url.GetLeftPart(UriPartial.Authority);

                EmailHelper.SendEmailNotification(notificationType, email, ref_id,data, e, url);
        }
    }


        #region Validation





        #endregion



        #region Helper


        public static class JsonStringNormalizer
        {
            public static string Normalize(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return input;

                // Fall A: bereits roher JSON-Text
                var t = input.TrimStart();
                if (t.StartsWith("{") || t.StartsWith("["))
                {
                    // optional: inneres Dequoten, falls values wie "\"Text\"" gespeichert wurden
                    return DequoteInnerValues(input);
                }

                // Fall B: JSON als String → einmal entquoten
                try
                {
                    var once = JsonConvert.DeserializeObject<string>(input); // "\"{...}\"" -> "{...}"
                    if (!string.IsNullOrWhiteSpace(once) && (once.TrimStart().StartsWith("{") || once.TrimStart().StartsWith("[")))
                    {
                        // optional: inneres Dequoten
                        return DequoteInnerValues(once);
                    }
                    // Falls es kein JSON ist, trotzdem sauber zurückgeben
                    return once ?? input;
                }
                catch
                {
                    // notfalls original zurück
                    return input;
                }
            }

            private static string DequoteInnerValues(string json)
            {
                try
                {
                    var token = JToken.Parse(json);
                    DequoteAll(token);
                    return token.ToString(Newtonsoft.Json.Formatting.None);
                }
                catch
                {
                    return json; // falls kein valider JSON
                }
            }

            private static void DequoteAll(JToken token)
            {
                if (token is JValue jv && jv.Type == JTokenType.String)
                {
                    var s = (string)jv;
                    if (!string.IsNullOrEmpty(s) && s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                    {
                        try { jv.Replace(JsonConvert.DeserializeObject<string>(s)); } catch { /* ignorieren */ }
                    }
                }
                else if (token is JContainer c)
                {
                    foreach (var child in c.Children().ToList())
                        DequoteAll(child);
                }
            }
        }





        #endregion


    }

}
