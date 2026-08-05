using BExIS.Emm.Entities.Event;
using BExIS.Modules.EMM.UI.Models;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;
using Entry = BExIS.Modules.EMM.UI.Models.Entry;

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

        public static void SendEmailNotification(string notificationType, string email, string ref_id, string data, Event e, string url)
        {
            // todo: add not allowed / log in info to mail

            EmailStructure emailStructure = new EmailStructure();
            emailStructure = EmailHelper.ReadFile(e.EventLanguage);

            string first_name = "";
            string last_name = "";
            string mail_message = "";
            string subject = "";

            switch (notificationType)
            {
                case "succesfully_registered":
                    subject = emailStructure.succesfullyRegisteredSubject + e.Name;
                    mail_message = emailStructure.succesfullyRegisteredMessage + e.Name + ".<br/>";
                    break;
                case "succesfully_registered_waiting_list":
                    subject = emailStructure.waitingListSubject + e.Name;
                    mail_message = emailStructure.waitingListMessage + e.Name + ".<br/>";
                    break;
                case "remove_from_waiting_list":
                    subject = emailStructure.removeFromWaitingListSubject + e.Name;
                    mail_message = emailStructure.removeFromWaitingList1 + "<br/><br/>";
                    break;
                case "updated":
                    subject = emailStructure.updateSubject + e.Name;
                    mail_message = emailStructure.updateMessage + e.Name + ".<br/>";
                    break;
                case "deleted":
                    subject = emailStructure.deletedSubject + e.Name;
                    mail_message = emailStructure.deletedMessage + e.Name + ".<br/>";
                    break;
                case "resend":
                    subject = "Resend of registration confirmation for " + e.Name;
                    mail_message = "your registration for " + e.Name + "<br/>";
                    break;
            }

            string details = "";
            //read xml file and format email output
            JsonEventModel model = JsonConvert.DeserializeObject<JsonEventModel>(data);
            var entries = model.Registration.SelectMany(r => r.Entries);
            last_name = entries.FirstOrDefault(a => a.Title == emailStructure.lableLastname)?.Value;
            first_name = entries.FirstOrDefault(a => a.Title == emailStructure.lableFirstName)?.Value;

            string displayNameRoot = "";

            foreach (var section in model.Registration)
            {
                displayNameRoot = section.Title;
                details = details + "<br/><b>" + displayNameRoot + "</b><br/><br/>";
                foreach (Entry entry in section.Entries)
                {
                    details = details + "<b>" + entry.Title + "</b>: " + entry.Value + "<br/>";
                }
            }

                string body = emailStructure.bodyTitle + first_name + " " + last_name + ", " + "<br/><br/>" +

                 mail_message + "<br/>";

                if (!String.IsNullOrEmpty(e.MailInformation))
                {
                    body += e.MailInformation + "<br/>" +
                    "<br/>";
                }
            
            body += emailStructure.bodyOpening + "<br/>" +
            details + "<br/><br/>";
            if (notificationType != "deleted")
                body += emailStructure.bodyHintToLink + "<a href=\"" + url + "/emm/eventregistration/edit?id=" + e.Id + "&ref_id=" + ref_id + "\" >" + url + "/emm/eventregistration/edit?id=" + e.Id + "&ref_id=" + ref_id + "</a><br/><br/>";
            body += emailStructure.bodyClosing + "<br/>" +
                 emailStructure.bodyClosingName;

            using (var es = new EmailService())
            {
                List<string> ccMails = new List<string>();
                if (!String.IsNullOrEmpty(e.EmailCC))
                    ccMails.AddRange(e.EmailCC.Split(',').ToList());


                List<string> bccMails = new List<string>();
                bccMails.Add(ConfigurationManager.AppSettings["SystemEmail"]);
                if (!String.IsNullOrEmpty(e.EmailBCC))
                    bccMails.AddRange(e.EmailBCC.Split(',').ToList());

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
                    ccMails, // CC 
                    bccMails, // Allways send BCC to SystemEmail + additional set 
                    new List<string> { replyTo }
                    );
            }
        }

        public static string GetRefIdFromEmail(string email)
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