using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Syn
{
    class Program
    {
        static void Main()
        {
            try
            {
                ////////////////////////////////////
                const string baseUri = @"https://m.synergia.librus.pl/";
                const string logUri = baseUri + "module/Common/action/Login";
                const string newsUri = baseUri + "module/Common/action/News";
                ////////////////////////////////////
                using (var client = new HttpClient{BaseAddress = new Uri(logUri)})
                {
                    ////////////////////////////////////
                    const string librusLogin = "LOGIN";
                    const string librusPassword = "PASSWORD";
                    //string smsapilogin = "smsapilogin";
                    //string smsapihaslo = "smsapilogin";
                    ////////////////////////////////////

                    var values = new Dictionary<string, string>
                    {
                        {"login", librusLogin},
                        {"passwd", librusPassword},
                        {"loginButton", "1"}
                    };

                    var content = new FormUrlEncodedContent(values);
                    var result = client.PostAsync(logUri, content).Result;
                    
                    Console.WriteLine(result.StatusCode + (result.StatusCode == HttpStatusCode.OK ? " Zalogowano": " Nie Zalogowano"));

                    while (true)
                    {
                        result = client.GetAsync(newsUri).Result;

                        Console.WriteLine(result.StatusCode + (result.StatusCode == HttpStatusCode.OK ? " Pobrano dane" : " Nie pobrano danych"));

                        var parsedString = result.Content.ReadAsStringAsync().Result;

                        //parsedString = File.ReadAllText(@"C:\Users\Public\ReadText.html");//for debug

                        var news = HtmlParse(parsedString);

                        if (news.Grades?.Count > 0)
                        {
                            var messageToSend = FormMessage(news);
                            Console.WriteLine(messageToSend);
                            //File.WriteAllText(@"C:\Users\Public\WriteText.html", messageToSend);
                            //for debug
                            //Process.Start(@"C:\Users\Public\WriteText.html");
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static string FormMessage(News news)
        {
            var msg = news.Grades.Aggregate("", (current, vr) => current + vr.Subject + " " + vr.Points + "\n");
            msg += news.Announcements > 0 ? "Ogłoszenia: " + news.Announcements + "\n" : "";
            msg += news.Events > 0 ? "Zdarzenia w terminarzu: " + news.Events + "\n" : "";
            msg += news.Messages > 0 ? "Wiadomości: " + news.Messages : "";
            return msg ;
        }

        private static News HtmlParse(string prsstr)
        {
            var retObj = new News();
            if (prsstr.Contains("<ul data-role=\"listview\" data-theme=\"a\" class=\"ui-listview\">"))
            {
                var gradesListView = CutString("<ul data-role=\"listview\" data-theme=\"a\" class=\"ui-listview\">", "</ul>", prsstr);
                retObj.Grades = IsolateGradesList(gradesListView);
            }
            retObj.Messages = GetCount("Liczba wiadomości: ", prsstr);
            retObj.Events = GetCount("Liczba zdarzeń w terminarzu: ", prsstr);
            retObj.Announcements = GetCount("Liczba ogłoszeń: ", prsstr);
            return retObj;
        }

        private static List<Grade> IsolateGradesList(string gradesLsView)
        {
            var ls = new List<Grade>();
            foreach (var vr in MultipleCutString("<div class=\"ui-grid-a\">", "</li>",gradesLsView))
            {
                var ls2 = MultipleCutString("\">", "</div>", vr);
                for (var index = 0; index < ls2.Count; index++)
                {
                    var vr2 = ls2[index];
                    if (vr2 == "") ls2.Remove(vr2);
                }
                ls.Add(new Grade
                {
                    Subject = ls2.FirstOrDefault(),
                    Points = Convert.ToDouble(ls2.LastOrDefault()?.Replace(".", ","))
                });
            }
            return ls;
        }

        private static List<string> MultipleCutString(string stra, string strb, string prsstr)
        {
            var ls = new List<string>();
            do
            {
                ls.Add(CutString(stra, strb, prsstr));
                prsstr = CutString(strb, "", prsstr);
            } while (prsstr.IndexOf(strb, StringComparison.CurrentCulture) != -1);
            return ls;
        }
        private static string CutString(string stra, string strb, string prsstr)
        {
            var i = prsstr.IndexOf(stra, StringComparison.CurrentCulture);
            var sbstr = prsstr.Substring(i + stra.Length);
            if (strb == "") return sbstr;
            i = sbstr.IndexOf(strb, StringComparison.CurrentCulture);
            return sbstr.Substring(0, i).Trim();
        }

        private static int GetCount(string query, string prsstr)
        {
            var i = prsstr.IndexOf(query, StringComparison.CurrentCulture);
            var sbstr = prsstr.Substring(i + query.Length);
            i = sbstr.IndexOf("<", StringComparison.CurrentCulture);
            return Convert.ToInt32(sbstr.Substring(0, i));
        }
    }

    class News
    {
        public List<Grade> Grades { get; set; }
/*
        public int Absences { get; set; }
*/
        public int Messages { get; set; }
        public int Events { get; set; }
        public int Announcements { get; set; }

    }
    class Grade
    {
        public string Subject { get; set; }
        public double Points { get; set; }
    }

}
