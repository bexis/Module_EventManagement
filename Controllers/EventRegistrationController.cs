using BExIS.Dcm.CreateDatasetWizard;
using BExIS.Dcm.Wizard;
using BExIS.Dlm.Entities.Common;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Dlm.Services.TypeSystem;
using BExIS.Emm.Entities.Event;
using BExIS.Emm.Services.Event;
using BExIS.IO.Transform.Validation.Exceptions;
using BExIS.Modules.EMM.UI.Helpers;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Utilities;
using BExIS.Utils.Data.MetadataStructure;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Telerik.Web.Mvc;
using Vaiona.Persistence.Api;
using Vaiona.Utils.Cfg;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;


namespace BExIS.Modules.EMM.UI.Controllers
{
    public class EventRegistrationController : Controller
    {
        private CreateTaskmanager TaskManager;
        private MetadataStructureUsageHelper metadataStructureUsageHelper = new MetadataStructureUsageHelper();

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
            using (EventManager eManger = new EventManager())
            using (SubjectManager subManager = new SubjectManager())
            {
                List<Event> allEvents = eManger.GetAllEvents().ToList();

                List<EventRegistrationModel> availableEvents = new List<EventRegistrationModel>();

                using (EventRegistrationManager erManager = new EventRegistrationManager())
                {
                    User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

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
                }

                return availableEvents;
            }
        }

        public ActionResult LogInToEvent(string id, string view_only = "false", string ref_id = null)
        {
            Session["DefaultEventInformation"] = null;
            LogInToEventModel model = new LogInToEventModel(long.Parse(id), bool.Parse(view_only), ref_id);

            //check if it is an edit
            using (SubjectManager subManager = new SubjectManager())
            {
                using (EventRegistrationManager erManager = new EventRegistrationManager())
                {
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
                }
            }

            return PartialView("_logInToEvent", model);
        }

        #endregion

        #region Load Registration Form

        public ActionResult LoadForm(LogInToEventModel model)

        {
            using (EventManager eManager = new EventManager())
            {
                Event e = eManager.EventRepo.Get(model.EventId);

                if (e.LogInPassword != model.LogInPassword)
                    ModelState.AddModelError("passwort", "The event passwort is wrong.");

                if (ModelState.IsValid)
                {
                    //add default value to session
                    DefaultEventInformation defaultEventInformation = new DefaultEventInformation();
                    defaultEventInformation.EventName = e.Name;
                    if(!String.IsNullOrEmpty(e.EventDate))
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

                    if (model.Edit)
                    {
                        using (EventRegistrationManager erManager = new EventRegistrationManager())
                        using (SubjectManager subManager = new SubjectManager())
                        {
                            User user = subManager.Subjects.Where(a => a.Name == HttpContext.User.Identity.Name).FirstOrDefault() as User;

                            if (user != null)
                            {
                                EventRegistration reg = erManager.GetRegistrationByUserAndEvent(user.Id, e.Id);

                                XmlNodeReader xmlNodeReader = new XmlNodeReader(reg.Data);
                                TaskManager.AddToBus(CreateTaskmanager.METADATA_XML, reg.Data);
                                xmlNodeReader.Dispose();
                            }
                            else if (model.RefId != null)
                            {
                                EventRegistration reg = erManager.GetRegistrationByRefIdAndEvent(model.RefId, e.Id);
                                XmlNodeReader xmlNodeReader = new XmlNodeReader(reg.Data);
                                TaskManager.AddToBus(CreateTaskmanager.METADATA_XML, reg.Data);
                                xmlNodeReader.Dispose();
                            }
                        }

                    }

                    TaskManager.AddToBus(CreateTaskmanager.SAVE_WITH_ERRORS, false);
                    if (model.ViewOnly == true)
                    {
                        TaskManager.AddToBus(CreateTaskmanager.LOCKED, true);
                    }

                    TaskManager.AddToBus(CreateTaskmanager.NO_IMPORT_ACTION, true);
                    TaskManager.AddToBus(CreateTaskmanager.INFO_ON_TOP_TITLE, "Event registration");
                    TaskManager.AddToBus(CreateTaskmanager.INFO_ON_TOP_DESCRIPTION, "<p><b>help</b></p>");


                    Session["EventRegistrationTaskmanager"] = TaskManager;

                    setAdditionalFunctions();

                    return Json(new { success = true, edit = model.Edit });


                    //return RedirectToAction("StartMetadataEditor", "Form", new { area = "DCM" });
                }
                else
                {
                    return PartialView("_logInToEvent", model);
                }
            }

        }

        //public ActionResult LoadMetadataForm()
        //{
        //    var view = this.Render("DCM", "Form", "StartMetadataEditor", new RouteValueDictionary()   
        //    {});

        //    return Content(view.ToHtmlString(), "text/html");
        //}

