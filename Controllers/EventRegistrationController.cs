using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.Wizard;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.IO.Transform.Output;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Authorization;
using BExIS.Security.Services.Subjects;
using BExIS.Web.Shell.Areas.EMM.Models;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
    public class EventRegistrationController : Controller
    {
        public ActionResult EventRegistration()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Manage Event Registrations", this.Session.GetTenant());

            List<EventRegistrationModel> model = GetAvailableEvents();
            return View("AvailableEventsList", model);
        }

        public ActionResult EventRegistrationPatial()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Manage Event Registrations", this.Session.GetTenant());

            List<EventRegistrationModel> model = GetAvailableEvents();
            return PartialView("AvailableEventsList", model);
        }

        #region Register to Event

        [GridAction]
        public ActionResult AvailableEvents()
        {
            List<EventRegistrationModel> model = GetAvailableEvents();
            return View("AvailableEventsList", new GridModel<EventRegistrationModel> { Data = model });
        }

        private List<EventRegistrationModel> GetAvailableEvents()
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
                if (today >= e.StartDate && today < e.Deadline)
                {
                    EventRegistrationModel model = new EventRegistrationModel(e);
                    //check if user already registered
                    EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
                    if (reg != null)
                        model.AlreadyRegistered = true;

                    availableEvents.Add(model);
                }
            }

            return availableEvents;
        }

        public ActionResult LogInToEvent(string id)
        {
            LogInToEventModel model = new LogInToEventModel(long.Parse(id));

            //check if it is an edit
            SubjectManager subManager = new SubjectManager();
            EventRegistrationManager erManager = new EventRegistrationManager();
            User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;
            EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, long.Parse(id));
            if (reg != null)
            {
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
                    EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);

                    taskManager.AddToBus(CreateTaskmanager.METADATA_XML, XDocument.Load(new XmlNodeReader(reg.Data)));
                }

                taskManager.AddToBus(CreateTaskmanager.SAVE_WITH_ERRORS, false);

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

            var result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Copy" }, { "controllerName", "CreateDataset" }, { "area", "DCM" }, { "type", "copy" } });
            result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Reset" }, { "controllerName", "Form" }, { "area", "Form" }, { "type", "reset" } });
            result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Cancel" }, { "controllerName", "Form" }, { "area", "DCM" }, { "type", "cancel" } });
            result = this.Run("DCM", "Form", "SetAdditionalFunctions", new RouteValueDictionary() { { "actionName", "Save" }, { "controllerName", "EventRegistration" }, { "area", "EMM" }, { "type", "submit" } });

            var view = this.Render("DCM", "Form", "StartMetadataEditor", new RouteValueDictionary()
            {
            });

    
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

            string message = "";
            //check Participants Limitation
            if (e.ParticipantsLimitation != 0)
            {
                int countRegs = erManager.GetNumerOfRegistrationsByEvent(e.Id);
                if (countRegs > e.ParticipantsLimitation)
                {
                    message = "Number of participants has been reached. You are now on the waiting list.";
                }
            }

            User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;
            EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);
            if (reg != null)
            {
                reg.Data = XmlMetadataWriter.ToXmlDocument(data);
                erManager.UpdateEventRegistration(reg);
            }
            else
                erManager.CreateEventRegistration(XmlMetadataWriter.ToXmlDocument(data), e, user, false);

            ////Set permissions on event registration
            //PermissionManager pManager = new PermissionManager();

            //foreach (RightType rightType in Enum.GetValues(typeof(RightType)).Cast<RightType>())
            //{
            //    pManager.CreateDataPermission(user.Id, 2, resource.Id, rightType);
            //}

            // return replace by parialview succesfully registrated-  send mail
            return RedirectToAction("EventRegistrationPatial");
        }

        #endregion

        #region Show Event Registration Results

        public ActionResult Show()
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Show Event Results", this.Session.GetTenant());

            return View("EventRegistrationResults", new DataTable());
        }

        public ActionResult ExportToExcel(string eventName, string eventId)
        {
            eventName = "eventName";
            ExcelWriter excelWriter = new ExcelWriter();

            string path = excelWriter.CreateFile(eventName);

            //excelWriter.AddDataTableToExcel(GetEventResults(long.Parse(eventId)), path);

            return File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

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