using BExIS.App.Bootstrap.Attributes;
using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.Modules.EMM.UI.Helper;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Utilities;
using BExIS.Utils.Data.MetadataStructure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Xml;



namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventRegistrationController : Controller
    {
        private CreateTaskmanager TaskManager;
        private MetadataStructureUsageHelper metadataStructureUsageHelper = new MetadataStructureUsageHelper();

        //public ActionResult EventRegistration(string ref_id = "")
        //{
        //    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Event Registrations", this.Session.GetTenant());

        //    List<EventRegistrationModel> model = GetAvailableEvents(ref_id);
        //    return View("AvailableEventsList", model);
        //}

        [JsonNetFilter]
        [HttpGet]
        public JsonResult GetEvents()
        {
            using (EventManager eManger = new EventManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                List<Event> allEvents = eManger.GetAllEvents().ToList();

                List<EventRegListModel> availableEvents = new List<EventRegListModel>();

                using (EventRegistrationManager erManager = new EventRegistrationManager())
                {
                    User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

                    foreach (Event e in allEvents)
                    {
                        DateTime today = DateTime.Now;
                        if (today >= e.StartDate)
                        {
                            EventRegListModel model = new EventRegListModel(e);
                            //model.NumberOfRegistration = erManager.GetAllRegistrationsNotDeletedByEvent(e.Id).Count;
                            //model.NrOfRegistrationWaitingList = erManager.GetAllWaitingListRegsByEvent(e.Id).Count;

                            List<EventRegistration> regs = new List<EventRegistration>();

                            //if (ref_id.Length > 0)
                            //{
                            //    regs = erManager.GetRegistrationsByRefIdAndEvent(ref_id, e.Id);
                            //}
                            //else
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
                                    //model.AlreadyRegistered = true;
                                    //model.Deleted = reg.Deleted;
                                }
                                //else there are only one or more deleted registrations and the user is not registered
                                else
                                {
                                    //model.AlreadyRegistered = false;
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
            {
                var e = eManager.GetEventById(id);
                EventRegistrationLoadModel model = new EventRegistrationLoadModel();
                model.Name = e.Name;
                model.Date = e.EventDate;
                model.Location = e.Location;
                model.Language = e.EventLanguage;
                model.ImportantInformation = e.ImportantInformation;
                model.JsonFile = e.Data;

                //JsonEventModel model = JsonConvert.DeserializeObject<JsonEventModel>(e.Data);

                //model.Registration[0].Entries.Where(a => a.Key == "name").FirstOrDefault().Value = e.Name;
                //model.Registration[0].Entries.Where(a => a.Key == "date").FirstOrDefault().Value = e.EventDate;
                //model.Registration[0].Entries.Where(a => a.Key == "location").FirstOrDefault().Value = e.Location;
                //model.Registration[0].Entries.Where(a => a.Key == "language").FirstOrDefault().Value = e.EventLanguage;
                //model.Registration[0].Entries.Where(a => a.Key == "importantInformation").FirstOrDefault().Value = e.ImportantInformation;
                //var json = JsonConvert.SerializeObject(model);

                return Json(model, JsonRequestBehavior.AllowGet);
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

                DefaultEventInformation defaultEventInformation = (DefaultEventInformation)Session["DefaultEventInformation"];

                //string data = JsonConvert.SerializeObject(model.JsonFile);
                JObject obj = JObject.Parse(model.JsonFile);

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
                User user = subManager.Subjects.Where(a => a.Name == "epetzold").FirstOrDefault() as User;

                CreateNewEventRegistration(e, model.JsonFile, user, email, notificationType, ref_id);

                //// Check if event registration already exists - update registration
                ////EventRegistration reg = CheckEventRegistration(ref_id, e.Id, erManager);
                //// get reg bei id
                //EventRegistration reg = erManager.GetRegistrationById(defaultEventInformation.RegistrationId);

                // Update event registration
                //if (reg != null)
                //{
                //    if (reg.Deleted == false)
                //    {
                //        if (e.EditAllowed != true)
                //        {
                //            //EmailHelper.SendEmailNotification("resend", email, ref_id, XmlMetadataWriter.ToXmlDocument(data), e, url);
                //            //return RedirectToAction("EventRegistrationPatial", new { message = "Update of your previous registration is not allowed. You registration details are send to your Email adress again.", message_type = "error" });
                //        }


                //        reg.Data = model.JsonFile;
                //        erManager.UpdateEventRegistration(reg);

                //        EmailHelper.SendEmailNotification("updated", email, ref_id, model.JsonFile, e, url);
                //    }
                //    else
                //        CreateNewEventRegistration(e, model.JsonFile, user, email, notificationType, ref_id);

                //// New event registration
                //else


                return Json(new { success = true, id = 0 });
            }
        }

        [JsonNetFilter]
        [HttpPost]
        public JsonResult Edit(long id)
        {
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                var reg = erManager.GetRegistrationById(id);

                return Json(reg.Data, JsonRequestBehavior.AllowGet);
            }
        }


            [JsonNetFilter]
        [HttpPost]
        public JsonResult Delete(long id, string ref_id)
        {
            string url = Request.Url.GetLeftPart(UriPartial.Authority);

            using (SubjectManager subManager = new SubjectManager())
            {
                using (EventRegistrationManager erManager = new EventRegistrationManager())
                using (var eventManager = new EventManager())
                {
                    User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;
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



        //public ActionResult EventRegistrationPatial(string message, string ref_id = "")
        //{
        //    ViewBag.Title = PresentationModel.GetViewTitleForTenant("Event Registrations", this.Session.GetTenant());

        //    List<EventRegistrationModel> model = GetAvailableEvents(ref_id);
        //    ViewBag.Message = message;
        //    return PartialView("AvailableEventsList", model);
        //}

        #region Register to Event

        //private List<EventRegistrationModel> GetAvailableEvents(string ref_id = "")
        //{
        //    using (EventManager eManger = new EventManager())
        //    using (SubjectManager subManager = new SubjectManager())
        //    {
        //        List<Event> allEvents = eManger.GetAllEvents().ToList();

        //        List<EventRegistrationModel> availableEvents = new List<EventRegistrationModel>();

        //        using (EventRegistrationManager erManager = new EventRegistrationManager())
        //        {
        //            User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

        //            foreach (Event e in allEvents)
        //            {
        //                DateTime today = DateTime.Now;
        //                if (today >= e.StartDate)
        //                {
        //                    EventRegistrationModel model = new EventRegistrationModel(e);
        //                    model.NumberOfRegistration = erManager.GetAllRegistrationsNotDeletedByEvent(e.Id).Count;
        //                    model.NrOfRegistrationWaitingList = erManager.GetAllWaitingListRegsByEvent(e.Id).Count;

        //                    model.Closed = e.Closed;
        //                    List<EventRegistration> regs = new List<EventRegistration>();
        //                    if (ref_id.Length > 0)
        //                    {
        //                        regs = erManager.GetRegistrationsByRefIdAndEvent(ref_id, e.Id);
        //                    }
        //                    else if(user != null)
        //                    {
        //                        regs = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
        //                    }

        //                        if (regs.Count > 0)
        //                        {
        //                            //if there is any registration where deleted == false there is an activ registration for that user 
        //                            EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
        //                            if (reg != null)
        //                            {
        //                                model.AlreadyRegistered = true;
        //                                model.Deleted = reg.Deleted;
        //                            }
        //                            //else there are only one or more deleted registrations and the user is not registered
        //                            else
        //                            {
        //                                model.AlreadyRegistered = false;
        //                                model.Deleted = true;
        //                            }
        //                        }
        //                        model.AlreadyRegisteredRefId = ref_id;


        //                    // Show event if deadline is not over
        //                    if (today <= e.Deadline.AddDays(1))
        //                        availableEvents.Add(model);
        //                }
        //            }
        //        }

        //        return availableEvents;
        //    }
        //}

        public ActionResult LogInToEvent(string id, string view_only = "false", string ref_id = "")
        {
            Session["DefaultEventInformation"] = null;
            LogInToEventModel model = new LogInToEventModel(long.Parse(id), bool.Parse(view_only), ref_id);

            //check if it is an edit
            using (SubjectManager subManager = new SubjectManager())
            {
                using (EventRegistrationManager erManager = new EventRegistrationManager())
                {
                    User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

                    if(ref_id.Length > 0)
                    {
                        List<EventRegistration> regs = erManager.GetRegistrationsByRefIdAndEvent(model.RefId, long.Parse(id));
                        EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
                        if (reg != null)
                            model.Edit = true;

                    }
                   else if (user != null)
                    {
                        List<EventRegistration> regs = erManager.GetRegistrationByUserAndEvent(user.Id, long.Parse(id));
                        EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
                        if (reg != null)
                                model.Edit = true;

                    }
                   
                }
            }

            return PartialView("_logInToEvent", model);
        }

        #endregion

        #region Load Registration Form

        //public ActionResult LoadForm(LogInToEventModel model)
        //{
        //    using (EventManager eManager = new EventManager())
        //    using (EventRegistrationManager erManager = new EventRegistrationManager())
        //    using (SubjectManager subManager = new SubjectManager())
        //    {
        //        Event e = eManager.EventRepo.Get(model.EventId);
        //        User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

        //        if (e.LogInPassword != model.LogInPassword)
        //            ModelState.AddModelError("passwort", "The event passwort is wrong.");

        //        if (ModelState.IsValid)
        //        {
        //            //add default value to session
        //            DefaultEventInformation defaultEventInformation = new DefaultEventInformation();
        //            defaultEventInformation.EventName = HttpUtility.HtmlDecode(e.Name);
        //            defaultEventInformation.Location = HttpUtility.HtmlDecode(e.Location);
        //            defaultEventInformation.Eventid = e.Id.ToString();
                    
        //            //user information
        //            if (user != null)
        //            {
        //                using (var partyManager = new PartyManager())
        //                {
        //                    defaultEventInformation.Email = user.Email;
        //                    var party = partyManager.GetPartyByUser(user.Id);
        //                    defaultEventInformation.FirstName = HttpUtility.HtmlDecode(party.CustomAttributeValues.Where(b => b.CustomAttribute.Name == "FirstName").Select(v => v.Value).FirstOrDefault());
        //                    defaultEventInformation.LastName = HttpUtility.HtmlDecode(party.CustomAttributeValues.Where(b => b.CustomAttribute.Name == "LastName").Select(v => v.Value).FirstOrDefault());
        //                }
        //            }

        //            if (!String.IsNullOrEmpty(e.EventDate))
        //                defaultEventInformation.Date = e.EventDate;
        //            if (!String.IsNullOrEmpty(e.EventLanguage))
        //                if(e.Id ==12)
        //                    defaultEventInformation.Language = "English";
        //            else
        //                defaultEventInformation.Language = e.EventLanguage;

        //            if (!String.IsNullOrEmpty(e.ImportantInformation))
        //                defaultEventInformation.ImportantInformation = HttpUtility.HtmlDecode(e.ImportantInformation);

        //            Session["DefaultEventInformation"] = defaultEventInformation;

        //            if (model.Edit)
        //            {
        //                    if(model.RefId != null && model.RefId.Length > 0)
        //                    {
        //                        List<EventRegistration> regs = erManager.GetRegistrationsByRefIdAndEvent(model.RefId, e.Id);
        //                        EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
        //                        defaultEventInformation.RegistrationId = reg.Id;
        //                       defaultEventInformation.Data = reg.Data;
        //                    }
        //                    else if (user != null)
        //                    {
        //                        List<EventRegistration> regs = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
        //                        EventRegistration reg = regs.Where(a => a.Deleted == false).FirstOrDefault();
        //                        defaultEventInformation.RegistrationId = reg.Id;

        //                        defaultEventInformation.Data = reg.Data;
        //                    }
        //                    //todo error message 
                            
                        

        //            }

        //            return Json(new { success = true, edit = model.Edit });
        //        }
        //        else
        //        {
        //            return PartialView("_logInToEvent", model);
        //        }
        //    }

        //}

        /// <summary>
        /// User deleted registration, this function set flag deleted in event registration = true
        /// </summary>
        /// <param name="id">event registration id</param>
        /// <param name="ref_id">event registration ref id</param>
        /// <returns></returns>
        public ActionResult DeleteRegistration(string id, string ref_id = "")
        {
            string url = Request.Url.GetLeftPart(UriPartial.Authority);

            using (SubjectManager subManager = new SubjectManager())
            {
                using (EventRegistrationManager erManager = new EventRegistrationManager())
                using (var eventManager = new EventManager())
                {
                    User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;
                    if (user != null)
                    {
                        List<EventRegistration> regs = erManager.GetRegistrationByUserAndEvent(user.Id, long.Parse(id));
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

                            EmailHelper.SendEmailNotification("deleted",email, ref_id, reg.Data, reg.Event, url);
                        }
                    }
                    else if (ref_id.Length > 0)
                    {
                        List<EventRegistration> regs = erManager.GetRegistrationsByRefIdAndEvent(ref_id, long.Parse(id));
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

               


            }
            return Json(new { result = "redirect", url = Url.Action("EventRegistration", "EventRegistration", new { area = "EMM" }) }, JsonRequestBehavior.AllowGet);
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

        //public JsonResult Save(long eventId, UpdateEventModel model)
        //{
        //    using (EventManager eManager = new EventManager())
        //    using (EventRegistrationManager erManager = new EventRegistrationManager())
        //    using (SubjectManager subManager = new SubjectManager())
        //    {
               
        //        DefaultEventInformation defaultEventInformation = (DefaultEventInformation)Session["DefaultEventInformation"];

        //        string data = JsonConvert.SerializeObject(model);

        //        Event e = new Event();
        //        e = eManager.EventRepo.Get(eventId);

        //        // get email adress from XML && get ref_id based on email adress
        //        EmailStructure emailStructure = new EmailStructure();
        //        emailStructure = EmailHelper.ReadFile(e.EventLanguage);
        //        string email = model.Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
        //        string ref_id = EmailHelper.GetRefIdFromEmail(email);

        //        string notificationType = "";

        //        string url = Request.Url.GetLeftPart(UriPartial.Authority);


        //        // Check for logged in user
        //        User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

        //        // Check if event registration already exists - update registration
        //        //EventRegistration reg = CheckEventRegistration(ref_id, e.Id, erManager);
        //        // get reg bei id
        //        EventRegistration reg = erManager.GetRegistrationById(defaultEventInformation.RegistrationId);

        //        // Update event registration
        //        if (reg != null)
        //        {
        //            if (reg.Deleted == false)
        //            {
        //                if (e.EditAllowed != true)
        //                {
        //                    //EmailHelper.SendEmailNotification("resend", email, ref_id, XmlMetadataWriter.ToXmlDocument(data), e, url);
        //                    //return RedirectToAction("EventRegistrationPatial", new { message = "Update of your previous registration is not allowed. You registration details are send to your Email adress again.", message_type = "error" });
        //                }


        //                reg.Data = data;
        //                erManager.UpdateEventRegistration(reg);

        //                EmailHelper.SendEmailNotification("updated", email, ref_id,data, e, url);
        //            }
        //            else
        //                CreateNewEventRegistration(e, data, user, email, notificationType, ref_id);
        //        }
        //        // New event registration
        //        else
        //            CreateNewEventRegistration(e, data, user, email, notificationType, ref_id);

        //        return Json(new { result = "redirect", url = Url.Action("EventRegistration", "EventRegistration", new { area = "EMM", ref_id = ref_id }) }, JsonRequestBehavior.AllowGet);
        //    }
        //}

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

        #endregion

        #region Validation



     

        #endregion

      

        #region Helper

        ///// <summary>
        ///// Check if user allready register with the given email adress
        ///// </summary>
        ///// <returns>true or false</returns>
        //private bool UserAllreadyRegister(long eventId, UpdateEventModel model)
        //{
        //    if (eventId != 0)
        //    {
        //        //get email adress to check if it already exsits in thsi event
        //        string email = "";
        //        EmailStructure emailStructure = new EmailStructure();
        //        using (var eventManager = new EventManager())
        //        {
        //            var e = eventManager.GetEventById(eventId);
        //            emailStructure = EmailHelper.ReadFile(e.EventLanguage);
        //            email = model.Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
        //        }

        //        using (var eventRegistrationManager = new EventRegistrationManager())
        //        {
        //            //get all registrations from the selected event 
        //            var eventRegistrations = eventRegistrationManager.GetAllRegistrationsByEvent(eventId);
        //            foreach (var er in eventRegistrations)
        //            {
        //                //return true if we have a match and the registration is not deleted. Id deleted == true registration with same email possible
        //                JsonEventModel m = (JsonEventModel)JsonConvert.DeserializeObject(er.Data);
        //                string mail = model.Entries.Where(a => a.Title == emailStructure.lableEmail).FirstOrDefault().Value;
        //                if (email == mail)
        //                    return true;
        //            }
        //        }
        //    }

        //    return false;
        //}

        private EventRegistration CheckEventRegistration(string ref_id, long event_id, EventRegistrationManager erManager)
        {
            EventRegistration reg_ref_id = erManager.GetRegistrationByRefIdAndEvent(ref_id, event_id);
         
            if (reg_ref_id != null)
            {
                return reg_ref_id; // provided ref_id fits to event
            }
            else
            {
                return null; 
            }
        }


       
        #endregion

      
    }

}
