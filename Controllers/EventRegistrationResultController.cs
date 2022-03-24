using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.IO.Transform.Output;
using BExIS.Modules.EMM.UI.Helper;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Authorization;
using BExIS.Security.Entities.Objects;
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
                    dr[xe.Name.ToString().Replace("Type", "")] = xe.Value; //add in the values
                }
            }

            return dr;
        }

        #endregion


    }

}
