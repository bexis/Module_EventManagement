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
                XmlNodeReader xmlNodeReader = new XmlNodeReader(XmlMetadataWriter.ToXmlDocument(data));
                results = CreateDataTableColums(results, XElement.Load(xmlNodeReader));
                results.Rows.Add(AddDataRow(XElement.Load(xmlNodeReader), results));
                xmlNodeReader.Dispose();
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


    }

}