        public ActionResult LoadMetadataForm(bool fromEditMode = true)
        {
            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Register to Event", this.Session.GetTenant());

            var Model = new MetadataEditorModel();

            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

            if (TaskManager != null)
            {
                //FromCreateOrEditMode
                TaskManager.AddToBus(CreateTaskmanager.EDIT_MODE, fromEditMode);
                Model.FromEditMode = (bool)TaskManager.Bus[CreateTaskmanager.EDIT_MODE];

            //load empty metadata xml if needed
            if (!TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
            {
                        CreateXml();
            }

                var loaded = false;
                //check if formsteps are loaded
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.FORM_STEPS_LOADED))
                {
                    loaded = (bool)TaskManager.Bus[CreateTaskmanager.FORM_STEPS_LOADED];
                }

                // load form steps
                if (loaded == false && TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
                {
                    var metadataStrutureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);
                    // generate all steps
                    // one step for each complex type  in the metadata structure
                    AdvanceTaskManager(metadataStrutureId);
                }

                var stepInfoModelHelpers = new List<StepModelHelper>();

                // foreach step and the childsteps... generate a stepModelhelper
                foreach (var stepInfo in TaskManager.StepInfos)
                {
                    var stepModelHelper = GetStepModelhelper(stepInfo.Id);

                    if (stepModelHelper.Model == null)
                    {
                        if (stepModelHelper.UsageType.Equals(typeof(MetadataPackageUsage)))
                            stepModelHelper.Model = createPackageModel(stepInfo.Id, false);

                        if (stepModelHelper.UsageType.Equals(typeof(MetadataNestedAttributeUsage)))
                            stepModelHelper.Model = createCompoundModel(stepInfo.Id, false);

                        getChildModelsHelper(stepModelHelper);
                    }

                    stepInfoModelHelpers.Add(stepModelHelper);
                }

                Model.StepModelHelpers = stepInfoModelHelpers;

            }

            //set addtionaly functions
            Model.Actions = getAddtionalActions();

            if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

