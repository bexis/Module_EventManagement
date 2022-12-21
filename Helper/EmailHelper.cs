using BExIS.Emm.Entities.Event;
using BExIS.Security.Entities.Subjects;
using BExIS.Security.Services.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
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

        public static void SendEmailNotification(string notificationType, string email, string ref_id, XmlDocument data, Event e, string url)
        {
            // todo: add not allowed / log in info to mail

            EmailStructure emailStructure = new EmailStructure();
            emailStructure = EmailHelper.ReadFile(e.EventLanguage);

            string first_name = data.GetElementsByTagName(emailStructure.lableFirstName)[0].InnerText;
            string last_name = data.GetElementsByTagName(emailStructure.lableLastname)[0].InnerText;
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
            XDocument xDocument = XDocument.Parse(data.OuterXml);
            foreach (XElement xe in XElement.Parse(xDocument.ToString()).Elements())
            {
                string displayNameRoot = "";
                if (xe.HasElements)
                {
                    displayNameRoot = char.ToUpper(xe.Name.ToString()[0]) + xe.Name.ToString().Substring(1);
                    displayNameRoot = Regex.Replace(displayNameRoot, "((?<=[a-z])[A-Z])", " $1");
                    details = details + "<br/><b>" + displayNameRoot + "</b><br/><br/>";

                    foreach (XElement x in xe.Elements())
                    {
                        foreach (XElement r in x.Elements())
                        {
                            string displayName = "";
                            displayName = char.ToUpper(r.Name.ToString()[0]) + r.Name.ToString().Substring(1);
                            displayName = Regex.Replace(displayName, "((?<=[a-z])[A-Z])", " $1");
                            details = details + "<b>" + displayName + "</b>: " + r.Descendants().First().Value + "<br/>";

                        }
                    }

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
                body += emailStructure.bodyHintToLink + url + "/emm/EventRegistration/EventRegistration/?ref_id=" + ref_id + "<br/><br/>";
            body += emailStructure.bodyClosing + "<br/>" +
                 emailStructure.bodyClosingName;

            var es = new EmailService();

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