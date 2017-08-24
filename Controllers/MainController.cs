using BExIS.Modules.Lui.UI.Models;
using System.Web.Mvc;
using Vaiona.Web.Mvc.Models;
using Vaiona.Web.Extensions;
using System.Collections.Generic;

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
        #endregion

        // GET: Main
        public ActionResult Index()
        {
            // set page title
            ViewBag.Title = PresentationModel.GetViewTitleForTenant(TITLE, this.Session.GetTenant());

            // show the view
            LUIQueryModel model = new LUIQueryModel();
            return View("Index", model);
        }

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
    }
}