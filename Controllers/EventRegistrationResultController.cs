using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.Wizard;
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
using System.Xml;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;

namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventRegistrationResultController : Controller
    {

        private CreateTaskmanager TaskManager;

        #region Show Event Registration Results

        public ActionResult Show()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant(" Show Reservation", this.Session.GetTenant());
            EventRegistrationResultModel model = new EventRegistrationResultModel();
            model.Results = new DataTable();
            return View("EventRegistrationResults", model);
        }

        /// <summary>
        /// delete event with all registrations
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns>csv file</returns>
        public ActionResult Delete(string id)
        {
            long eventId = Convert.ToInt64(id);
            using (var eventRegistrationManager = new EventRegistrationManager())
            using (var eventManager = new EventManager())
            { 
                //delete first all registrations
                List<EventRegistration> eventRegistrations = eventRegistrationManager.GetAllRegistrationsByEvent(eventId);
                eventRegistrations.ForEach(a => eventRegistrationManager.DeleteEventRegistration(a));

                eventManager.DeleteEvent(eventManager.GetEventById(eventId));
            }
          
            return RedirectToAction("Show");
        }

        /// <summary>
        /// clear, that means delete all registrations from one event
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns>csv file</returns>
        public ActionResult Clear(string id)
        {
            long eventId = Convert.ToInt64(id);
            using (var eventRegistrationManager = new EventRegistrationManager())
            using (var eventManager = new EventManager())
            {
                //delete first all registrations
                List<EventRegistration> eventRegistrations = eventRegistrationManager.GetAllRegistrationsByEvent(eventId);
                eventRegistrations.ForEach(a => eventRegistrationManager.DeleteEventRegistration(a));

                var e = eventManager.GetEventById(eventId);
                if(e.Closed == true)
                {
                    e.Closed = false;
                    eventManager.UpdateEvent(e);
                }
            }

            return RedirectToAction("Show");
        }

        /// <summary>
        /// export as comma seperatred csv
        /// </summary>
        /// <param name="id">event id</param>
        /// <returns>csv file</returns>
        public ActionResult Export(string id)
        {
            DataTable dataTable = GetEventResults(long.Parse(id));

            var lines = new List<string>();
            string[] columnNames = dataTable.Columns
                    .Cast<DataColumn>()
                    .Select(column => column.ColumnName)
                    .ToArray();

            var header = string.Join(",", columnNames.Select(name => $"\"{name}\""));
            lines.Add(header);

            var valueLines = dataTable.AsEnumerable()
                .Select(row => string.Join(",", row.ItemArray.Select(val => $"\"{val}\"")));

            lines.AddRange(valueLines);

            string eventName;

            using (EventManager eventManager = new EventManager())
            {
                eventName = eventManager.GetEventById(long.Parse(id)).Name;
            }
            //remove invaid chars in eventname for filename
            string filename = string.Join("_", eventName.Split(Path.GetInvalidFileNameChars()));

            string dataPath = AppConfiguration.DataPath;
            string storePath = Path.Combine(dataPath, "EMM", "Temp", filename + ".csv");

            System.IO.File.WriteAllLines(storePath, lines);

            return File(storePath, MimeMapping.GetMimeMapping(eventName + ".csv"), Path.GetFileName(storePath));
        }

        public ActionResult FillTree()
        {
            using (EventManager eManager = new EventManager())
            {
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

                open.EventFilterItems = open.EventFilterItems.OrderBy(a => a.Id).ToList();
                closed.EventFilterItems = closed.EventFilterItems.OrderBy(a => a.Id).ToList();
                open.EventFilterItems = Enumerable.Reverse(open.EventFilterItems).ToList();
                closed.EventFilterItems = Enumerable.Reverse(closed.EventFilterItems).ToList();

                model.Add(open);
                model.Add(closed);

                return PartialView("_selectEvent", model);
            }
        }

        public ActionResult OnSelectTreeViewItem(long id)
        {
            EventRegistrationResultModel model = new EventRegistrationResultModel();
            model.Results = GetEventResults(id);
            model.WaitingListResults =  GetWaitingListResults(id);
            
            model.EventId = id;

            //check rights on event
            using (var permissionManager = new EntityPermissionManager())
            using (var entityTypeManager = new EntityManager())
            using (var userManager = new UserManager())
            {
                var user = userManager.FindByNameAsync(HttpContext.User.Identity.Name).Result;
                Entity entity = entityTypeManager.FindByName("Event");
                model.UserHasRights = permissionManager.HasEffectiveRight(user.Name, entity.EntityType, id, RightType.Read);
            }

            return View("EventRegistrationResults", model);
        }

        public ActionResult MoveFromWaitingList(long id, long eventId)
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

            return RedirectToAction("OnSelectTreeViewItem", new { id = eventId });
        }

        public ActionResult ResendNotification(long id, long eventId)
        {
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (EventManager eventManager = new EventManager())
            {
                var registration = erManager.EventRegistrationRepo.Get(a => a.Id == id).FirstOrDefault();

                var e = eventManager.GetEventById(eventId);
                Resend(registration.Data, e);
            }

            return RedirectToAction("OnSelectTreeViewItem", new { id = eventId });
        }

        private void Resend(XmlDocument data, Event e)
        {
            // get email adress from XML && get ref_id based on email adress
            string email = data.GetElementsByTagName("Email")[0].InnerText;
            string ref_id = EmailHelper.GetRefIdFromEmail(email);
            string url = Request.Url.GetLeftPart(UriPartial.Authority);
            EmailHelper.SendEmailNotification("resend", email, ref_id, data, e, url);
        }

      

        private void SendNotification(XmlDocument data, Event e)
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
                emailStructure.removeFromWaitingList1  + "<br/><br/>" +
                 emailStructure.bodyClosing + "<br/>" +
                 emailStructure.bodyClosingName;


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
                new List<string> { ConfigurationManager.AppSettings["SystemEmail"], e.EmailBCC }, // Allways send BCC to SystemEmail + additional set 
                new List<string> { replyTo }
                );

        }

        #endregion

        #region edit event registration

        public ActionResult LoadForm(long id, long eventid)
        {
            using (EventManager eManager = new EventManager())
            {
                Event e = eManager.EventRepo.Get(eventid);

                //add default value to session
                DefaultEventInformation defaultEventInformation = new DefaultEventInformation();
                defaultEventInformation.EventName = e.Name;
                defaultEventInformation.Eventid = e.Id.ToString();
                if (!String.IsNullOrEmpty(e.EventDate))
                    defaultEventInformation.Date = e.EventDate;
                if (!String.IsNullOrEmpty(e.EventLanguage))
                       defaultEventInformation.Language = e.EventLanguage;

                if (!String.IsNullOrEmpty(e.ImportantInformation))
                    defaultEventInformation.ImportantInformation = e.ImportantInformation;

                Session["DefaultEventInformation"] = defaultEventInformation;

                //CreateTaskmanager taskManager = new CreateTaskmanager();
                if (TaskManager == null)
                    TaskManager = new CreateTaskmanager();

                TaskManager.AddToBus(CreateTaskmanager.METADATASTRUCTURE_ID, e.MetadataStructure.Id);
                TaskManager.AddToBus(CreateTaskmanager.ENTITY_ID, e.Id);
                
                using (EventRegistrationManager erManager = new EventRegistrationManager())
                {

                    EventRegistration reg = erManager.EventRegistrationRepo.Get(a => a.Id == id).FirstOrDefault();
                    XmlNodeReader xmlNodeReader = new XmlNodeReader(reg.Data);
                    TaskManager.AddToBus(CreateTaskmanager.METADATA_XML, reg.Data);
                    xmlNodeReader.Dispose();
                    }

                }

                TaskManager.AddToBus(CreateTaskmanager.SAVE_WITH_ERRORS, false);

                TaskManager.AddToBus(CreateTaskmanager.NO_IMPORT_ACTION, true);
                TaskManager.AddToBus(CreateTaskmanager.INFO_ON_TOP_TITLE, "Event registration");
                TaskManager.AddToBus(CreateTaskmanager.INFO_ON_TOP_DESCRIPTION, "<p><b>help</b></p>");


                Session["EventRegistrationTaskmanager"] = TaskManager;

                setAdditionalFunctions();

            return new EmptyResult();

         }
        private void setAdditionalFunctions()
        {
            CreateTaskmanager taskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];


            ActionInfo submitAction = new ActionInfo();
            submitAction.ActionName = "Save";
            submitAction.ControllerName = "EventRegistration";
            submitAction.AreaName = "EMM";

            ActionInfo cancelAction = new ActionInfo();
            cancelAction.ActionName = "Cancel";
            cancelAction.ControllerName = "EventRegistration";
            cancelAction.AreaName = "EMM";

            taskManager.Actions.Add(CreateTaskmanager.SUBMIT_ACTION, submitAction);
            taskManager.Actions.Add(CreateTaskmanager.CANCEL_ACTION, cancelAction);

            Session["EventRegistrationTaskmanager"] = taskManager;

        }


        #endregion

        #region delete reg

        public ActionResult DeleteRegistration(long id)
        {
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (UserManager userManager = new UserManager())
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
                    User user = userManager.FindByIdAsync(reg.Person.Id).Result;
                    email = user.Email;
                }
                else
                    email = reg.Data.GetElementsByTagName("Email")[0].InnerText;

                EmailHelper.SendEmailNotification("deleted", email, "", reg.Data, reg.Event, url);
            }

            return RedirectToAction("Show");

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
                    else
                        email = reg.Data.GetElementsByTagName("Email")[0].InnerText;

                    EmailHelper.SendEmailNotification("remove_from_waiting_list", email, "", reg.Data, reg.Event, url);

                }
            }
        }


        #endregion

        #region Xml to DataTable

        private DataTable GetEventResults(long eventId)
        {
            DataTable results = new DataTable();
            results.Columns.Add("Id");
            results.Columns.Add("Deleted");

            using (EventRegistrationManager erManager = new EventRegistrationManager())
            {
                List<EventRegistration> eventRegistrations = erManager.GetAllRegistrationsByEvent(eventId);

                if (eventRegistrations.Count != 0)
                {
                    XmlNodeReader xmlNodeReader = new XmlNodeReader(eventRegistrations[0].Data);
                    results = CreateDataTableColums(results, XElement.Load(xmlNodeReader));
                    xmlNodeReader.Dispose();
                }

                foreach (EventRegistration er in eventRegistrations)
                {
                    XmlNodeReader xmlNodeReader = new XmlNodeReader(er.Data);
                    results.Rows.Add(AddDataRow(XElement.Load(xmlNodeReader), results, er.Deleted.ToString(), er.Id));
                    xmlNodeReader.Dispose();
                }
            }

            return results;
        }

        private DataTable GetWaitingListResults(long eventId)
        {
            DataTable results = new DataTable();
            results.Columns.Add("Id");
            results.Columns.Add("Deleted");
            results.Columns.Add("Action");

            using (EventRegistrationManager erManager = new EventRegistrationManager())
            {
                List<EventRegistration> eventRegistrations = erManager.GetAllWaitingListRegsByEvent(eventId);

                if (eventRegistrations.Count != 0)
                {
                    XmlNodeReader xmlNodeReader = new XmlNodeReader(eventRegistrations[0].Data);
                    results = CreateDataTableColums(results, XElement.Load(xmlNodeReader));
                    xmlNodeReader.Dispose();
                }

                foreach (EventRegistration er in eventRegistrations)
                {
                    XmlNodeReader xmlNodeReader = new XmlNodeReader(er.Data);
                    results.Rows.Add(AddDataRow(XElement.Load(xmlNodeReader), results, er.Deleted.ToString(), er.Id));
                    xmlNodeReader.Dispose();
                }
            }

            return results;
        }
        //private DataTable GetEventRegistration(long eventId, XDocument data)
        //{
        //    DataTable results = new DataTable();

        //    using (EventRegistrationManager erManager = new EventRegistrationManager())
        //    {
        //        XmlNodeReader xmlNodeReader = new XmlNodeReader(XmlMetadataWriter.ToXmlDocument(data));
        //        results = CreateDataTableColums(results, XElement.Load(xmlNodeReader));
        //        results.Rows.Add(AddDataRow(XElement.Load(xmlNodeReader), results));
        //        xmlNodeReader.Dispose();
        //    }
        //    return results;
        //}

        private DataTable CreateDataTableColums(DataTable dataTable, XElement x)
        {
            DataTable dt = dataTable;
            // build your DataTable
            foreach (XElement xe in x.Descendants())
            {
                if (!xe.HasElements)
                {
                    DataColumn dc = new DataColumn();
                    string colName = xe.Name.ToString().Replace("Type", "");
                    dc.Caption = colName;
                    dc.ColumnName = colName;
                    dt.Columns.Add(dc); // add columns to your dt
                }
            }

            return dt;
        }

        private DataRow AddDataRow(XElement x, DataTable dt, string deleted, long id)
        {
            DataRow dr = dt.NewRow();
            dr["Id"] = id;
            dr["Deleted"] = deleted;
            foreach (XElement xe in x.Descendants())
            {
                if (!xe.HasElements)
                {
                    string value = xe.Value.Replace("\r\n", " ");
                    value = value.Replace("\n", " ");
                    value = HttpUtility.HtmlDecode(value);
                    dr[xe.Name.ToString().Replace("Type", "")] = value;  //add in the values
                }
            }

            return dr;
        }

        #endregion


    }

}
