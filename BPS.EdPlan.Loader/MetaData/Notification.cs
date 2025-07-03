using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using BPS.EdPlanLoaderCore.Models;

namespace BPS.EdPlanLoaderCore.MetaData
{
    class Notification
    {
        private readonly string _recipient;
        private readonly string _subject;
        private readonly string _body;
        private readonly string _attachmentFilename;

        public Notification(string recipient, string subject, string body, string attachmentFilename)
        {
            _recipient = recipient;
            _subject = subject;
            _body = body;
            _attachmentFilename = attachmentFilename;
        }

        // <summary>
        /// Sends an email with the specified recipient, subject, body, and optional attachment.
        /// </summary>
        /// <param name="recipient">The recipient's email address.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="body">The body content of the email.</param>
        /// <param name="attachmentFilename">The filename of the attachment (currently unused).</param>
        /// <returns>True if the email is sent successfully, false otherwise.</returns>
        public bool SendMail(string recipient, string subject, string body, string attachmentFilename)
        {
            try
            {
                // Create a new SendEmail object to hold email details
                SendEmail emailObj = new SendEmail();

                // Create a memory stream (not currently used for the attachment)
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    Attachment att = null;

                    // If the log file exists, create an attachment from it and add it to the email
                    if (File.Exists(Constants.LOG_FILE))
                    {
                        att = new Attachment(Constants.LOG_FILE);
                        emailObj.AttachmentList = new List<Attachment> { att };
                    }

                    // Initialize the recipient list and add the recipient
                    emailObj.ToAddr = new System.Collections.ArrayList();
                    emailObj.ToAddr.Add(recipient);
                    emailObj.FromAddr = Constants.EmailFromAddress;
                    emailObj.EmailSubject = subject;
                    emailObj.EmailContent = body;

                    // Call a method to actually send the email
                    SendEmailNotification(emailObj);
                }
                return true;
            }

            catch (Exception ex)
            {
                string message = $"Exception while sending email " + ex;
                //new EmailException(message, ex);
                return false;
            }

        }


        /// <summary>
        /// Send & Save email notification - general 
        /// </summary>
        /// <param name="emailObj"></param>
        /// <returns></returns>
        private bool SendEmailNotification(SendEmail emailObj)
        {

            MailMessage message = new MailMessage();
            message.From = new MailAddress(emailObj.FromAddr);
            foreach (string toString in emailObj.ToAddr)
            {
                message.To.Add(toString);
            }

            //send if there are attachments
            if (emailObj.AttachmentList != null && emailObj.AttachmentList.Count > 0)
            {
                foreach (Attachment att in emailObj.AttachmentList)
                {
                    message.Attachments.Add(att);
                }
            }

            if (!String.IsNullOrEmpty(emailObj.BccToAdr))
            {
                message.Bcc.Add(new MailAddress(emailObj.BccToAdr));
            }

            message.Subject = emailObj.EmailSubject;
            message.IsBodyHtml = true;
            AlternateView av = AlternateView.CreateAlternateViewFromString(emailObj.EmailContent, null, MediaTypeNames.Text.Html);
            message.AlternateViews.Add(av);
            using (SmtpClient smtp = new SmtpClient())
            {
                smtp.Host = Constants.SmtpServerHost;
                smtp.Send(message);
                return true;
            }
        }
    }
}

