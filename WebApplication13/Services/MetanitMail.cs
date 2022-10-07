using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Mail;

namespace FactPortal.Services
{
    public static class SimpleMail
    {
        public struct Sender
        {
            public string Email { get;}
            public string Password { get;}
            public string SMTP { get;}
            public int Port { get; }
            public bool SSL { get; }
            public Sender(string _email, string _password, string _smtp, int _port, bool _ssl)
            {
                Email = _email;
                Password = _password;
                SMTP = _smtp;
                Port = _port;
                SSL = _ssl;
            }
        }
        private static Sender Sender2 = new Sender("aspsender@mail.ru", "fa0F9eAxw7FvpwnM5RxB", "smtp.mail.ru", 587, true);
        private static Sender Sender1 = new Sender("noreply@xn--80aegjjroe.xn--p1ai", "Dima556368", "smtp.spaceweb.ru", 2525, false);

        // aspsender@mail.ru // noreply@мойзавод.рф // konstantin.belomoin@aseng.ru
        // fa0F9eAxw7FvpwnM5RxB // Dima556368
        // smtp.mail.ru // smtp.spaceweb.ru
        // 587 // 25

        public static void Send(string Title, string Message, string val_TO, string SenderName= "Сервис МойЗавод")
        {
            // отправитель - устанавливаем адрес и отображаемое в письме имя
            MailAddress from = new MailAddress(Sender1.Email, SenderName);
            // кому отправляем
            MailAddress to = new MailAddress(val_TO);
            // создаем объект сообщения
            MailMessage m = new MailMessage(from, to);
            // тема письма
            m.Subject = Title;
            // текст письма
            m.Body = Message;
            // письмо представляет код html
            m.IsBodyHtml = true;
            // адрес smtp-сервера и порт, с которого будем отправлять письмо
            SmtpClient smtp = new SmtpClient(Sender1.SMTP, Sender1.Port); 
            // логин и пароль
            smtp.Credentials = new NetworkCredential(Sender1.Email, Sender1.Password);
            smtp.EnableSsl = Sender1.SSL;
            smtp.Timeout = 60000; // new
            smtp.Send(m);
            smtp.Dispose(); // new
            m.Dispose(); // new
        }

        public static async Task SendAsync(string Title, string Message, string val_TO, string SenderName = "Сервис МойЗавод")
        {
                // отправитель - устанавливаем адрес и отображаемое в письме имя
                MailAddress from = new MailAddress(Sender1.Email, SenderName);
                // кому отправляем
                MailAddress to = new MailAddress(val_TO);
                // создаем объект сообщения
                MailMessage m = new MailMessage(from, to);
                // тема письма
                m.Subject = Title;
                // текст письма
                m.Body = Message;
                // письмо представляет код html
                m.IsBodyHtml = true;
                // адрес smtp-сервера и порт, с которого будем отправлять письмо
                SmtpClient smtp = new SmtpClient(Sender1.SMTP, Sender1.Port);
                // логин и пароль
                smtp.Credentials = new NetworkCredential(Sender1.Email, Sender1.Password);
                smtp.EnableSsl = Sender1.SSL;
                //smtp.UseDefaultCredentials = true; // new
                //smtp.DeliveryMethod = SmtpDeliveryMethod.Network; // new
                smtp.Timeout = 60000; // new
                await smtp.SendMailAsync(m);
                smtp.Dispose(); // new
                m.Dispose(); // new
            

        }

        // Шаблон письма для подтверждения почты
        public static string ConfirmEmail(string Email, string Link)
        {
            string Message = $@"<h3>Привет {Email},</h3>
                <div style='max - width:600px' align='justify'>
                <div>
                    Вы или вам зарегистрировали учетную запись на портале МойЗавод, прежде чем вы сможете использовать свою учетную запись,
                    вам необходимо подтвердить свой адрес электронной почты, для этого
                </div>
                <div align = 'center'>
                    <h4><a href = '{Link}'> нажмите здесь </a></h4>
                </div >
                <div style = 'padding-top:10px'> Если Вы не регистрировали учетную запись или она вам не нужна, то удалите это письмо.</div>
                <div style = 'padding-top:10px'> С уважением, команда <a href = 'http://176.67.48.57/MySite'> МойЗавод </a></div>
                <div style = 'padding-top:10px;font-size:80%'> Это письмо выслано автоматически и на него не следует отвечать.</div >
                </div>";
            return Message;
        }

        // Шаблон письма для восстановления пароля
        public static string ForgotEmail(string Email, string Link)
        {
            string Message = $@"<h3>Привет {Email},</h3>
                <div style='max - width:600px' align='justify'>
                <div>
                    Проблемы со входом на портал МойЗавод? Нажмите ссылку ниже и следуйте инструкциям.
                </div>
                <div align = 'center'>
                    <h4><a href = '{Link}'> Восстановить пароль </a></h4>
                </div >
                <div style = 'padding-top:10px'> Если вы не отправляли запрос на восстановление пароля, то удалите это письмо.</div>
                <div style = 'padding-top:10px'> С уважением, команда <a href = 'http://176.67.48.57/MySite'> МойЗавод </a></div>
                <div style = 'padding-top:10px;font-size:80%'> Это письмо выслано автоматически и на него не следует отвечать.</div >
                </div>";
            return Message;
        }
    }
}