            ViewData["MetadataStructureID"] = TaskManager.Bus["MetadataStructureId"];
            return View("MetadataEditor", Model);

        }

        public ActionResult ReloadMetadataEditor(
            bool locked = false,
            bool show = false,
            bool created = false,
            long entityId = -1,
            bool fromEditMode = false,
            bool latestVersion = false
            )
        {
            ViewData["Locked"] = locked;
            ViewData["ShowOptional"] = show;

            ViewBag.Title = PresentationModel.GetViewTitleForTenant("Create Dataset", this.Session.GetTenant());
            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

            TaskManager?.AddToBus(CreateTaskmanager.SAVE_WITH_ERRORS, true);

            var stepInfoModelHelpers = new List<StepModelHelper>();

            foreach (var stepInfo in TaskManager.StepInfos)
            {
                var stepModelHelper = GetStepModelhelper(stepInfo.Id);

                if (stepModelHelper.Model == null)
                {
                    if (stepModelHelper.UsageType.Equals(typeof(MetadataPackageUsage)))
                        stepModelHelper.Model = createPackageModel(stepInfo.Id, false);

                    if (stepModelHelper.UsageType.Equals(typeof(MetadataNestedAttributeUsage)))
                        stepModelHelper.Model = createCompoundModel(stepInfo.Id, false);

                    getChildModelsHelper(stepModelHelper);
                }

                stepInfoModelHelpers.Add(stepModelHelper);
            }

            var Model = new MetadataEditorModel();
            Model.StepModelHelpers = stepInfoModelHelpers;
            Model.SaveWithErrors = Model.StepModelHelpers.Any(s => s.Model.ErrorList.Count() > 0);

            #region security permissions and authorisations check

            // set edit rigths

            bool hasAuthorizationRights = true;
            bool hasAuthenticationRigths = true;

            //Model.FromEditMode = true;

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
            {
                long metadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);
                //Model.Import = IsImportAvavilable(metadataStructureId);
                Model.Import = false;

            }

            #endregion security permissions and authorisations check

            //set addtionaly functions
            Model.Actions = getAddtionalActions();

            //save with errors?
            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.SAVE_WITH_ERRORS))
            {
                Model.SaveWithErrors = (bool)TaskManager.Bus[CreateTaskmanager.SAVE_WITH_ERRORS];
            }

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.NO_IMPORT_ACTION))
            {
                Model.Import = !(bool)TaskManager.Bus[CreateTaskmanager.NO_IMPORT_ACTION];
            }

            //Replace the title of the info box on top
            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.INFO_ON_TOP_TITLE))
            {
                ViewBag.Title = PresentationModel.GetViewTitleForTenant(Convert.ToString(TaskManager.Bus[CreateTaskmanager.INFO_ON_TOP_TITLE]), this.Session.GetTenant());
            }

            //Replace the description in the info box on top
            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.INFO_ON_TOP_DESCRIPTION))
            {
                Model.HeaderHelp = Convert.ToString(TaskManager.Bus[CreateTaskmanager.INFO_ON_TOP_DESCRIPTION]);
            }

            Model.Created = created;
            Model.FromEditMode = fromEditMode;
            Model.DatasetId = entityId;
            Model.LatestVersion = latestVersion;

            //set title
            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.ENTITY_TITLE))
            {
                if (TaskManager.Bus[CreateTaskmanager.ENTITY_TITLE] != null)
                    Model.DatasetTitle = TaskManager.Bus[CreateTaskmanager.ENTITY_TITLE].ToString();
            }
            else
                Model.DatasetTitle = "No Title available.";

            ViewData["MetadataStructureID"] = TaskManager.Bus["MetadataStructureId"];
            return PartialView("MetadataEditor", Model);
        }

        private Dictionary<string, ActionInfo> getAddtionalActions()
        {
            var TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];
            if (TaskManager.Actions.Any())
            {
                return TaskManager.Actions;
            }

            return new Dictionary<string, ActionInfo>();
        }

        public ActionResult Cancel()
        {
            return Json(new { result = "redirect", url = Url.Action("EventRegistration", "EventRegistration", new { area = "EMM"}) }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Save()
        {
            using (EventManager eManager = new EventManager())
            using (EventRegistrationManager erManager = new EventRegistrationManager())
            using (SubjectManager subManager = new SubjectManager())
            {

                CreateTaskmanager taskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

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
                if (reg != null)
                {
                    if (e.EditAllowed != true)
                    {
                        SendEmailNotification("resend", email, ref_id, data, e, user);
                        return RedirectToAction("EventRegistrationPatial", new { message = "Update of your previous registration is not allowed. You registration details are send to your Email adress again.", message_type = "error" });
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
                        if (countRegs >= e.ParticipantsLimitation)
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
                        message = message + " To view " + change + " your registration log in or follow the link send via email.";
                    }
                    else
                    {
                        message = message + " To view " + change + " your registration follow the link send via email.";
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

                return Json(new { result = "redirect", url = Url.Action("EventRegistration", "EventRegistration", new { area = "EMM", ref_id = ref_id }) }, JsonRequestBehavior.AllowGet);
            }        
        }

        #endregion

        #region Validation

        public ActionResult Validate()
        {
            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

            if (TaskManager != null && TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_STEP_MODEL_HELPER))
            {
                var stepInfoModelHelpers = (List<StepModelHelper>)TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER];
                ValidateModels(stepInfoModelHelpers.Where(s => s.Activated && s.IsParentActive()).ToList());
            }

            return RedirectToAction("ReloadMetadataEditor", "EventRegistration");
        }

        //XX number of index des values nötig
        [HttpPost]
        public ActionResult ValidateMetadataAttributeUsage(string value, int id, int parentid, string parentname, int number, int parentModelNumber, int parentStepId)
        {
            //delete all white spaces from start and end
            value = value.Trim();

            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];
            var stepModelHelper = GetStepModelhelper(parentStepId);

            var ParentUsageId = stepModelHelper.UsageId;
            var parentUsage = loadUsage(stepModelHelper.UsageId, stepModelHelper.UsageType);
            var pNumber = stepModelHelper.Number;

            metadataStructureUsageHelper = new MetadataStructureUsageHelper();

            var metadataAttributeUsage = metadataStructureUsageHelper.GetChildren(parentUsage.Id, parentUsage.GetType()).Where(u => u.Id.Equals(id)).FirstOrDefault();

            //Path.Combine(AppConfiguration.GetModuleWorkspacePath("dcm"),"x","file.xml");

            //UpdateXml
            var metadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);
            var model = FormHelper.CreateMetadataAttributeModel(metadataAttributeUsage, parentUsage, metadataStructureId, parentModelNumber, stepModelHelper.StepId);

            //check if datatype is a datetime then check display pattern and manipulate the incoming string
            if (model.SystemType.Equals(typeof(DateTime).Name))
            {
                if (!string.IsNullOrEmpty(model.DisplayPattern))
                {
                    var dt = DateTime.Parse(value);
                    value = dt.ToString(model.DisplayPattern);
                }
            }

            model.Value = value;
            model.Number = number;

            UpdateAttribute(parentUsage, parentModelNumber, metadataAttributeUsage, number, value, stepModelHelper.XPath);

            ViewData["Xpath"] = stepModelHelper.XPath; // set Xpath for idbyxapth

            if (stepModelHelper.Model.MetadataAttributeModels.Where(a => a.Id.Equals(id) && a.Number.Equals(number)).Count() > 0)
            {
                var selectedMetadatAttributeModel = stepModelHelper.Model.MetadataAttributeModels.Where(a => a.Id.Equals(id) && a.Number.Equals(number)).FirstOrDefault();
                // select the attributeModel and change the value
                selectedMetadatAttributeModel.Value = model.Value;
                selectedMetadatAttributeModel.Errors = validateAttribute(selectedMetadatAttributeModel);

                Session["EventRegistrationTaskmanager"] = TaskManager;

                return PartialView("_metadataAttributeView", selectedMetadatAttributeModel);
            }
            else
            {
                stepModelHelper.Model.MetadataAttributeModels.Add(model);
                return PartialView("_metadataAttributeView", model);
            }
        }

        private void UpdateAttribute(BaseUsage parentUsage, int packageNumber, BaseUsage attribute, int number, object value, string parentXpath)
        {
            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];
            var metadataXml = getMetadata();
            var xmlMetadataWriter = new XmlMetadataWriter(XmlNodeMode.xPath);

            metadataXml = xmlMetadataWriter.Update(metadataXml, attribute, number, value, metadataStructureUsageHelper.GetNameOfType(attribute), parentXpath);

            TaskManager.Bus[CreateTaskmanager.METADATA_XML] = metadataXml;
            // locat path
            var path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DCM"), "metadataTemp.Xml");
            metadataXml.Save(path);
        }

        private List<Error> validateAttribute(MetadataAttributeModel aModel)
        {
            var errors = new List<Error>();
            //optional check
            if (aModel.MinCardinality > 0 && (aModel.Value == null || String.IsNullOrEmpty(aModel.Value.ToString())))
                errors.Add(new Error(ErrorType.MetadataAttribute, "is required", new object[] { aModel.DisplayName, aModel.Value, aModel.Number, aModel.ParentModelNumber, aModel.Parent.Label }));
            else
                if (aModel.MinCardinality > 0 && String.IsNullOrEmpty(aModel.Value.ToString()))
                errors.Add(new Error(ErrorType.MetadataAttribute, "is required", new object[] { aModel.DisplayName, aModel.Value, aModel.Number, aModel.ParentModelNumber, aModel.Parent.Label }));

            //check datatype
            if (aModel.Value != null && !String.IsNullOrEmpty(aModel.Value.ToString()))
            {
                if (!DataTypeUtility.IsTypeOf(aModel.Value, aModel.SystemType))
                {
                    errors.Add(new Error(ErrorType.MetadataAttribute, "Value can´t convert to the type: " + aModel.SystemType + ".", new object[] { aModel.DisplayName, aModel.Value, aModel.Number, aModel.ParentModelNumber, aModel.Parent.Label }));
                }
                else
                {
                    var type = Type.GetType("System." + aModel.SystemType);
                    var value = Convert.ChangeType(aModel.Value, type);

                    // check Constraints
                    foreach (var constraint in aModel.GetMetadataAttribute().Constraints)
                    {
                        if (value != null && !constraint.IsSatisfied(value))
                        {
                            errors.Add(new Error(ErrorType.MetadataAttribute, constraint.ErrorMessage, new object[] { aModel.DisplayName, aModel.Value, aModel.Number, aModel.ParentModelNumber, aModel.Parent.Label }));
                        }
                    }
                }
            }

            //// dataset title node should be check if its exit or not
            //if (errors.Count == 0 && aModel.DataType.ToLower().Contains("string"))
            //{
            //    XmlReader reader =;
            //}

            if (errors.Count == 0)
                return null;
            else
                return errors;
        }

        private void ValidateModels(List<StepModelHelper> stepModelHelpers)
        {
            List<Error> tmp = new List<Error>();

            foreach (var stepModelHelper in stepModelHelpers)
            {
                //only check if step is a instance
                if (stepModelHelper.Model.StepInfo.IsInstanze)
                {
                    // if model exist then validate attributes
                    if (stepModelHelper.Model != null)
                    {
                        foreach (var metadataAttrModel in stepModelHelper.Model.MetadataAttributeModels)
                        {
                            metadataAttrModel.Errors = validateAttribute(metadataAttrModel);

                            if (metadataAttrModel.Errors != null) tmp.AddRange(metadataAttrModel.Errors);

                            //if (metadataAttrModel.Errors.Count > 0)
                            //    step.stepStatus = StepStatus.error;
                        }
                    }
                    // else check for required elements
                    else
                    {
                        if (metadataStructureUsageHelper.HasUsagesWithSimpleType(stepModelHelper.UsageId, stepModelHelper.UsageType))
                        {
                            //foreach (var metadataAttrModel in stepModeHelper.Model.MetadataAttributeModels)
                            //{
                            //    metadataAttrModel.Errors = validateAttribute(metadataAttrModel);
                            //    if (metadataAttrModel.Errors.Count>0)
                            //        step.stepStatus = StepStatus.error;
                            //}

                            //if(MetadataStructureUsageHelper.HasRequiredSimpleTypes(stepModeHelper.Usage))
                            //{
                            //    StepInfo step = TaskManager.Get(stepModeHelper.StepId);
                            //    if (step != null && step.IsInstanze)
                            //    {
                            //        Error error = new Error(ErrorType.Other, String.Format("{0} : {1} {2}", "Step: ", stepModeHelper.Usage.Label, "is not valid. There are fields that are required and not yet completed are."));

                            //        errors.Add(new Tuple<StepInfo, List<Error>>(step, tempErrors));

                            //        step.stepStatus = StepStatus.error;
                            //    }
                            //}
                        }
                    }
                }
            }
        }

        private List<Error> validateStep(AbstractMetadataStepModel pModel)
        {
            var errorList = new List<Error>();

            if (pModel != null)
            {
                foreach (var m in pModel.MetadataAttributeModels)
                {
                    var temp = validateAttribute(m);
                    if (temp != null)
                        errorList.AddRange(temp);
                }
            }

            if (errorList.Count == 0)
                return null;
            else
                return errorList;
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
            return View("EventRegistrationResults", GetEventResults(id));
        }


        #endregion

        #region Xml to DataTable

        private DataTable GetEventResults(long eventId)
        {
            //string path = AppConfiguration.GetModuleWorkspacePath("EMM");
            //XDocument xDoc = XDocument.Load(Path.Combine(path, "workshopReg4.xml"));

            DataTable results = new DataTable();

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
                    results.Rows.Add(AddDataRow(XElement.Load(xmlNodeReader), results));
                    xmlNodeReader.Dispose();

                }
            }

            return results;
        }

        private DataTable GetEventRegistration(long eventId, XDocument data)
        {
            DataTable results = new DataTable();
            using (EventRegistrationManager erManager = new EventRegistrationManager())

            {

#pragma warning disable CA2000 // Objekte verwerfen, bevor Bereich verloren geht	    
                results = CreateDataTableColums(results, XElement.Load(new XmlNodeReader(XmlMetadataWriter.ToXmlDocument(data))));
                results.Rows.Add(AddDataRow(XElement.Load(new XmlNodeReader(XmlMetadataWriter.ToXmlDocument(data))), results));
#pragma warning restore CA2000 // Objekte verwerfen, bevor Bereich verloren geht	             


                return results;
            }
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
            using (MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider())
            {
                byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes("abd_" + email));

                for (int i = 0; i < bytes.Length; i++)
                {
                    hash.Append(bytes[i].ToString("x2"));
                }
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
                    mail_message = "you registered to " + e.Name + "<br/>";
                    break;
                case "succesfully_registered_waiting_list":
                    subject = "Registration confirmation for" + e.Name + " - Event fully booked";
                    mail_message = "For registered to " + e.Name + "you are currently on a replacment place.";
                    break;
                case "updated":
                    subject = "Registration update confirmation for " + e.Name;
                    mail_message = "you updated your registration for " + e.Name + "<br/>";
                    break;
                case "resend":
                    subject = "Resend of registration confirmation for " + e.Name;
                    mail_message = "your registration for " + e.Name + "<br/>";
                    break;
            }

            string details = "";
            DataTable res = GetEventRegistration(e.Id, data);
            int row_count = res.Columns.Count;
            for (int i = 0; i < row_count; i++)
            {
                details = details + res.Columns[i].ToString().Split('/')[res.Columns[i].ToString().Split('/').Length - 2] + ":  " + res.Rows[0][i] + "<br/>";
            }


            string body = "Dear " + first_name + " " + last_name + ", " + "<br/><br/>" +
                 mail_message +
                 "<br/> Your registration details are: <br/><br/>" + 
                 details + "<br/><br/>" +
                 "To view or change your registration follow this link: " + url + "/emm/EventRegistration/EventRegistration/?ref_id=" + ref_id + "<br/><br/>" +
                 "Sincerely yours, <br/>" +
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

        private void CreateXml()
        {
            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

            // load metadatastructure with all packages and attributes

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATASTRUCTURE_ID))
            {
                var xmlMetadatWriter = new XmlMetadataWriter(XmlNodeMode.xPath);

                var metadataXml = xmlMetadatWriter.CreateMetadataXml(Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]));

                //local path
                //string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DCM"), "metadataTemp.Xml");

                TaskManager.AddToBus(CreateTaskmanager.METADATA_XML, metadataXml);



                //setup loaded
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.SETUP_LOADED))
                    TaskManager.Bus[CreateTaskmanager.SETUP_LOADED] = true;
                else
                    TaskManager.Bus.Add(CreateTaskmanager.SETUP_LOADED, true);

            }
        }

        private void AdvanceTaskManager(long MetadataStructureId)
        {
            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

            var metadataStructureManager = new MetadataStructureManager();

            try
            {
                var metadataPackageList = metadataStructureManager.GetEffectivePackages(MetadataStructureId).ToList();

                var stepModelHelperList = new List<StepModelHelper>();
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_STEP_MODEL_HELPER))
                {
                    TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER] = stepModelHelperList;
                }
                else
                {
                    TaskManager.Bus.Add(CreateTaskmanager.METADATA_STEP_MODEL_HELPER, stepModelHelperList);
                }

                TaskManager.StepInfos = new List<StepInfo>();

                StepModelHelper stepModelHelper;
                StepInfo si;
                foreach (var mpuId in metadataPackageList.Select(p => p.Id))
                {
                    MetadataPackageUsage mpu = metadataStructureManager.PackageUsageRepo.Get(mpuId);

                    //only add none optional usages
                    si = new StepInfo(mpu.Label)
                    {
                        Id = TaskManager.GenerateStepId(),
                        parentTitle = mpu.MetadataPackage.Name,
                        Parent = TaskManager.Root,
                        IsInstanze = false,
                    };

                    TaskManager.StepInfos.Add(si);
                    stepModelHelper = new StepModelHelper(si.Id, 1, mpu.Id, mpu.Label, GetUsageAttrName(mpu), mpu.GetType(), "Metadata//" + mpu.Label.Replace(" ", string.Empty) + "[1]", null, mpu.Extra);

                    stepModelHelperList.Add(stepModelHelper);

                    si = AddStepsBasedOnUsage(mpu, si, "Metadata//" + mpu.Label.Replace(" ", string.Empty) + "[1]", stepModelHelper);
                    TaskManager.Root.Children.Add(si);

                    //TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER] = stepModelHelperList;
                }

                TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER] = stepModelHelperList;
                Session["EventRegistrationTaskmanager"] = TaskManager;
            }
            finally
            {
                metadataStructureManager.Dispose();
            }
        }

        private StepInfo AddStepsBasedOnUsage(BaseUsage usage, StepInfo current, string parentXpath, StepModelHelper parent)
        {
            // genertae action, controller base on usage
            var actionName = "";
            var childName = "";
            var min = usage.MinCardinality;

            if (usage is MetadataPackageUsage)
            {
                actionName = "SetMetadataPackageInstanze";
                childName = ((MetadataPackageUsage)usage).MetadataPackage.Name;
            }
            else
            {
                actionName = "SetMetadataCompoundAttributeInstanze";

                if (usage is MetadataNestedAttributeUsage)
                    childName = ((MetadataNestedAttributeUsage)usage).Member.Name;

                if (usage is MetadataAttributeUsage)
                    childName = ((MetadataAttributeUsage)usage).MetadataAttribute.Name;
            }

            var list = new List<StepInfo>();
            var stepHelperModelList = (List<StepModelHelper>)TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER];

            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
            {
                var xMetadata = getMetadata();

                var x = new XElement("null");
                var elements = new List<XElement>();

                var keyValueDic = new Dictionary<string, string>();
                keyValueDic.Add("id", usage.Id.ToString());

                if (usage is MetadataPackageUsage)
                {
                    keyValueDic.Add("type", BExIS.Xml.Helpers.XmlNodeType.MetadataPackageUsage.ToString());
                    elements = XmlUtility.GetXElementsByAttribute(usage.Label, keyValueDic, xMetadata).ToList();
                }
                else
                {
                    keyValueDic.Add("type", BExIS.Xml.Helpers.XmlNodeType.MetadataAttributeUsage.ToString());
                    elements = XmlUtility.GetXElementsByAttribute(usage.Label, keyValueDic, xMetadata).ToList();
                }

                x = elements.FirstOrDefault();

                if (x != null && !x.Name.Equals("null"))
                {
                    var xelements = x.Elements();

                    if (xelements.Count() > 0)
                    {
                        var counter = 0;
                        var title = "";
                        Int64 id = 0;
                        var xPath = "";
                        StepInfo s;
                        StepModelHelper newStepModelHelper = new StepModelHelper();

                        foreach (var element in xelements)
                        {
                            counter++;
                            title = counter.ToString(); //usage.Label+" (" + counter + ")";
                            id = Convert.ToInt64((element.Attribute("roleId")).Value.ToString());

                            s = new StepInfo(title)
                            {
                                Id = TaskManager.GenerateStepId(),
                                Parent = current,
                                IsInstanze = true,
                                HasContent = metadataStructureUsageHelper.HasUsagesWithSimpleType(usage.Id, usage.GetType()),
                            };

                            xPath = parentXpath + "//" + childName.Replace(" ", string.Empty) + "[" + counter + "]";

                            if (TaskManager.Root.Children.Where(z => z.title.Equals(title)).Count() == 0)
                            {
                                newStepModelHelper = new StepModelHelper(s.Id, counter, usage.Id, usage.Label, GetUsageAttrName(usage), usage.GetType(), xPath, parent, usage.Extra);
                                stepHelperModelList.Add(newStepModelHelper);

                                s.Children = GetChildrenSteps(usage.Id, usage.GetType(), s, xPath, newStepModelHelper);

                                current.Children.Add(s);
                            }
                        }
                    }
                }

                //TaskManager.AddToBus(CreateDatasetTaskmanager.METADATAPACKAGE_IDS, MetadataPackageDic);
            }
            return current;
        }

        private List<StepInfo> GetChildrenSteps(long usageId, Type type, StepInfo parent, string parentXpath, StepModelHelper parentStepModelHelper)
        {
            var childrenSteps = new List<StepInfo>();
            var childrenUsages = metadataStructureUsageHelper.GetCompoundChildrens(usageId, type);
            var stepHelperModelList = (List<StepModelHelper>)TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER];

            if (childrenUsages.Count() > 0)
            {
                var xPath = "";
                var complex = false;
                var actionName = "";
                var attrName = "";

                StepInfo s;
                var newStepModelHelper = new StepModelHelper();

                foreach (var u in childrenUsages)
                {
                    //var u = loadUsage(id, type);
                    xPath = parentXpath + "//" + u.Label.Replace(" ", string.Empty) + "[1]";
                    complex = false;
                    actionName = "";
                    attrName = "";

                    if (u is MetadataPackageUsage)
                    {
                        actionName = "SetMetadataPackage";
                    }
                    else
                    {
                        actionName = "SetMetadataCompoundAttribute";

                        if (u is MetadataAttributeUsage)
                        {
                            var mau = (MetadataAttributeUsage)u;
                            if (mau.MetadataAttribute.Self is MetadataCompoundAttribute)
                            {
                                complex = true;
                                attrName = mau.MetadataAttribute.Self.Name;
                            }
                        }

                        if (u is MetadataNestedAttributeUsage)
                        {
                            var mau = (MetadataNestedAttributeUsage)u;
                            if (mau.Member.Self is MetadataCompoundAttribute)
                            {
                                complex = true;
                                attrName = mau.Member.Self.Name;
                            }
                        }
                    }

                    if (complex)
                    {
                        s = new StepInfo(u.Label)
                        {
                            Id = TaskManager.GenerateStepId(),
                            parentTitle = attrName,
                            Parent = parent,
                            IsInstanze = false,
                            GetActionInfo = new ActionInfo
                            {
                                ActionName = actionName,
                                ControllerName = "CreateSetMetadataPackage",
                                AreaName = "DCM"
                            },

                            PostActionInfo = new ActionInfo
                            {
                                ActionName = actionName,
                                ControllerName = "CreateSetMetadataPackage",
                                AreaName = "DCM"
                            }
                        };

                        if (TaskManager.StepInfos.Where(z => z.Id.Equals(s.Id)).Count() == 0)
                        {
                            newStepModelHelper = new StepModelHelper(s.Id, 1, u.Id, u.Label, GetUsageAttrName(u), u.GetType(), xPath,
                                parentStepModelHelper, u.Extra);
                            stepHelperModelList.Add(newStepModelHelper);

                            s = AddStepsBasedOnUsage(u, s, xPath, newStepModelHelper);
                            childrenSteps.Add(s);
                        }
                    }
                }
            }

            return childrenSteps;
        }

        private StepModelHelper GetStepModelhelper(int stepId)
        {
            TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];
            if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_STEP_MODEL_HELPER))
            {
                return ((List<StepModelHelper>)TaskManager.Bus[CreateTaskmanager.METADATA_STEP_MODEL_HELPER]).Where(s => s.StepId.Equals(stepId)).FirstOrDefault();
            }

            return null;
        }

        private string GetUsageAttrName(BaseUsage usage)
        {
            if (usage.GetType().Equals(typeof(MetadataAttributeUsage)))
            {
                var u = (MetadataAttributeUsage)usage;
                return u.MetadataAttribute.Name;
            }
            if (usage.GetType().Equals(typeof(MetadataNestedAttributeUsage)))
            {
                var u = (MetadataNestedAttributeUsage)usage;
                return u.Member.Name;
            }
            if (usage.GetType().Equals(typeof(MetadataPackageUsage)))
            {
                var u = (MetadataPackageUsage)usage;
                return u.MetadataPackage.Name;
            }

            return null;
        }

        private XDocument getMetadata()
        {
            try
            {
                if (TaskManager == null) TaskManager = (CreateTaskmanager)Session["EventRegistrationTaskmanager"];

                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
                {
                    var metadata = TaskManager.Bus[CreateTaskmanager.METADATA_XML];

                    if (metadata is XDocument) return (XDocument)metadata;
                    else
                    {
                        if (metadata is XmlDocument)
                        {
                            return XmlUtility.ToXDocument((XmlDocument)metadata);
                        }
                    }
                }

                return new XDocument();
            }
            catch
            {
                return new XDocument();
            }
        }

        private MetadataPackageModel createPackageModel(int stepId, bool validateIt)
        {
            var stepInfo = TaskManager.Get(stepId);
            var stepModelHelper = GetStepModelhelper(stepId);

            var metadataPackageId = stepModelHelper.UsageId;
            var metadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);

            var mpu = (MetadataPackageUsage)loadUsage(stepModelHelper.UsageId, stepModelHelper.UsageType);
            var model = new MetadataPackageModel();

            model = FormHelper.CreateMetadataPackageModel(mpu, stepModelHelper.Number);
            model.ConvertMetadataAttributeModels(mpu, metadataStructureId, stepId);

            if (stepInfo.IsInstanze == false)
            {
                //get Instance
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
                {
                    var xMetadata = getMetadata();
                    model.ConvertInstance(xMetadata, stepModelHelper.XPath);
                }
            }
            else
            {
                if (stepModelHelper.Model != null)
                {
                    model = (MetadataPackageModel)stepModelHelper.Model;
                }
                else
                {
                    stepModelHelper.Model = model;
                }
            }

            model.StepInfo = stepInfo;

            return model;
        }

        private BaseUsage loadUsage(long Id, Type type)
        {
            if (type.Equals(typeof(MetadataAttributeUsage)))
                return this.GetUnitOfWork().GetReadOnlyRepository<MetadataAttributeUsage>().Get(Id);
            if (type.Equals(typeof(MetadataNestedAttributeUsage)))
                return this.GetUnitOfWork().GetReadOnlyRepository<MetadataNestedAttributeUsage>().Get(Id);
            if (type.Equals(typeof(MetadataPackageUsage)))
                return this.GetUnitOfWork().GetReadOnlyRepository<MetadataPackageUsage>().Get(Id);

            return null;
        }

        private MetadataCompoundAttributeModel createCompoundModel(int stepId, bool validateIt)
        {
            var stepInfo = TaskManager.Get(stepId);
            var stepModelHelper = GetStepModelhelper(stepId);

            var metadataStructureId = Convert.ToInt64(TaskManager.Bus[CreateTaskmanager.METADATASTRUCTURE_ID]);
            var Id = stepModelHelper.UsageId;

            BaseUsage usage = loadUsage(stepModelHelper.UsageId, stepModelHelper.UsageType);
            var model = FormHelper.CreateMetadataCompoundAttributeModel(usage, stepModelHelper.Number);

            // get children
            model.ConvertMetadataAttributeModels(usage, metadataStructureId, stepInfo.Id);
            model.StepInfo = TaskManager.Get(stepId);

            if (stepInfo.IsInstanze == false)
            {
                //get Instance
                if (TaskManager.Bus.ContainsKey(CreateTaskmanager.METADATA_XML))
                {
                    var xMetadata = getMetadata();
                    model.ConvertInstance(xMetadata, stepModelHelper.XPath);
                }
            }
            else
            {
                if (stepModelHelper.Model != null)
                {
                    model = (MetadataCompoundAttributeModel)stepModelHelper.Model;
                }
                else
                {
                    stepModelHelper.Model = model;
                }
            }

            //if (validateIt)
            //{
            //    //validate packages
            //    List<Error> errors = validateStep(stepModelHelper.Model);
            //    if (errors != null)
            //        model.ErrorList = errors;
            //    else
            //        model.ErrorList = new List<Error>();

            //}

            model.StepInfo = stepInfo;

            return model;
        }

        private StepModelHelper getChildModelsHelper(StepModelHelper stepModelHelper)
        {
            StepInfo currentStepInfo = stepModelHelper.Model.StepInfo;

            if (currentStepInfo.Children.Count > 0)
            {
                StepModelHelper childStepModelHelper;

                foreach (var childStep in currentStepInfo.Children)
                {
                    childStepModelHelper = GetStepModelhelper(childStep.Id);

                    if (childStepModelHelper.Model == null)
                    {
                        childStepModelHelper.Model = createModel(childStep.Id, false, childStepModelHelper.UsageType);

                        if (childStepModelHelper.Model.StepInfo.IsInstanze)
                            LoadSimpleAttributesForModelFromXml(childStepModelHelper);
                    }

                    childStepModelHelper = getChildModelsHelper(childStepModelHelper);

                    stepModelHelper.Childrens.Add(childStepModelHelper);
                }
            }

            return stepModelHelper;
        }

       
        #endregion

        #region Models

        private AbstractMetadataStepModel createModel(int stepId, bool validateIt, Type usageType)
        {
            if (usageType.Equals(typeof(MetadataPackageUsage)))
            {
                return createPackageModel(stepId, validateIt);
            }

            return createCompoundModel(stepId, validateIt);
        }

        private void setStepModelActive(StepModelHelper model)
        {
            model.Activated = true;
            if (model.Parent != null)
                setStepModelActive(model.Parent);
        }

        private AbstractMetadataStepModel LoadSimpleAttributesForModelFromXml(StepModelHelper stepModelHelper)
        {
            TaskManager = (CreateTaskmanager)Session["CreateDatasetTaskmanager"];
            var metadata = getMetadata();

            var complexElement = XmlUtility.GetXElementByXPath(stepModelHelper.XPath, metadata);
            var additionalyMetadataAttributeModel = new List<MetadataAttributeModel>();

            foreach (var simpleMetadataAttributeModel in stepModelHelper.Model.MetadataAttributeModels)
            {
                var numberOfSMM = 1;
                if (complexElement != null)
                {
                    //Debug.WriteLine("XXXXXXXXXXXXXXXXXXXX");
                    //Debug.WriteLine(simpleMetadataAttributeModel.Source.Label);
                    var childs = XmlUtility.GetChildren(complexElement).Where(e => e.Attribute("id").Value.Equals(simpleMetadataAttributeModel.Id.ToString()));

                    if (childs.Any())
                        numberOfSMM = childs.First().Elements().Count();
                }

                for (var i = 1; i <= numberOfSMM; i++)
                {
                    var xpath = stepModelHelper.GetXPathFromSimpleAttribute(simpleMetadataAttributeModel.Id, i);
                    var simpleElement = XmlUtility.GetXElementByXPath(xpath, metadata);

                    if (i == 1)
                    {
                        if (simpleElement != null && !String.IsNullOrEmpty(simpleElement.Value))
                        {
                            simpleMetadataAttributeModel.Value = simpleElement.Value;

                            #region entity mapping

                            // if this simple attr is linked to a enity, some attr need to get from the xelement and create a url for the model
                            if (simpleElement.Attributes().Any(a => a.Name.LocalName.ToLowerInvariant().Equals("entityid")))
                            {
                                long id = 0;
                                int version = 0;
                                string idAsString = simpleElement.Attributes().FirstOrDefault(a => a.Name.LocalName.ToLowerInvariant().Equals("entityid"))?.Value;
                                string versionAsString = simpleElement.Attributes().FirstOrDefault(a => a.Name.LocalName.ToLowerInvariant().Equals("entityversion"))?.Value;

                                if (Int64.TryParse(idAsString, out id) &&
                                    Int32.TryParse(versionAsString, out version))
                                {
                                    string server = this.Request.Url.GetLeftPart(UriPartial.Authority);
                                    string url = server + "/DDM/Data/Show/" + id.ToString() + "?version=" + version;

                                    simpleMetadataAttributeModel.EntityUrl = url;
                                }
                            }

                            #endregion entity mapping

                            // if at least on item has a value, the parent should be activated
                            setStepModelActive(stepModelHelper);
                        }
                    }
                    else
                    {
                        var newMetadataAttributeModel = simpleMetadataAttributeModel.Kopie(i, numberOfSMM);
                        newMetadataAttributeModel.Value = simpleElement.Value;
                        if (i == numberOfSMM) newMetadataAttributeModel.last = true;
                        additionalyMetadataAttributeModel.Add(newMetadataAttributeModel);
                    }
                }
            }

            foreach (var item in additionalyMetadataAttributeModel)
            {
                var tempList = stepModelHelper.Model.MetadataAttributeModels;

                var indexOfLastSameAttribute = tempList.IndexOf(tempList.Where(a => a.Id.Equals(item.Id)).Last());
                tempList.Insert(indexOfLastSameAttribute + 1, item);
            }

            return stepModelHelper.Model;
        }

        #endregion
    }

}
