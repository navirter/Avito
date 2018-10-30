using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Web;
using System.Threading;
using My;


namespace Avito
{
    public class Pagedownloader
    {
        public Pagedownloader.WebBrowser webBrowser = new WebBrowser();
        public Pagedownloader.WebClient webClient = new WebClient();
        public class WebBrowser
        {
            public HtmlDocument htmlDocument = null;
            public string Uri = "";
            long downloaded_scripts = 0;
            System.Timers.Timer timer = new System.Timers.Timer();
            bool timed = false;
            long interval = 10000; long target_scripts = 15; // = 7 sec

            public void downloadPage(string URL)
            {
                timer.Interval = interval;
                timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                timer.AutoReset = true;
                Uri = URL;
                System.Windows.Forms.WebBrowser wb = new System.Windows.Forms.WebBrowser();
                wb.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(wb_dc);
                wb.ScriptErrorsSuppressed = true;
                wb.Navigate(Uri);
                downloaded_scripts = 0;
                while (!timed && downloaded_scripts < target_scripts) { Application.DoEvents(); Thread.Sleep(50); }
                try
                {
                    htmlDocument = wb.Document;
                }
                catch
                { MessageBox.Show("error downloadViaWB " + URL); }
                try
                {
                    Uri = wb.Url.AbsoluteUri;
                }
                catch 
                { }
                try
                {
                    wb.Dispose();
                    timer.Dispose();
                    Thread.Sleep(2500);
                }
                catch 
                {

                }
            }
            private void wb_dc(object sender, EventArgs e)
            {
                downloaded_scripts++;
                if (interval > 2000)
                    interval -= 1000;
                timer.Interval = interval;
                timer.Start();
            }
            private void timer_Elapsed(object sender, EventArgs e)
            {
                timed = true;
            }
            public long tries = 0;
        }
        public class WebClient
        {
            public string htmlstring = "";
            private long tries = 0;
            public string Uri = "";
            public void downloadPage(string uri, Encoding enc, WebProxy proxyOrNull)
            {
                try
                {
                    var wc = new System.Net.WebClient();
                    if (proxyOrNull != null)
                        wc.Proxy = proxyOrNull;                   
                    wc.Encoding = enc;
                    htmlstring = wc.DownloadString(uri);
                    if (htmlstring.Contains("avito") && htmlstring.Contains("Доступ с Вашего IP временно ограничен"))
                    {
                        string messageX = "Пожалуйста, откройте браузер в режиме инкогнито, зайдите на авито и введите капчу.\n"+
                            "И перезапустите программу\n"+
                            "Не забудте также сказать создаетелю программы, что интервал между загрузками страниц слишком короткий.";
                        ThreadSeeker.addThread("забанило IP. Нужно зайти на сайт с него анонимно и ввести капчу", "avito", true, false, false);
                        string path = Directory.GetCurrentDirectory() + "\\temp";
                        Directory.CreateDirectory(path);
                        path += "\\lookAtLog.txt";
                        File.Create(path);
                        MessageBox.Show(messageX);
                    }
                    Uri = uri;
                    wc.Dispose();
                }
                catch 
                {
                    //Удаленный сервер возвратил ошибку: (403) Запрещено.
                    tries++;
                    string on = "off";
                    try
                    {
                        if (proxyOrNull.Address.AbsolutePath != "")
                            on = "on";
                    }
                    catch { }
                    ThreadSeeker.addThread("Не удалось скачать страницу " + uri + " Proxy " + on, "PageDownloader", true, false, false);
                }
                Thread.Sleep(2000);
            }
            static public System.Drawing.Bitmap downloadPicture(string uri)
            {
                Bitmap b = new Bitmap(10, 10);
                try
                {
                    System.Net.WebClient wc = new System.Net.WebClient();
                    byte[] bytes = wc.DownloadData(uri);
                    using (var ms = new MemoryStream(bytes))
                    {
                        b = new Bitmap(ms);
                    }
                }
                catch
                {

                }
                Thread.Sleep(2000);
                return b;
            }                        
        }
    }
    public static class FinderInfoInPage
    {
        public static string[] get_strings(string sourceString, string[] bonds, string contains)
        {
            string[] strings = sourceString.Split(bonds, StringSplitOptions.RemoveEmptyEntries);
            List<string> weContain = new List<string>();
            for (int i = 0; i < strings.Length; i++)
            {                
                if (strings[i].Contains(contains))
                    weContain.Add(strings[i]);
            }
            try { return weContain.ToArray(); }
            catch { return null; }
        }
        //public static HtmlElement get_class(HtmlDocument hd, string tagName, string Class, string pattern)
        //{
        //    HtmlElement Kid = null;
        //    HtmlElementCollection kids = hd.Body.Children;
        //    hd = null;
        //    for (int kid = 0; kid < kids.Count; kid++)
        //    {
        //        Thread.Sleep(10);
        //        try
        //        {
        //            if (kids[kid].OuterHtml.Contains(Class))
        //            {
        //                Kid = kids[kid];
        //                break;
        //            }
        //        }
        //        catch { }
        //    }
        //    kids = null;
        //    List<HtmlElement> contains = new List<HtmlElement>();
        //    Thread.Sleep(20);
        //    HtmlElementCollection tns = Kid.GetElementsByTagName(tagName);
        //    while (tns == null) { Application.DoEvents(); Thread.Sleep(100); }
        //    if (tns.Count == 0) throw new Exception();
        //    for (HtmlElement tn in tns)
        //    {
        //        try
        //        {
        //            if (tn.GetAttribute("class") != Class)
        //                continue;
        //            if (tn.OuterHtml.Contains(pattern))
        //                contains.Add(tn);
        //        }
        //        catch { }
        //    }
        //    return contains[0];
        //}
        public static List<HtmlElement> get_classes(HtmlElement he, string tagName, string Class)
        {
            try
            {
                HtmlElementCollection Kids = he.GetElementsByTagName(tagName);
                while (Kids == null) { Application.DoEvents(); Thread.Sleep(100); }
                he = null;
                List<HtmlElement> contains = new List<HtmlElement>();
                Random r = new Random();
                for (int kid = 0; kid < Kids.Count; kid++)
                {
                    Thread.Sleep(20);           
                    if (Kids[kid].OuterHtml.Contains("class=\""+Class+"\""))
                    {
                        contains.Add(Kids[kid]);
                    }
                }
                Kids = null;
                List<HtmlElement> res = contains;
                contains = new List<HtmlElement>();
                for (int i = 1; i < res.Count; i++)
                {                               
                    for (int j = 0; j < i; j++)
                    {                        
                        try
                        {
                            if (res[j].OuterHtml.Contains(res[i].OuterHtml))
                            {
                                res[j] = null;
                            }
                        }
                        catch
                        {

                        }
                    }
                }
                for (int i = 0; i < res.Count; i++)
                {
                    if (res[i] != null)
                        contains.Add(res[i]);
                }                
                return contains;
            }
            catch
            {
                return null;
            }
        }
    }


