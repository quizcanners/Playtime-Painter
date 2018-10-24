using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.Networking;

namespace SharedTools_Stuff {
    public static class ExternalCommunication {

        public static void SendEmail(string to) => Application.OpenURL("mailto:{0}".F(to));

        public static void SendEmail(string email, string subject, string body) =>
        Application.OpenURL("mailto:{0}?subject={1}&body={2}".F(email, subject.MyEscapeURL(), body.MyEscapeURL()));

        static string MyEscapeURL(this string url) => UnityWebRequest.EscapeURL(url).Replace("+", "%20");

        public static void OpenBrowser(string address) => Application.OpenURL(address);

    }
}
