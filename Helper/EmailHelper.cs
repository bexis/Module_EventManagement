using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Vaiona.Utils.Cfg;

namespace BExIS.Modules.EMM.UI.Helper
{
    public static class EmailHelper
    {
        public static  EmailStructure ReadFile(string language)
        {
            string filePath = Path.Combine(AppConfiguration.GetModuleWorkspacePath("EMM"),"LanguageFiles", language.ToLower() +".json");
            string text = System.IO.File.ReadAllText(filePath);
            EmailStructure emailStructure = Newtonsoft.Json.JsonConvert.DeserializeObject<EmailStructure>(text);
            
            return emailStructure;
        }
    }


    public class EmailStructure
    {
        public string lableFirstName { get; set; }
        public string lableLastname { get; set; }
        public string lableEmail { get; set; }
        public string succesfullyRegisteredSubject { get; set; }
        public string succesfullyRegisteredMessage { get; set; }
        public string waitingListSubject { get; set; }
        public string waitingListMessage { get; set; }
        public string updateSubject { get; set; }
        public string updateMessage { get; set; }
        public string bodyTitle { get; set; }
        public string bodyOpening { get; set; }
        public string bodyHintToLink { get; set; }
        public string bodyClosing { get; set; }
        public string bodyClosingName { get; set; }
        public string removeFromWaitingListSubject { get; set; }
        public string removeFromWaitingList1 { get; set; }
        public string deletedSubject { get; set; }
        public string deletedMessage { get; set; }


        public EmailStructure()
        {
        }
    }
}