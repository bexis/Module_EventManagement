//using BExIS.Modules.Lui.UI.Helper;
using BExIS.Modules.EMM.UI.Helpers;
using System;
using Vaiona.Logging;
using Vaiona.Web.Mvc.Modularity;

namespace BExIS.Modules.EMM.UI
{
    public class EMMModule : ModuleBase
    {
        public EMMModule(): base("EMM")
        {

        }

        public override void Start()
        {
            base.Start();
            LoggerFactory.GetFileLogger().LogCustom("Start EMM");
        }

        public override void Install()
        {
            LoggerFactory.GetFileLogger().LogCustom("... start install of EMM ...");
            try
            {
                base.Install();
                EMMSeedDataGenerator eMMSeedDataGenerator = new EMMSeedDataGenerator();
                eMMSeedDataGenerator.GenerateSeedData();
            }
            catch (Exception e)
            {
                LoggerFactory.GetFileLogger().LogCustom(e.Message);
                LoggerFactory.GetFileLogger().LogCustom(e.StackTrace);
            }

            LoggerFactory.GetFileLogger().LogCustom("... end install of EMM ...");
        }
    }
}
