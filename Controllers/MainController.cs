using BExIS.Modules.Lui.UI.Models;
using System.Web.Mvc;
using Vaiona.Web.Mvc.Models;
using Vaiona.Web.Extensions;
using System.Collections.Generic;
using System;
using System.Data;
using System.Linq;
using System.IO;
using BExIS.IO.Transform.Output;
using BExIS.Dlm.Services.Data;
using BExIS.Dlm.Services.DataStructure;

namespace BExIS.Modules.Lui.UI.Controllers
{
    public class MainController : Controller
    {
        #region constants
        // page title
        private static string TITLE = "LUI Calculation";

        // session variable names
        private static string SESSION_TABLE = "lui:resultTable";
        private static string SESSION_FILE = "lui:resultFile";

        // namespace for download files
        private static string FILE_NAMESPACE = Settings.get("lui:filename:namespace") as string;
        #endregion

        // GET: Main
        public ActionResult Index()
        {
            if( checkPreconditions() )
            {

                // set page title
                ViewBag.Title = PresentationModel.GetViewTitleForTenant(TITLE, this.Session.GetTenant());

                // show the view
                LUIQueryModel model = new LUIQueryModel();
                return View("Index", model);

            } else
            {

                // preconditions failed, show error page
                return View("Error");

            }
        }

        /// <summary>
        /// trigger calculation of LUI values
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CalculateLUI(LUIQueryModel model)
        {
            // set page title
            ViewBag.Title = PresentationModel.GetViewTitleForTenant(TITLE, this.Session.GetTenant());

            // do the calucaltion
            var results = CalculateLui.DoCalc(model);

            // store results in session
            Session[SESSION_TABLE] = results;
            if (null != Session[SESSION_FILE])
            {
                ((Dictionary<string, string>)Session[SESSION_FILE]).Clear();
            }

            return PartialView("_results", results);
        }

        
        /// <summary>
        /// prepare the serialized data file for download
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public ActionResult PrepareDownloadFile(string mimeType)
        {

            // if we have already a matching file cached, we can short circuit here
            if ((null != Session[SESSION_FILE]) && ((Dictionary<string, string>)Session[SESSION_FILE]).ContainsKey(mimeType))
            {
                return Json(new { error = false, mimeType = mimeType }, JsonRequestBehavior.AllowGet);
            }

            // helper class
            OutputDataManager outputDataManager = new OutputDataManager();

            // filename
            // use unix timestamp to make filenames unique
            string filename = Settings.get("lui:filename:download") as string;
            // https://stackoverflow.com/a/17632585/1169798
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            filename += "_" + unixTimestamp;

            // datastructure ID
            int dsId = (int)Settings.get("lui:datastructure");

            // depends on the requested type
            string path = "";
            switch (mimeType)
            {
                case "text/csv":
                case "text/tsv":
                    path = outputDataManager.GenerateAsciiFile(FILE_NAMESPACE, Session[SESSION_TABLE] as DataTable, filename, mimeType, dsId);
                    break;

                case "application/vnd.ms-excel.sheet.macroEnabled.12":
                case "application/vnd.ms-excel":
                    path = outputDataManager.GenerateExcelFile(FILE_NAMESPACE, Session[SESSION_TABLE] as DataTable, filename, dsId);
                    break;

                default:
                    Response.StatusCode = 420;
                    return Json(new { error = true, msg = "Unknown file-type: " + mimeType }, JsonRequestBehavior.AllowGet);
            }

            // store path in session for further download
            if (null == Session[SESSION_FILE])
            {
                Session[SESSION_FILE] = new Dictionary<string, string>();
            }
            ((Dictionary<string, string>)Session[SESSION_FILE])[mimeType] = path;

            return Json(new { error = false, mimeType = mimeType }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// return the serialized data file
        /// if the file does not exist yet, it will be created
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public ActionResult DownloadFile(string mimeType)
        {

            // make sure the file was created
            if ((null == Session[SESSION_FILE]) || !((Dictionary<string, string>)Session[SESSION_FILE]).ContainsKey(mimeType))
            {
                ActionResult res = PrepareDownloadFile(mimeType);

                // check, if everything went ok
                if (200 != Response.StatusCode)
                {
                    return res;
                }
            }

            // get file path
            string path = ((Dictionary<string, string>)Session[SESSION_FILE])[mimeType];

            // return file for download
            return File(path, mimeType, Path.GetFileName(path));
        }

    
        /// <summary>
        /// check for preconditions, so that we can do all computations
        /// * Link to LUI dataset
        /// * Link to result data structure
        /// </summary>
        /// <returns></returns>
        private bool checkPreconditions()
        {
            // check for LUI dataset
            DatasetManager dm = new DatasetManager();
            int luiId = (int)Settings.get("lui:dataset");
            bool exists = dm.DatasetRepo.Query()
                                        .Where(x => x.Id == luiId )
                                        .Any();
            if (!exists)
            {
                return false;
            }

            // check for export data structure
            DataStructureManager dsm = new DataStructureManager();
            int dsdId = (int)Settings.get("lui:datastructure");
            exists = dsm.StructuredDataStructureRepo.Query()
                                    .Where(x => x.Id == dsdId)
                                    .Any();
            if(!exists)
            {
                return false;
            }

            // if we came that far, all conditions are met
            return true;
        }
    }
}