    public class ProxyInfo
    {
        public System.Net.WebProxy proxy;//ip and port 
        public string country;
        public string city;
        public long delay;
        public string type;
        public string anonymity;
        public DateTime lastUpdate;
        /*Нет: Удалённый сервер знает ваш IP, и знает, что вы используете прокси.
Низкая: Удалённый сервер не знает ваш IP, но знает, что вы используете прокси.
Средняя: Удалённый сервер знает, что вы используете прокси, и думает, что знает ваш IP, но он не ваш.
Высокая: Удалённый сервер не знает ваш IP, и у него нет прямых доказательств, что вы используете прокси (заголовков из семейства прокси-информации).*/
        public static ProxyInfo[] getSiteHideMe()
        {
            Pagedownloader pd = new Pagedownloader();
            string uri = "http://hideme.ru/proxy-list/?country=RU&type=s45&anon=234#list";
            pd.webBrowser.downloadPage(uri);
            string body = pd.webBrowser.htmlDocument.Body.InnerHtml.ToLower();
            var tables = FinderInfoInPage.get_strings(body, new string[] { "tr>", "/tr>" }, "russian federation");
            List<ProxyInfo> res = new List<ProxyInfo>();
            for (int i = 1; i < tables.Length; i++)
                try
                {
                    string modificated = tables[i].Replace("div", "").Replace("span", "").Replace("tr", "").Replace("class=", "").Replace("tdr", "");
                    modificated = new string(modificated.ToCharArray().ToList().FindAll(s => Char.IsLetter(s) || s ==' ' || s=='.' || Char.IsDigit(s)).ToArray()).Trim();
                    int ind = 3; ind = modificated.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                    modificated = modificated.Substring(ind);
                    string[] lines = modificated.Split(new string[] { "td" }, StringSplitOptions.RemoveEmptyEntries);
                    string ip = lines[0].Replace("l","").Replace("|","").Trim();
                    string port = lines[1].Trim();
                    string country = "russia";
                    string[] cities = lines[2].Split(new string[] { "russian federation" }, StringSplitOptions.RemoveEmptyEntries);
                    string city = ""; if (cities.Length == 2) city = cities[1].Trim();
                    string[] speeds = lines[3].Split(new string[] { "pxp", "мс" }, StringSplitOptions.RemoveEmptyEntries);
                    string speed = speeds[1].Trim();
                    string type = lines[4].Trim();
                    string anonimity = lines[5].Trim();
                    string lastUpdate = lines[6].Trim();
                    ProxyInfo pi = new ProxyInfo();
                    WebProxy proxy = new WebProxy(ip, int.Parse(port));
                    pi.proxy = proxy;
                    pi.country = country;
                    pi.city = city;
                    pi.delay = long.Parse(speed);
                    pi.type = type;
                    pi.anonymity = anonimity;
                    string lus = new string(lastUpdate.ToCharArray().ToList().FindAll(s => Char.IsDigit(s) || s == ' ').ToArray()).Trim();
                    string[] hourAndMinute = lus.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    string hour = "0"; string minute = "0";
                    if (hourAndMinute.Length == 2)
                    {
                        hour = hourAndMinute[0];
                        minute = hourAndMinute[1];
                    }
                    else
                        minute = hourAndMinute[0];
                    TimeSpan ago = new TimeSpan(0, int.Parse(hour), int.Parse(minute), 0);
                    DateTime check = DateTime.Now.Add(-ago);
                    pi.lastUpdate = check;
                    if(pi.delay<3000)
                        res.Add(pi);
                }
                catch
                { }
            return res.ToArray();
        }
    }
}
