using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Syn
{
    class Program
    {
        static void Main()
        {
            var allSubjectsCache = new List<Subject>();
            while (true)
            {
                ////////////////////////////////////
                var libruslogin = "librusowylogin";
                var librushaslo = "librusowylogin";
                var smsapilogin = "smsapilogin";
                var smsapihaslo = "smsapilogin";
                

                ////////////////////////////////////
                var uri = @"https://synergia.librus.pl/";
                var loguri = uri + "loguj";
                var geturi = uri + "przegladaj_oceny/uczen";
                try
                {
                    var cookieContainer = new CookieContainer();
                    using (var handler = new HttpClientHandler {CookieContainer = cookieContainer})
                    using (var client = new HttpClient(handler) {BaseAddress = new Uri(loguri)})
                    {
                              var values = new Dictionary<string, string>
                        {
                            {"login", libruslogin},
                            {"passwd", librushaslo},
                            {"czy_js", "0"}
                        };
                       
                        var content = new FormUrlEncodedContent(values);
                        
                        cookieContainer.Add(new Uri(loguri), new Cookie("TestCookie", "1"));
                        
                        var result = client.PostAsync(loguri, content).Result;

                        Console.WriteLine(result.StatusCode);

                        result = client.GetAsync(geturi).Result;

                        var parsedstring = result.Content.ReadAsStringAsync().Result;

                        var allsubjects = HtmlParse(parsedstring);

                        if (allSubjectsCache.Count == 0) allSubjectsCache = allsubjects;

                        var itemsToSend = new List<Subject>();
                        
                            for (var i = 0; i < allsubjects.Count; i++)
                            {
                                var v1 = allSubjectsCache[i];
                                var v2 = allsubjects[i];
                                if (v2.Grades.Count > v1.Grades.Count)
                                {
                                    v2.Grades.RemoveRange(0, v1.Grades.Count);
                                    var gradeslist = v2.Grades;
                                    itemsToSend.Add(new Subject {Name = v1.Name, Grades = gradeslist});
                                }
                            }
                        Console.WriteLine(itemsToSend.Count);

                        var smsmessage = "";

                        foreach (var vr in itemsToSend)
                        {
                            smsmessage += vr.Name + ": ";
                            foreach (var ve in vr.Grades)
                            {
                                smsmessage += ve.Category + " " + ve.Points + "/" + ve.MaxPoints + " " + ve.Teacher +
                                              " " + ve.Category + " " + ve.Date;
                                if (vr.Grades.Count > 1)
                                {
                                    smsmessage += "|";
                                }
                            }
                        }


                        if (itemsToSend.Count > 0 )
                        {
                            SendSms(smsmessage, smsapihaslo, smsapilogin);
                        }

                        allSubjectsCache = allsubjects;

                        //File.WriteAllText(@"C:\Users\Public\WriteText.html", );

                        //Process.Start(@"C:\Users\Public\WriteText.html");
                    }


                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }

                catch (Exception e)
                {
                    
                    Console.WriteLine(e.Message);
                }
            }
            
        }

        private static void SendSms(string smsmessage, string smsapihaslo, string smsapilogin)
        {
            try
            {
                SMSApi.Api.Client client = new SMSApi.Api.Client(smsapilogin);
                client.SetPasswordHash(smsapihaslo);

                var smsApi = new SMSApi.Api.SMSFactory(client);

                var result =
                    smsApi.ActionSend()
                        .SetText(smsmessage)
                        .SetTo("501305836")
                        .SetSender("ECO") //Pole nadawcy lub typ wiadomość 'ECO', '2Way'
                        .Execute();

                Console.WriteLine("Send: " + result.Count);

                string[] ids = new string[result.Count];

                for (int i = 0, l = 0; i < result.List.Count; i++)
                {
                    if (!result.List[i].isError())
                    {
                        //Nie wystąpił błąd podczas wysyłki (numer|treść|parametry... prawidłowe)
                        if (!result.List[i].isFinal())
                        {
                            //Status nie jest koncowy, może ulec zmianie
                            ids[l] = result.List[i].ID;
                            l++;
                        }
                    }
                }

                Console.WriteLine("Get:");
                result =
                    smsApi.ActionGet()
                        .Ids(ids)
                        .Execute();

                foreach (var status in result.List)
                {
                    Console.WriteLine("ID: " + status.ID + " NUmber: " + status.Number + " Points:" + status.Points + " Status:" + status.Status + " IDx: " + status.IDx);
                }

                for (int i = 0; i < result.List.Count; i++)
                {
                    if (!result.List[i].isError())
                    {
                        var deleted =
                            smsApi.ActionDelete()
                                .Id(result.List[i].ID)
                                .Execute();
                        Console.WriteLine("Deleted: " + deleted.Count);
                    }
                }
            }
            catch (SMSApi.Api.ActionException e)
            {
                /**
                 * Błędy związane z akcją (z wyłączeniem błędów 101,102,103,105,110,1000,1001 i 8,666,999,201)
                 * http://www.smsapi.pl/sms-api/kody-bledow
                 */
                Console.WriteLine(e.Message);
            }
            catch (SMSApi.Api.ClientException e)
            {
                /**
                 * 101 Niepoprawne lub brak danych autoryzacji.
                 * 102 Nieprawidłowy login lub hasło
                 * 103 Brak punków dla tego użytkownika
                 * 105 Błędny adres IP
                 * 110 Usługa nie jest dostępna na danym koncie
                 * 1000 Akcja dostępna tylko dla użytkownika głównego
                 * 1001 Nieprawidłowa akcja
                 */
                Console.WriteLine(e.Message);
            }
            catch (SMSApi.Api.HostException e)
            {
                /* błąd po stronie servera lub problem z parsowaniem danych
                 * 
                 * 8 - Błąd w odwołaniu
                 * 666 - Wewnętrzny błąd systemu
                 * 999 - Wewnętrzny błąd systemu
                 * 201 - Wewnętrzny błąd systemu
                 * SMSApi.Api.HostException.E_JSON_DECODE - problem z parsowaniem danych
                 */
                Console.WriteLine(e.Message);
            }
            catch (SMSApi.Api.ProxyException e)
            {
                // błąd w komunikacji pomiedzy klientem a serverem
                Console.WriteLine(e.Message);
            }
        }

        private static List<Subject> HtmlParse(string parsedstring)
        {
            
            //var start = "<div class=\"container-background\">";
            //var start2 = "<h3";
            //var start3 = "<table class=\"decorated";
            //var start4 = "<tbody";
            //var end = "</form>";
            //var end2 = "</table>";
            //var end3 = "</tbody>";

            

            //ind = parsedstring.IndexOf(start2, StringComparison.CurrentCulture);
            //parsedstring = parsedstring.Substring(ind);

            //ind = parsedstring.IndexOf(start3, StringComparison.CurrentCulture);
            //parsedstring = parsedstring.Substring(ind);

            //ind = parsedstring.IndexOf(start4, StringComparison.CurrentCulture);
            //parsedstring = parsedstring.Substring(ind);

            //ind = parsedstring.IndexOf(end, StringComparison.CurrentCulture);
            //parsedstring = parsedstring.Substring(0, ind);

            //ind = parsedstring.LastIndexOf(end2, StringComparison.CurrentCulture);
            //parsedstring = parsedstring.Substring(0, ind);

            var st =
                " <img src=\"/images/tree_colapsed.png\" id=\"przedmioty_OP_all_node\"";
            var ind = parsedstring.IndexOf(st, StringComparison.CurrentCulture) + st.Length;
            parsedstring = parsedstring.Substring(ind);

            var end = "<!-- END : Szczegóły -->";

            ind = parsedstring.IndexOf(end, StringComparison.CurrentCulture);
            parsedstring = parsedstring.Substring(0, ind);

            var impstring = "<img src=\"/images/tree_colapsed.png\"";

            var allsubjects = new List<Subject>();

            while (parsedstring.IndexOf(impstring, StringComparison.CurrentCulture) != -1)
            {
                var x = parsedstring.IndexOf(impstring, StringComparison.CurrentCulture);
                parsedstring = parsedstring.Substring(x);
                x = parsedstring.IndexOf("<td >", StringComparison.CurrentCulture) + "<td >".Length;
                if (parsedstring.IndexOf("<td >", StringComparison.CurrentCulture)== -1) break;
                parsedstring = parsedstring.Substring(x);
                x = parsedstring.IndexOf("</td>", StringComparison.CurrentCulture);
                var subject = new Subject {Name = parsedstring.Substring(0, x), Grades = new List<Grade>()};
                parsedstring = parsedstring.Substring(x + "</td>".Length);

                x = parsedstring.IndexOf("<td class=\"\">", StringComparison.CurrentCulture);
                parsedstring = parsedstring.Substring(x);

                x = parsedstring.IndexOf("<td class=\"right\">", StringComparison.CurrentCulture);
                var oneSubcjetToParse = parsedstring.Substring(0, x);
                parsedstring = parsedstring.Substring(x);

                while (oneSubcjetToParse.IndexOf("<a title=\"Kategoria: ", StringComparison.CurrentCulture) != -1)
                {
                    
                    x = oneSubcjetToParse.IndexOf("<a title=\"Kategoria: ", StringComparison.CurrentCulture) + "<a title=\"Kategoria: ".Length;
                    oneSubcjetToParse = oneSubcjetToParse.Substring(x);
                    x = oneSubcjetToParse.IndexOf(" (0-", StringComparison.CurrentCulture);
                    var grade = new Grade {Category = oneSubcjetToParse.Substring(0, x)};
                    oneSubcjetToParse = oneSubcjetToParse.Substring(x + " (0-".Length);
                    x = oneSubcjetToParse.IndexOf( ")", StringComparison.CurrentCulture);
                    grade.MaxPoints = Convert.ToDouble(oneSubcjetToParse.Substring(0, x));
                    x = oneSubcjetToParse.IndexOf("Data: ", StringComparison.CurrentCulture) + "Data: ".Length;
                    oneSubcjetToParse = oneSubcjetToParse.Substring(x);
                    x = oneSubcjetToParse.IndexOf(")", StringComparison.CurrentCulture) + ")".Length;
                    grade.Date = oneSubcjetToParse.Substring(0, x);
                    x = oneSubcjetToParse.IndexOf("Dodał: ", StringComparison.CurrentCulture) + "Dodał: ".Length;
                    oneSubcjetToParse = oneSubcjetToParse.Substring(x);
                    x = oneSubcjetToParse.IndexOf("\"", StringComparison.CurrentCulture);
                    grade.Teacher = oneSubcjetToParse.Substring(0, x);
                    oneSubcjetToParse = oneSubcjetToParse.Substring(x);
                    x = oneSubcjetToParse.IndexOf(">", StringComparison.CurrentCulture) + ">".Length;
                    oneSubcjetToParse = oneSubcjetToParse.Substring(x);
                    x = oneSubcjetToParse.IndexOf("<", StringComparison.CurrentCulture);
                    var points = oneSubcjetToParse.Substring(0, x);
                    points = points.Replace(".", ",");
                    grade.Points = Convert.ToDouble(points);

                    subject.Grades.Add(grade);                   
                }
                allsubjects.Add(subject);
            }
            return allsubjects;
        }
    }

    class Subject
    {
        public string Name { get; set; }
        public List<Grade> Grades { get; set; }

    }
    class Grade
    {
        public string Category { get; set; }
        public double MaxPoints { get; set; }
        public string Date { get; set; }
        public string Teacher { get; set; }
        public double Points { get; set; }
        public string Comment { get; set; }
        public bool InBase { get; set; }
    }

}
