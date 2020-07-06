using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.Wizard;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.IO.Transform.Output;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Utilities;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using System.Xml.Linq;
using Telerik.Web.Mvc;
using Vaiona.Utils.Cfg;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;
using Vaiona.Web.Mvc.Modularity;

namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventRegistrationResultController : Controller
    {
        public ActionResult EventRegistration(string ref_id = "")
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Event Registrations", this.Session.GetTenant());

            List<EventRegistrationModel> model = GetAvailableEvents(ref_id);
            return View("AvailableEventsList", model);
        }

        public ActionResult EventRegistrationPatial(string message, string ref_id = "")
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Event Registrations", this.Session.GetTenant());

            List<EventRegistrationModel> model = GetAvailableEvents(ref_id);
            ViewBag.Message = message;
            return PartialView("AvailableEventsList", model);
        }

        #region Register to Event

        [GridAction]
        public ActionResult AvailableEvents(string ref_id)
        {
            List<EventRegistrationModel> model = GetAvailableEvents(ref_id);
            return View("AvailableEventsList", new GridModel<EventRegistrationModel> { Data = model });
        }

        private List<EventRegistrationModel> GetAvailableEvents(string ref_id = null)
        {
            EventManager eManger = new EventManager();
            List<Event> allEvents = eManger.GetAllEvents().ToList();

            List<EventRegistrationModel> availableEvents = new List<EventRegistrationModel>();

            SubjectManager subManager = new SubjectManager();
            EventRegistrationManager erManager = new EventRegistrationManager();
            User user = subManager.Subjects.Where(a=>a.Name== HttpContext.User.Identity.Name).FirstOrDefault() as User;

            foreach (Event e in allEvents)
            {
                DateTime today = DateTime.Now;
                if (today >= e.StartDate)
                {
                    EventRegistrationModel model = new EventRegistrationModel(e);
                    //check if user already registered (if logged in)
                    if (user != null)
                    { 
                        EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
                        if (reg != null)
                            model.AlreadyRegistered = true;
                    }
                    else if (ref_id != null)
                    {
                        EventRegistration reg = erManager.GetRegistrationByRefIdAndEvent(ref_id, e.Id);
                        if (reg != null)
                            model.AlreadyRegistered = true;
                            model.AlreadyRegisteredRefId = ref_id;
                    }

           
                    // Show event if either registered or deadline is not over
                    if (today <= e.Deadline || model.AlreadyRegistered == true)
                        availableEvents.Add(model);
                }
            }

            return availableEvents;
        }

        public ActionResult LogInToEvent(string id, string view_only = "false", string ref_id = null)
        {
            LogInToEventModel model = new LogInToEventModel(long.Parse(id), bool.Parse(view_only), ref_id);

            //check if it is an edit
            SubjectManager subManager = new SubjectManager();
            EventRegistrationManager erManager = new EventRegistrationManager();
            User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;
            if (user != null)
            {
                EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, long.Parse(id));
                if (reg != null)
                {
                    model.Edit = true;
                }
            }
            else if (ref_id != null)
            {
                EventRegistration reg = erManager.GetRegistrationByRefIdAndEvent(ref_id, long.Parse(id));
                if (reg != null)
                    model.Edit = true;
            }

            return PartialView("_logInToEvent", model);
        }

        public ActionResult LoadForm(LogInToEventModel model)
        {
            EventManager eManager = new EventManager();
            Event e = eManager.EventRepo.Get(model.EventId);

            if (e.LogInPassword != model.LogInPassword)
                ModelState.AddModelError("passwort", "The event passwort is wrong.");

            if (ModelState.IsValid)
            {
                CreateTaskmanager taskManager = new CreateTaskmanager();
                taskManager.AddToBus(CreateTaskmanager.METADATASTRUCTURE_ID, e.MetadataStructure.Id);
                taskManager.AddToBus(CreateTaskmanager.ENTITY_ID, e.Id);

                if (model.Edit)
                {
                    EventRegistrationManager erManager = new EventRegistrationManager();
                    SubjectManager subManager = new SubjectManager();
                    User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;
                    if (user != null)
                    {
                        EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
                        taskManager.AddToBus(CreateTaskmanager.METADATA_XML, XDocument.Load(new XmlNodeReader(reg.Data)));
                    }
                    else if (model.RefId != null)
                    {
                        EventRegistration reg = erManager.GetRegistrationByRefIdAndEvent(model.RefId, e.Id);
                        taskManager.AddToBus(CreateTaskmanager.METADATA_XML, XDocument.Load(new XmlNodeReader(reg.Data)));
                    }

                }

                taskManager.AddToBus(CreateTaskmanager.SAVE_WITH_ERRORS, false);
                if (model.ViewOnly == true)
                {
                    taskManager.AddToBus(CreateTaskmanager.LOCKED, true);
                }

                taskManager.AddToBus(CreateTaskmanager.NO_IMPORT_ACTION, true);
                taskManager.AddToBus(CreateTaskmanager.INFO_ON_TOP_TITLE, "Event registration");
                taskManager.AddToBus(CreateTaskmanager.INFO_ON_TOP_DESCRIPTION, "<p><b>help</b></p>");


                Session["CreateDatasetTaskmanager"] = taskManager;

                setAdditionalFunctions();
                return Json(new { success = true, edit = model.Edit });


                //return RedirectToAction("StartMetadataEditor", "Form", new { area = "DCM" });
            }
            else
            {
                return PartialView("_logInToEvent", model);
            }

        }

        public ActionResult LoadMetadataForm()
        {
            var view = this.Render("DCM", "Form", "StartMetadataEditor", new RouteValueDictionary()   
            {});

            return Content(view.ToHtmlString(), "text/html");
        }

        public ActionResult Save()
        {
            EventRegistrationManager erManager = new EventRegistrationManager();
            EventManager eManager = new EventManager();
            SubjectManager subManager = new SubjectManager();

            CreateTaskmanager taskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            XDocument data = new XDocument();
            if (taskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
                data = (XDocument)taskManager.Bus[CreateTaskmanager.METADATA_XML];

            long eventId = 0;
            if (taskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_ID))
                eventId = (long)taskManager.Bus[CreateTaskmanager.ENTITY_ID];

            Event e = new Event();
            if (eventId != 0)
                e = eManager.EventRepo.Get(eventId);

            // get email adress from XML && get ref_id based on email adress
            string email = XmlMetadataWriter.ToXmlDocument(data).GetElementsByTagName("Email")[0].InnerText;
            string ref_id = GetRefIdFromEmail(email);

            string notificationType = "";
            string message = "";

            // Check for logged in user
            User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

            // Check if event registration already exists - update registration
            EventRegistration reg = CheckEventRegistration(user, ref_id, e.Id, erManager);

            // Update event registration
            if (reg != null )
            {
                if (e.EditAllowed != true)
                {
                    SendEmailNotification("resend", email, ref_id, data, e, user);
                    return RedirectToAction("EventRegistrationPatial", new { message ="Update of your previous registration is not allowed. You registration details are send to your Email adress again." , message_type = "error"}); 
                }

                reg.Data = XmlMetadataWriter.ToXmlDocument(data);
                erManager.UpdateEventRegistration(reg);

                SendEmailNotification("updated", email, ref_id, data, e, user);
                message = "Registration details sucessfully updated.";

            }
            // New event registration
            else
            {

                //check Participants Limitation
                if (e.ParticipantsLimitation != 0)
                {
                    int countRegs = erManager.GetNumerOfRegistrationsByEvent(e.Id);
                    if (countRegs > e.ParticipantsLimitation)
                    {
                        message = "Number of participants has been reached. You are now on the waiting list.";
                        notificationType = "succesfully_registered_waiting_list";
                    }
                    else
                    {
                        message = "You registered sucessfully.";
                        notificationType = "succesfully_registered";
                    }
                }
                else
                {
                    message = "You registered sucessfully.";
                    notificationType = "succesfully_registered";
                }

                // Add hint to message text
                string change = "";
                if (e.EditAllowed == true)
                {
                   change = "and change";
                }
                else
                {
                    change = "(edit is not allowed - in urgent cases please contact ...)"; // todo fill with mail adress
                }
                if (user != null)
                {
                    message = message + " To view "+ change+" your registration log in or follow the link send via email.";
                }
                else
                {
                    message =  message + " To view " + change + " your registration follow the link send via email.";
                }

                // Save registration and send notification
                erManager.CreateEventRegistration(XmlMetadataWriter.ToXmlDocument(data), e, user, false, ref_id);
                SendEmailNotification(notificationType, email, ref_id, data, e, user);
                

                ////Set permissions on event registration
                //PermissionManager pManager = new PermissionManager();

                //foreach (RightType rightType in Enum.GetValues(typeof(RightType)).Cast<RightType>())
                //{
                //    pManager.CreateDataPermission(user.Id, 2, resource.Id, rightType);
                //}

            }
            return RedirectToAction("EventRegistrationPatial", new { message = message, ref_id = ref_id}); 
        }

        #endregion

        #region Show Event Registration Results

        public ActionResult Show()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Show Event Results", this.Session.GetTenant());

            return View("EventRegistrationResults", new DataTable());
        }

       // public ActionResult ExportToExcel(string eventName, string eventId)
       // {
       //     eventName = "eventName";
      //      ExcelWriter excelWriter = new ExcelWriter();

           // string path = excelWriter.CreateFile(eventName);

            //excelWriter.AddDataTableToExcel(GetEventResults(long.Parse(eventId)), path);

           // return File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
      //  }

        public ActionResult FillTree()
        {
            EventManager eManager = new EventManager();
            List<Event> events = eManager.GetAllEvents().ToList();
            List<EventRegistrationFilterModel> model = new List<EventRegistrationFilterModel>();

            EventRegistrationFilterModel closed = new EventRegistrationFilterModel();
            closed.Status = "closed";
            closed.EventFilterItems = new List<EventFilterItem>();

            EventRegistrationFilterModel open = new EventRegistrationFilterModel();
            open.Status = "open";
            open.EventFilterItems = new List<EventFilterItem>();

            foreach (Event e in events)
            {
                if (e.Deadline < DateTime.Now)
                    closed.EventFilterItems.Add(new EventFilterItem(e));
                else
                    open.EventFilterItems.Add(new EventFilterItem(e));
            }

            model.Add(open);
            model.Add(closed);

            return PartialView("_selectEvent", model);
        }

        public ActionResult OnSelectTreeViewItem(long id)
        {
            return View("EventRegistrationResults", GetEventResults(id));
        }


        #endregion

        #region Xml to DataTable

        private DataTable GetEventResults(long eventId)
        {
            //string path = AppConfiguration.GetModuleWorkspacePath("EMM");
            //XDocument xDoc = XDocument.Load(Path.Combine(path, "workshopReg4.xml"));

            DataTable results = new DataTable();

            EventRegistrationManager erManager = new EventRegistrationManager();
            List<EventRegistration> eventRegistrations = erManager.GetAllRegistrationsByEvent(eventId);

            if (eventRegistrations.Count != 0)
                results = CreateDataTableColums(results, XElement.Load(new XmlNodeReader(eventRegistrations[0].Data)));

            foreach (EventRegistration er in eventRegistrations)
            {
                results.Rows.Add(AddDataRow(XElement.Load(new XmlNodeReader(er.Data)), results));
            }

            return results;
        }

        private DataTable GetEventRegistration(long eventId, XDocument data)
        {
            DataTable results = new DataTable();

            EventRegistrationManager erManager = new EventRegistrationManager();

            results = CreateDataTableColums(results, XElement.Load(new XmlNodeReader(XmlMetadataWriter.ToXmlDocument(data))));
            results.Rows.Add(AddDataRow(XElement.Load(new XmlNodeReader(XmlMetadataWriter.ToXmlDocument(data))), results));
         
            return results;
        }

        private DataTable CreateDataTableColums(DataTable dataTable, XElement x)
        {
            DataTable dt = dataTable;
            // build your DataTable
            foreach (XElement xe in x.Descendants())
            {
                //if (xe.Attribute("input") != null)
                //{
                //if (xe.Attribute("input").Value != "intern" && !xe.HasElements)
                if (!xe.HasElements)
                {
                    DataColumn dc = new DataColumn();
                    dc.Caption = xe.Name.ToString();
                    dc.ColumnName = xe.GetAbsoluteXPath();

                    dt.Columns.Add(dc); // add columns to your dt
                }
                // }

                //if (!xe.HasElements)
                //{
                //    DataColumn dc = new DataColumn();
                //    dc.Caption = xe.Name.ToString();
                //    dc.ColumnName =xe.GetAbsoluteXPath();
                //    dt.Columns.Add(dc); // add columns to your dt
                //}


            }

            return dt;
        }

        private DataRow AddDataRow(XElement x, DataTable dt)
        {
            //var all = from p in x.Descendants(x.Name.ToString()) select p;
            DataRow dr = dt.NewRow();
            foreach (XElement xe in x.Descendants())
            {
                //if (xe.Attribute("input") != null)
                //{
                if (!xe.HasElements)
                {
                    //dr[xe.Name.ToString()] = xe.Value; //add in the values
                    dr[xe.GetAbsoluteXPath()] = xe.Value;
                }
                // }
            }

            return dr;
        }

        #endregion

        #region Helper

        private string GetRefIdFromEmail(string email)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes("abd_" + email));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            string ref_id = hash.ToString();

            return ref_id;
        }

        private EventRegistration CheckEventRegistration(User user, string ref_id, long event_id, EventRegistrationManager erManager)
        {
            EventRegistration reg_ref_id = erManager.GetRegistrationByRefIdAndEvent(ref_id, event_id);
            if (user != null)
            {
                EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, event_id);
                return reg; // user is logged in
            }
            else if (reg_ref_id != null)
            {
                return reg_ref_id; // provided ref_id fits to event
            }
            else
            {
                return null; 
            }
        }

        private void SendEmailNotification(string notificationType, string email, string ref_id, XDocument data, Event e, User user)
        {
            // todo: add not allowed / log in info to mail
            string first_name = XmlMetadataWriter.ToXmlDocument(data).GetElementsByTagName("FirstName")[0].InnerText;
            string last_name = XmlMetadataWriter.ToXmlDocument(data).GetElementsByTagName("LastName")[0].InnerText;
            string url = Request.Url.GetLeftPart(UriPartial.Authority);

            string mail_message = "";
            string subject = "";

            switch (notificationType)
            {
                case "succesfully_registered":
                    subject = "Registration confirmation for " + e.Name;
                    mail_message = "you registered to " + e.Name + ".\n";
                    break;
                case "succesfully_registered_waiting_list":
                    subject = "Registration confirmation for " + e.Name + " - wating list";
                    mail_message = "you registered to " + e.Name + ", but you are currently on the waiting list. \n";
                    break;
                case "updated":
                    subject = "Registration update confirmation for " + e.Name;
                    mail_message = "you updated your registration for " + e.Name + ".\n";
                    break;
                case "resend":
                    subject = "Resend of registration confirmation for " + e.Name;
                    mail_message = "your registration for " + e.Name + ".\n";
                    break;
            }

            string details = "";
            DataTable res = GetEventRegistration(e.Id, data);
            int row_count = res.Columns.Count;
            for (int i = 0; i < row_count; i++)
            {
                details = details + res.Columns[i].ToString().Split('/')[res.Columns[i].ToString().Split('/').Length - 2] + ":  " + res.Rows[0][i] + "\n";
            }


            string body = "Dear " + first_name + " " + last_name + ", " + "\n\n" +
                 mail_message +
                 "\nYour registration details are:\n\n" + 
                 details + "\n\n" +
                 "To view or change your registration follow this link: " + url + "/emm/EventRegistration/EventRegistration/?ref_id=" + ref_id + "\n\n" +
                 "Sincerely yours, \n" +
                 "BExIS Team";
     
            var es = new EmailService();


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
                new List<string> { ConfigurationManager.AppSettings["SystemEmail"] , e.EmailBCC}, // Allways send BCC to SystemEmail + additional set 
                new List<string> { replyTo }  
                );
        }

        private void setAdditionalFunctions()
        {
            CreateTaskmanager taskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];

            //set function actions of COPY, RESET,CANCEL,SUBMIT
            //ActionInfo copyAction = new ActionInfo();
            //copyAction.ActionName = "Index";
            //copyAction.ControllerName = "CreateDataset";
            //copyAction.AreaName = "DCM";

            //ActionInfo resetAction = new ActionInfo();
            //resetAction.ActionName = "Reset";
            //resetAction.ControllerName = "Form";
            //resetAction.AreaName = "DCM";

            //ActionInfo cancelAction = new ActionInfo();
            //cancelAction.ActionName = "Cancel";
            //cancelAction.ControllerName = "Form";
            //cancelAction.AreaName = "DCM";

            ActionInfo submitAction = new ActionInfo();
            submitAction.ActionName = "Save";
            submitAction.ControllerName = "EventRegistration";
            submitAction.AreaName = "EMM";
            //submitAction..IsPartial = false;


            //taskManager.Actions.Add(CreateTaskmanager.CANCEL_ACTION, cancelAction);
            //taskManager.Actions.Add(CreateTaskmanager.COPY_ACTION, copyAction);
            //taskManager.Actions.Add(CreateTaskmanager.RESET_ACTION, resetAction);
            taskManager.Actions.Add(CreateTaskmanager.SUBMIT_ACTION, submitAction);

        }

        #endregion
    }

}
