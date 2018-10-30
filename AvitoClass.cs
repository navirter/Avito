using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Windows.Media;
using System.Media;
using My;
using System.Xml.Serialization;

namespace Avito
{
    public class AvitoClass
    {
        static char[] cyrrilian = {
            'а', 'б', 'в', 'г', 'д', 'е', 'ё', 'ж', 'з', 'и', 'й', 'к', 'л', 'м', 'н', 'о',
            'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ь', 'ы', 'ъ', 'э', 'ю', 'я'
            ,'А','Б','В','Г','Д','Е','Ё','Ж','З','И','Й','К','Л','М','Н','О','П','Р','С','Т'
            ,'У','Ф','Х','Ц','Ч','Ш','Щ','Ь','Ы','Ъ','Э','Ю','Я'};
        public static bool isCirrilyc(char ch)
        {            
            foreach (char c in cyrrilian)
                if (c == ch)
                    return true;
            return false;
        }

        public const int minuteTimer = 60;     
        public List<Pattern> patterns = new List<Pattern>();
        public Pattern selectedPattern = null;    
        public bool searchExecuting = false;
        public long iteration = 1;
        public static Ad lastAd = null;

        public static bool sleep(int seconds, bool mainThread)
        {            
            string curact = ThreadSeeker.currentActivity;
            for (int i = 0; i < seconds; i++)
            {
                if (Form2.closed) return true;
                Thread.Sleep(1000);
                int remain = seconds - i;
                if (mainThread)
                    ThreadSeeker.currentActivity = curact + ".sleeping(" + remain + ")";
            }
            if (mainThread)
                ThreadSeeker.currentActivity = curact;
            while (ThreadSeeker.pause) Thread.Sleep(1000);
            return false;
        }
        public void main(object sender)
        {
            Form2.Sender source = sender as Form2.Sender;
            try { lastAd = Ad.WritingAndReading.ReadFromXmlFile<Ad>(Directory.GetCurrentDirectory() + "\\settings\\exampleChecked.txt"); } catch { }
            while (true)
                try
                {
                    #region read settings and read patterns and ads and criterias
                    ThreadSeeker.addThread("Получаем ресурсы", "avito", false, false, true);
                    SearchSettings.UnitList unipatterns = new SearchSettings.UnitList();
                    unipatterns.ReadInFile(); patterns.Clear();
                    foreach (var v in unipatterns) patterns.Add(new Pattern(v.pattern, v, patterns.Count));
                    #endregion
                    #region get averages and numbers for patterns
                    foreach (var pattern in patterns)
                    {
                        if (sleep(1, true)) return;
                        #region nullify amounts
                        pattern.totalAds = 0;
                        pattern.totalCoolExistingAds = 0;
                        pattern.totalDissapearedAds = 0;
                        pattern.totalNotCoolExistingAds = 0;
                        #endregion
                        pattern.dataIsComplete = false;
                        pattern.recalculateAverageData();
                        foreach (var category in pattern.categories)
                        {
                            //category.findCoolAdsOfExisting(ref category.existingAds, pattern.averageData);
                            pattern.totalNotCoolExistingAds += category.existingAds.Count;
                            pattern.totalDissapearedAds += category.dissapearedAds.Count;
                            pattern.totalCoolExistingAds += category.coolExistingAds.Count;
                            pattern.totalAds += category.existingAds.Count + category.dissapearedAds.Count + category.coolExistingAds.Count;
                        }
                        pattern.dataIsComplete = true;
                        string mes = "Всего=" + pattern.totalAds + ", существующих=" + pattern.totalCoolExistingAds + "+" +
                            pattern.totalNotCoolExistingAds + ", исчезнувших=" + pattern.totalDissapearedAds;
                        ThreadSeeker.addThread(mes, pattern.name + ".getting statistics", false, false, false);
                        ThreadSeeker.currentActivity = "";
                    }
                    #endregion
                    //ThreadSeeker.addThread("Ресурсы получены", "avito", false, false, true);
                    if (source.withoutInternet) break;
                    if (sleep(20 * 60, true)) return;
                    #region search
                    DateTime start = DateTime.Now;
                    if (!source.goFullSearch && !rightTime(start.Hour))
                    {
                        foreach (var v in patterns) v.main(false, source.lb, ref lastAd);
                    }
                    else
                    {
                        start = DateTime.Now;
                        foreach (var v in patterns) v.main(true, source.lb, ref lastAd);
                        source.goFullSearch = false;
                        playMusic(source);
                        tryRenewTiming(start, ref source);
                        ThreadSeeker.addThread("Отдых", "avito", false, false, true);
                        if (sleep(60 * 60 * 3, true)) return;
                    }
                    iteration++;
                    #endregion
                }
                catch (Exception e)
                {
                    ThreadSeeker.addThread(e.Message, "avito", true, false, false);
                }
        }
        bool rightTime(long nowHour)
        {
            if (nowHour > 4 && nowHour != 24)
            {
                long howMuchWait = 24 - nowHour;                
                ThreadSeeker.addThread("Основной поиск начнется через " + howMuchWait.ToString() + " часов.", "avito", false, false, false);
                return false;
            }
            else
                return true;
        }
        void playMusic(Form2.Sender source)
        {
            try
            {
                source.mp = new MediaPlayer();
                string musPath = Directory.GetCurrentDirectory() + "\\settings\\sound.mp3";
                source.mp.Open(new Uri(musPath, UriKind.Relative));
                for (int i = 1; i < 2; i++)
                {
                    source.mp.Play();
                    Thread.Sleep(6000 / i);
                    source.mp.Stop();
                }
                source.mp.Close();
            }
            catch
            { }
        }
        void tryRenewTiming(DateTime start, ref Form2.Sender source)
        {
            try
            {
                DateTime now = DateTime.Now;
                TimeSpan difference = now - start;
                string time = Convert.ToInt32(difference.TotalDays).ToString() + " дней, " + difference.Hours.ToString() + " часов, " + difference.Minutes.ToString() + " минут.";
                string[] lines = new string[] { "Последний полный проход занял", time };
                source.OneRoundTime.Lines = lines;
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\settings");
                File.WriteAllLines(Directory.GetCurrentDirectory() + "\\settings\\timing.txt", lines);
            }
            catch 
            { }
        }        

        public class Pattern
        {
            SearchSettings.Unit unit;
            public string name = "";
            public int number = 0;
            [XmlIgnore]public List<Category> categories = new List<Category>();
            public static Ad patLastAd;
            //these datas lack categories and therefore can't be saved
            [XmlIgnore]public Criteria averageData;
            public int totalAds = 0;
            public int totalCoolExistingAds = 0;
            public int totalNotCoolExistingAds = 0;
            public int totalDissapearedAds = 0;
            public bool dataIsComplete = false;

            public Pattern(string Name, SearchSettings.Unit Unit, int Number)
            {
                dataIsComplete = false;
                name = Name;
                unit = Unit;
                number = Number;
                foreach (var v in unit.categories)
                {
                    categories.Add(new Category(v.value, v.link, this, categories.Count));
                }
                averageData = new Criteria(this, DateTime.Today);
                dataIsComplete = true;
            }
            public void main(bool fullSearch, ListBox lb, ref Ad lastAd)
            {                
                if (sleep(1, true)) return;
                DateTime start = DateTime.Now;
                averageData = new Criteria(this, DateTime.Today);
                long processedAds = 0;
                foreach (var v in categories)
                {
                    v.calculateDatasFromDissapearedAdsAndSave(fullSearch);
                    recalculateAverageData();
                    v.findCoolAdsOfExisting(ref v.readAds, averageData);
                    processedAds += v.downloadAds(fullSearch); if (sleep(3, true)) return;
                    v.separateAds(averageData); if (sleep(3, true)) return;
                    #region saving
                    v.saveAds(v.coolExistingAds);
                    v.saveAds(v.existingAds);
                    v.saveAds(v.dissapearedAds);
                    v.addThreadOfGettingLatestExAndDisAds();
                    saveLastAd(lastAd);
                    #endregion
                    v.calculateDatasFromDissapearedAdsAndSave(fullSearch); if (sleep(3, true)) return;
                }                
                recalculateAverageData(); if (sleep(3, true)) return;
                #region showTime
                DateTime finish = DateTime.Now;
                TimeSpan timeSpent = finish - start;
                string[] lbs = lb.Items[number].ToString().Split(new string[] { " Время: " }, StringSplitOptions.RemoveEmptyEntries);
                string one = lbs[0];
                string timesPent = Convert.ToInt32(timeSpent.TotalHours) + "ч." + timeSpent.Minutes + "м.";
                lb.Items[number] = one + " Время: " + timesPent;
                #endregion
                ThreadSeeker.addThread("Запрос обработан" + "("+processedAds+" объяв.)", "avito."+name, false, false, false);
            }
            public void recalculateAverageData()
            {
                #region getGenerals
                long generalRR = 0;
                long generalPrice = 0;
                long count = 0;
                for (int j = 0; j < categories.Count; j++)//foreach category
                    try
                    {
                        generalRR += categories[j].criteria.averageRealRating;
                        generalPrice += categories[j].criteria.averagePrice;
                        count++;
                    }
                    catch { }
                #endregion
                if (count == 0) return;
                long averageRR = generalRR / count;
                long averagePrice = generalPrice / count;
                int minutes = 0;
                if (averageRR != 0)
                    minutes = Convert.ToInt32(averagePrice * 1000 / averageRR);
                TimeSpan averageminimumLifeTime = new TimeSpan(0, minutes, 0);

                var curdata = new Criteria(this, DateTime.Today);
                curdata.averageminimumLifeTime = averageminimumLifeTime;
                curdata.averagePrice = averagePrice;
                curdata.averageRealRating = averageRR;
                averageData = curdata;
            }
            void saveLastAd(Ad lastad)
            {
                string path = Directory.GetCurrentDirectory() + "\\settings";
                Directory.CreateDirectory(path);
                string fp = path + "\\exampleChecked.txt";
                Ad.WritingAndReading.WriteToXmlFile(fp, lastad, false);
            }
            //finished
            public class Category
            {
                public string name;
                [XmlIgnore]public Pattern owner;
                public string link;
                public long number;
                //general data above
                public List<Ad> downloadedAds = new List<Ad>();
                public List<Ad> readAds = new List<Ad>();
                //two above transform to two below 
                public List<Ad> existingAds = new List<Ad>();
                public List<Ad> dissapearedAds = new List<Ad>();
                //existing + datasUR = cool
                [XmlIgnore]public Criteria criteria;
                public List<Ad> coolExistingAds = new List<Ad>();
                // all existing = existing + cool
                public Ad catLastAd = null;


                public Category(string Name, string Link, Pattern Owner, long Number)
                {
                    name = Name;
                    link = Link;
                    owner = Owner;
                    number = Number;
                    criteria = new Criteria(this, DateTime.Today);
                    //
                    criteria = readSavedData("");
                    readAdsAndDeleteOld();            
                }
                public void addThreadOfGettingLatestExAndDisAds()
                {
                    ThreadSeeker.addThread("Получено " + existingAds.Count + " сущ. и " + dissapearedAds.Count
                        + " пропавш. объявл.", "avito." + owner.name + "." + name, false, true, false);
                }
                #region downloadAds
                protected long crashedAds = 0;
                protected long processedAds = 0;
                protected long pagesAmount = 1;
                //
                protected long currentPage = 0;
                protected string currentLink = "";
                public long downloadAds(bool fullSearch)
                {
                    if (AvitoClass.sleep(1, true)) return 0;
                    #region Notification                    
                    if (fullSearch)                    
                        ThreadSeeker.addThread("Полный поиск " + owner.name + "." + name, "avito", false, false, true);                    
                    else                    
                        ThreadSeeker.addThread("Быстрый поиск " + owner.name + "." + name, "avito", false, false, true);                    
                    #endregion
                    try
                    {
                        downloadedAds.Clear();
                        string initialuri = link;
                        getPages(fullSearch, initialuri);
                        Random random = new Random();
                        int rand = random.Next(minuteTimer - 20, minuteTimer + 20);
                        if (sleep(rand, true)) return processedAds;
                        long pc = pagesAmount;
                        if (pc > 20) pc += 2;
                        for (int j = 1; j <= pc; j++)
                        {
                            string uri = initialuri.Replace("?", "?p=" + j.ToString() + "&");
                            currentPage = j;
                            currentLink = uri;
                            //category.host.currentCategory = numCat;
                            bool stopIfQuickSearch = false;
                            processPage(uri, ref stopIfQuickSearch, fullSearch);
                            if (sleep(1, true)) return processedAds;
                            if (!fullSearch && stopIfQuickSearch)
                                break;
                        }
                        if (fullSearch)
                            ThreadSeeker.addThread("Скачано " + processedAds + " объяв.", "avito." + owner.name + "." + name, false, false, true);
                        else
                            ThreadSeeker.addThread("Скачано " + processedAds + " объяв.", "avito." + owner.name + "." + name, false, true, true);
                        if (crashedAds > 0)
                            ThreadSeeker.addThread("Всего " + crashedAds.ToString() + " ошибок скачивания", "avito." + owner.name + "." + name, true, false, false);                        
                    }
                    catch
                    { }
                    ThreadSeeker.currentActivity = "";
                    return processedAds;
                }
                void getPages(bool fullSearch, string iniuri)
                {
                    ThreadSeeker.currentActivity = owner.name + "." + name + ".getting pages amount";
                    long res = 1;
                    string page = iniuri;
                    Pagedownloader pd = new Pagedownloader();
                    pd.webClient.downloadPage(page, Encoding.UTF8, null);
                    string[] bonds = { "p=", "</a>" };
                    try
                    {
                        string[] x = FinderInfoInPage.get_strings(pd.webClient.htmlstring, bonds, "Последняя");
                        pd = null;
                        string[] sep = { "&" };
                        string[] xs = x[0].Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        string xx = xs[0];
                        res = int.Parse(xx);
                    }
                    catch
                    {
                        if (fullSearch)
                            ThreadSeeker.addThread("неудалось скачать число страниц для " + page, "avito." + owner.name + "." + name, true, false, true);
                        else
                            ThreadSeeker.addThread("неудалось скачать число страниц для " + page, "avito." + owner.name + "." + name, true, true, true);
                    }
                    pd = null;
                    pagesAmount = res;
                }
                void processPage(string uri, ref bool stopIfQuickSearch, bool fullSearch)
                {
                    ThreadSeeker.currentActivity = owner.name + "." + name + ". downloading page " + currentPage.ToString() + "/" + pagesAmount.ToString();
                    Pagedownloader pd = new Pagedownloader();
                    //pd.download_via_wr(new WebProxy("192.168.0.1", 8080), new NetworkCredential("user","password"), uri);
                    pd.webClient.downloadPage(uri, Encoding.UTF8, null);
                    int s = new Random().Next(1, 10);//2+ secs waiting in ad processing
                    if (sleep(s, true)) return;
                    string[] adStrings = getAdsString(pd.webClient.htmlstring);
                    if (adStrings.Length == 0) return;
                    for (int i = 1; i < adStrings.Length; i++)
                    {
                        int count = adStrings.Length - 1;
                        ThreadSeeker.currentActivity = owner.name + "." + name + ". downloading page " + currentPage.ToString() + "/" + pagesAmount.ToString()
                            + ". ad " + i.ToString() + "/" + count.ToString();
                        processAd(adStrings[i].Substring(3), ref stopIfQuickSearch, fullSearch);
                    }
                    long humanCurCat = number + 1;
                    string res = "Обработана страница " + currentPage.ToString() + " из " + pagesAmount.ToString()
                        + "(" + humanCurCat.ToString() + "/" + owner.categories.Count.ToString() + ")";                                       
                        ThreadSeeker.addThread(res, "avito." + owner.name + "." + name, false, true, true);
                }
                string[] getAdsString(string source)
                {
                    try
                    {
                        string[] bonds = { "js-catalog_before-ads", "ads_direct_low" };
                        string[] itemItem = { "item item_" };
                        string[] adList = FinderInfoInPage.get_strings(source, bonds, itemItem[0]);
                        var res = adList[0].Split(itemItem, StringSplitOptions.RemoveEmptyEntries);
                        if (res.Length == 0)
                        { ThreadSeeker.addThread("нет объявлений по ссылке", "avito." + owner.name + "." + name, true, false, true); }
                        return res;
                    }
                    catch
                    {
                        return null;
                    }
                }
                void processAd(string source, ref bool stopIfQuickSearch, bool fullSearch)
                {
                    try
                    {
                        processedAds++;
                        Ad ad = GetAdInfo.execute(source, owner.name, this.name);
                        if (ad == null) return;
                        string[] texts = gettimeTexts(source);
                        if (texts.Length == 3) case3(texts, ref ad);
                        if (texts.Length == 4) case4(texts, ref ad);
                        if (ad.birth == new DateTime()) return;
                        ad = processTimesAndDates(ad, ref stopIfQuickSearch);
                        ad.downloadPhotoIfNotExist(source); if (AvitoClass.sleep(1, true)) return;
                        ad = ad.details.tryGetDescriptionAndRatingAndCheckings(ad);if (AvitoClass.sleep(1, true)) return;
                        ad.rating.estimateThings(ref ad, this);
                        downloadedAds.Add(ad);
                        catLastAd = ad;
                        patLastAd = ad;
                        lastAd = ad;
                    }
                    catch (Exception er)
                    {
                        My.ThreadSeeker.addThread(er.Message, "ad processing", false, true, false);
                    }
                }
                #region detailedAdProcessing
                static Ad processTimesAndDates(Ad ad, ref bool stopIfQuickSearch)
                {
                    if (ad.birth.Day != DateTime.Today.Day) stopIfQuickSearch = true;
                    else if (DateTime.Now.Hour - ad.birth.Hour > 5) stopIfQuickSearch = true;
                    if (ad.birth.Hour == 23 && DateTime.Now.Hour == 0) if (ad.birth.Day == DateTime.Today.Day)
                            ad.birth.AddDays(-1);
                    try
                    {
                        Ad last = Ad.WritingAndReading.ReadFromXmlFile<Ad>(Ad.WritingAndReading.getAdInfoPath(ad));
                        if (last != null)
                        {
                            ad.details.checkings = last.details.checkings;
                            //ad.primary.UserPhotoRating = last.primary.UserPhotoRating;
                        }
                    }
                    catch { }
                    ad.details.checkings.Add(DateTime.Now);
                    ad.details.renewminimumLifeTime(ad.birth);
                    return ad;
                }

                static class GetAdInfo
                {
                    static public Ad execute(string source, string pattern, string category)
                    {
                        Ad ad = new Ad();
                        try
                        {
                            if (source.Contains("VIP") || source.Contains("Премиум")) return null;
                            ad.primary.id = getId(source);
                            ad.primary.price = getPrice(source);
                            if (ad.primary.price == 0)
                                return null;
                            ad.primary.amountPhotos = getPhotos(source);
                            if (ad.primary.amountPhotos == 0) ad.primary.amountPhotos++;
                            ad.primary.name = getName(source);
                            ad.primary.place = getPlace(source);
                            ad.primary.pattern = pattern;
                            ad.primary.category = category;
                            ad.primary.source = "avito";
                            return ad;
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    static int getPrice(string s)
                    {
                        int res = 0;
                        try
                        {
                            string[] sep2 = { ">\n", "<" };
                            string[] prices = FinderInfoInPage.get_strings(s, sep2,
                                "руб.");
                            res = int.Parse(prices[0].Replace("руб.", "").Replace(" ", ""));
                        }
                        catch
                        {

                        }
                        return res;
                    }
                    static int getPhotos(string s)
                    {
                        int res = 0;
                        try
                        {
                            string[] sep2 = { "item-slider-list js-item-slider-list", "favorites-add is-design-simple js-favorites-add" };
                            string[] photos = FinderInfoInPage.get_strings(s, sep2,
                            "item-slider-item js-item-slider-item ");
                            string[] newPhotos = photos[0].Split(new[] { "item-slider-item js-item-slider-item" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var v in newPhotos) if (v.Contains("background-image")) res++;
                        }
                        catch
                        { }
                        return res;
                    } //unimportant at all
                    static string getName(string s)
                    {
                        try
                        {
                            string res = "";
                            string[] sep22 = { "title=", "</a" };
                            string[] hel = FinderInfoInPage.get_strings(s, sep22,
                            "в Москве");
                            res = hel[0].Split(new[] { ">" }, StringSplitOptions.RemoveEmptyEntries)[1];
                            res = res.Replace("\n", "").Trim();
                            return res;
                        }
                        catch
                        {
                            return null;

                        }
                    }
                    static string getId(string s)
                    {
                        try
                        {
                            string res = "";
                            string[] sep = { "id=\"i", "\" data" };
                            string[] flinks = FinderInfoInPage.get_strings(s, sep, "");
                            res = flinks[1].Replace("/favorites/add/", "");
                            return res;
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    static string getPlace(string s)
                    {
                        string place = "Москва";
                        string[] sep = { "м." };
                        string[] places = s.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        string place1 = places[1].Substring(2).TrimStart();
                        places = place1.Split(new string[] { "<div" }, StringSplitOptions.RemoveEmptyEntries);
                        char[] chars = places[0].ToLower().Trim().ToCharArray();
                        place1 = "";
                        foreach (char ch in chars)
                            if (AvitoClass.isCirrilyc(ch) || ch == ' ')
                                place1 += ch;
                        if (place1 != "")
                            place = place1;
                        if (place == "библиотека и")
                            place += "мени ленина";
                        return place;
                    }
                }

                static string[] gettimeTexts(string s)
                {
                    try
                    {
                        string[] sep = { "c-2\">", "</div>" };
                        string[] dt = FinderInfoInPage.get_strings(s, sep, "&nbsp;");
                        string t = "";
                        for (int i = dt.Length - 1; i >= 0; i--)
                        {
                            if (dt[i].Contains(":"))
                            {
                                t = dt[i].Replace("\n", "").Trim();
                                break;
                            }
                        }
                        dt = null;
                        string[] sep1 = { " ", ":", "&nbsp;" };
                        //Вчера 00:02
                        //29 февраля 23:57
                        return t.Split(sep1, StringSplitOptions.RemoveEmptyEntries);
                    }
                    catch
                    {
                    }
                    return null;
                }
                static void case3(string[] ss, ref Ad ad)
                {
                    try
                    {
                        DateTime dt = DateTime.Today;
                        int hour = int.Parse(ss[1]);
                        int minute = int.Parse(ss[2]);
                        dt = new DateTime(dt.Year, dt.Month, dt.Day, hour, minute, 0);
                        if (ss[0] == "Вчера")
                            dt.AddDays(-1);
                        ad.birth = dt;
                    }
                    catch
                    {
                        //format is changed
                    }
                }
                static void case4(string[] ss, ref Ad ad)
                {
                    try
                    {
                        string[] months = {"", "января","февраля","марта","апреля","мая","июня","июля","августа","сентября","октября",
                                                            "ноября","декабря"};
                        for (int jk = 1; jk < months.Length; jk++)
                        {
                            if (ss[1] == months[jk])
                            {
                                int day = int.Parse(ss[0]);
                                int month = jk;
                                int year = DateTime.Now.Year;
                                if (DateTime.Now.Month == 1 && jk >= 6)
                                    year--;
                                int hour = int.Parse(ss[2]);
                                int minute = int.Parse(ss[3]);
                                ad.birth = new DateTime(year, month, day, hour, minute, 0);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Format of Date is changed");
                        //format is changed
                    }
                }
                #endregion
                #endregion
                #region readAds
                public void readAdsAndDeleteOld()
                {
                    #region get to list of files ads
                    string sourcePath = Directory.GetCurrentDirectory() + "\\data\\avito\\" + owner.name + "\\"
                        + name + "\\ads";
                    readAds.Clear();
                    try
                    {
                        string[] years = Directory.GetDirectories(sourcePath);
                        for (int i = 0; i < years.Length; i++)
                        {
                            List<string> days = new List<string>();
                            string[] months = Directory.GetDirectories(years[i]);
                            foreach (var v in months) days.AddRange(Directory.GetDirectories(v));
                            List<string> files = new List<string>();
                            foreach (var day in days)
                            {
                                string[] ids = Directory.GetDirectories(day);
                                foreach (var id in ids) files.AddRange(Directory.GetFiles(id));
                            }
                            files = files.FindAll(s => s.Contains("info.txt"));
                            #endregion
                            for (int j = 0; j < files.Count; j++)
                                #region process each ad
                                try
                                {
                                    ThreadSeeker.currentActivity = owner.name + "." + name + ".reading ads(" + j + "/" + files.Count + ")";
                                    Ad ad = Ad.WritingAndReading.ReadFromXmlFile<Ad>(files[j]);
                                    if (ad == null || isOld(ad))
                                        delete(new FileInfo(files[j]).Directory.FullName);
                                    else
                                    {
                                        readAds.Add(ad);
                                        try { if(ad.birth>catLastAd.birth) catLastAd = ad; }
                                        catch { catLastAd = ad; }
                                        if (ad.details.existing)
                                        {
                                            if (ad.rating.betterThanCategoryEstimationsStrictly || ad.rating.betterThanCategoryEstimationsStrictlyNot ||
                                                ad.rating.betterThanPatternEstimationsStrictly || ad.rating.betterThanPatternEstimationsStrictlyNot)
                                                coolExistingAds.Add(ad);
                                            else
                                                existingAds.Add(ad);
                                        }
                                        else
                                            dissapearedAds.Add(ad);
                                    }
                                }
                                catch { }
                            #endregion
                    #region things
                        }
                        addThreadOfGettingLatestExAndDisAds();
                    }
                    catch
                    {
                        ThreadSeeker.addThread("Не получены последние объявления", "avito." + owner.name + "." + name, true, false, true);
                    }
                    #endregion
                }
                public bool isOld(Ad ad)
                {
                    var ts = DateTime.Now - ad.birth;
                    if (ts > new TimeSpan(65, 0, 0, 0))
                        return true;
                    return false;
                }
                void delete(string folder)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch { }
                }
                #endregion             
                #region separateAds                
                public void separateAds(AvitoClass.Criteria patternAverageData)
                {
                    dissapearedAds.Clear();
                    existingAds.Clear();
                    //i have now list of donloaded and list of last ads. => existing and dissapered = last
                    //downloaded not written  existing only
                    // last      written      existing+past
                    processDownloadedAds();
                    processReadAds();
                    findCoolAdsOfExisting(ref existingAds, patternAverageData);                    
                }
                void processDownloadedAds()
                {
                    foreach (var ad in downloadedAds)
                    {
                        existingAds.Add(ad);
                    }
                }
                void processReadAds()
                {
                    if (catLastAd == null || catLastAd.birth == null || catLastAd.birth == new DateTime())
                    {
                        ThreadSeeker.addThread("нет последнего объявления", "avito", true, false, true);
                        return;
                    }
                    List<Ad> mayBeDissaperingAds = new List<Ad>();
                    for (int i = 0; i < readAds.Count; i++)
                        if (readAds[i].birth >= catLastAd.birth)
                            mayBeDissaperingAds.Add(readAds[i]);
                        else
                            if (readAds[i].details.existing)
                            existingAds.Add(readAds[i]);
                        else dissapearedAds.Add(readAds[i]);
                    //all dissapered ads now are already saved earlier
                    processMayBeDissaperedAds(mayBeDissaperingAds.ToArray());
                }
                void processMayBeDissaperedAds(Ad[] dissaperingOrNot)
                {
                    //I have two parallel lists: previos existing and current existing
                    //from previos one i need only those which current existings lack
                    foreach (var v in dissaperingOrNot)
                    {
                        bool match = false;
                        foreach (var d in existingAds)
                            if (d.primary.id == v.primary.id)
                            {
                                match = true;
                                break;
                            }
                        if (match) continue;
                        v.details.existing = false;
                        v.details.renewminimumLifeTime(v.birth);
                        v.rating.getRealRating(v);
                        dissapearedAds.Add(v);
                    }
                }
                #endregion
                public void saveAds(IEnumerable<Ad> ads)
                {
                    foreach (var v in ads)
                        try
                        {
                            Ad.WritingAndReading.WriteToXmlFile(Ad.WritingAndReading.getAdInfoPath(v), v);
                        }
                        catch (Exception e)
                        {
                            ThreadSeeker.addThread("не удалось сохранить объявление", "avito", true, false, false);
                            ThreadSeeker.addThread(v.ToString() + ": " + e.Message, "avito", true, true, false);
                        }
                }
                #region calculateDatas
                public void calculateDatasFromDissapearedAdsAndSave(bool fullSearch)
                {
                    if (!fullSearch) return;
                    criteria = new Criteria(this, DateTime.Today);
                    long CriteriaAmount = 0;
                    for (int i = 0; i < dissapearedAds.Count; i++)
                    {
                        if (dissapearedAds[i].rating.realRatingOfAd == 0)
                            continue;
                        ThreadSeeker.currentActivity = owner.name + "." + name + ".getting criterias.melting." + i.ToString() + "/" + dissapearedAds.Count; 
                        CriteriaAmount += meltElements(dissapearedAds[i]);
                    }
                    criteria.calculateAverages();
                    criteria.save();
                    string output = "Посчитано " + CriteriaAmount + " критериев";
                    ThreadSeeker.addThread(output, "avito." + owner.name + "." + name, false, true, true);
                }
                long meltElements(Ad ad)
                {
                    long totalAmount = 0;
                    ad.rating.getElements(ad);
                    var elems = ad.rating.elements;
                    for (int i = 0; i < elems.Count; i++)
                        try
                        {
                            bool create = true;
                            for (int j = 0; j < criteria.typeValues.Count; j++)
                                if (criteria.typeValues[j].type == elems[i].typeOfValue)
                                    if (criteria.typeValues[j].value == elems[i].value)
                                    {
                                        create = false;
                                        criteria.typeValues[j].elements.Add(elems[i]);
                                        if (!criteria.typeValues[j].owners.Contains(elems[i].owner))
                                            criteria.typeValues[j].owners.Add(elems[i].owner);
                                        break;
                                    }
                            if (create)
                            {
                                Criteria.ElementTypeValue etv
                                    = new Criteria.ElementTypeValue(elems[i].typeOfValue, elems[i].value);
                                etv.elements.Add(elems[i]);
                                etv.owners.Add(elems[i].owner);
                                criteria.typeValues.Add(etv);
                                totalAmount++;
                            }
                        }
                        catch (Exception e)
                        { }
                    return totalAmount;
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="date"> leave empty for latest date. Format: yyyy.MM.dd</param>
                /// <returns></returns>
                public Criteria readSavedData(string date)
                {
                    Criteria c = null;                    
                    try
                    {
                        DateTime day = ThreadSeeker.convertStringToDateTime(date, false);
                        string path = Directory.GetCurrentDirectory() + "\\data\\avito\\" + owner.name + "\\" + name + "\\datas";
                        string datePath = "";
                        string[] years = Directory.GetDirectories(path);
                        foreach (var y in years) { datePath = y; if (new DirectoryInfo(y).Name == day.Year.ToString()) break; }
                        string[] months = Directory.GetFiles(datePath);
                        foreach (var m in months) { datePath = m;  if (new FileInfo(m).Name == day.Month.ToString() +"."+ day.Day.ToString() + ".txt" ) break; }
                        c = Ad.WritingAndReading.ReadFromXmlFile<Criteria>(datePath);
                        c.owningCategory = this;
                        c.owningPattern = this.owner;
                    }
                    catch
                    {
                        try
                        {
                            sleep(5, true);
                            string path = Directory.GetCurrentDirectory() + "\\data\\avito\\" + owner.name + "\\" + name+"\\datas";
                            string datePath = "";
                            string[] years = Directory.GetDirectories(path);
                            datePath = years[years.Length - 1];
                            string[] months = Directory.GetFiles(datePath);
                            datePath = months[months.Length - 1];
                            c = Ad.WritingAndReading.ReadFromXmlFile<Criteria>(datePath);
                            c.owningCategory = this;
                            c.owningPattern = this.owner;
                        }
                        catch { }
                    }
                    return c;
                }
                #endregion
                public void findCoolAdsOfExisting(ref List<Ad> ads, Criteria patternAverage)
                {
                    coolExistingAds.Clear();
                    for (int i = 0; i < ads.Count; i++)
                        try
                        {
                            ThreadSeeker.currentActivity = owner.name + "." + name + ".getting cool." + i.ToString() + "/" + ads.Count; 
                            if (ads[i].rating.estimatedRating == 0)
                                continue;
                            if (ads[i].rating.betterThanCategoryEstimationsStrictly || ads[i].rating.betterThanPatternEstimationsStrictly
                                || ads[i].rating.betterThanCategoryEstimationsStrictlyNot || ads[i].rating.betterThanPatternEstimationsStrictlyNot)
                            {
                                coolExistingAds.Add(ads[i]);
                                ads[i] = null;
                            }
                        }
                        catch(Exception e)
                        { }
                    ads = ads.FindAll(s => s != null);
                    long count = coolExistingAds.Count;
                    ThreadSeeker.addThread("Найдено " + coolExistingAds.Count +
                        " самых лучших объявлений", "avito." + owner.name + "." + name, true, true, true);
                }
            }
        }
        public class Criteria
        {
            [XmlIgnore]public Pattern.Category owningCategory;
            [XmlIgnore]public Pattern owningPattern;
            public DateTime calculationDate = new DateTime();
            public Criteria() { }
            public Criteria(Pattern.Category Owner, DateTime today)
            {
                owningCategory = Owner;
                owningPattern = Owner.owner;
                calculationDate = today;
            }
            public Criteria(Pattern owner, DateTime today)
            {
                owningPattern = owner;
                calculationDate = today;
            }
            public long averageRealRating = 0;
            public long averagePrice = 0;
            public TimeSpan averageminimumLifeTime = new TimeSpan();
            public List<ElementTypeValue> typeValues = new List<ElementTypeValue>();

            public void calculateAverages()
            {
                long generalRR = 0;
                long generalPrice = 0;
                for (int i = 0; i < this.typeValues.Count; i++)
                {
                    this.typeValues[i].calculateAverages();
                    generalRR += typeValues[i].averageRealRating;
                    generalPrice += typeValues[i].averagePrice;
                }
                try { averageRealRating = generalRR / typeValues.Count; } catch { }
                try { averagePrice = generalPrice / typeValues.Count; } catch { }
                //rr = price * 1000 / minutes
                //rr*minutes = price * 1000
                //minutes = price * 1000 / rr
                try
                {
                    int minutes = Convert.ToInt32(averagePrice * 1000 / averageRealRating);
                    averageminimumLifeTime = new TimeSpan(0, minutes, 0);
                }
                catch { }
            }
            public void save()
            {
                try
                {
                    if (owningCategory == null)
                        return;
                    string dirpath = Directory.GetCurrentDirectory() + "\\data\\avito\\"
                        + owningCategory.owner.name + "\\" + owningCategory.name + "\\datas\\"//pattern+category
                        + "\\" + calculationDate.Year.ToString();
                    string month = calculationDate.Month.ToString();
                    if (month.Length == 1) month = "0" + month;
                    string day = calculationDate.Day.ToString();
                    if (day.Length == 1) day = "0" + day;
                    string path = dirpath + "\\" + month + "." + day + ".txt";
                    Directory.CreateDirectory(dirpath);
                    Ad.WritingAndReading.WriteToXmlFile(path, this);
                }
                catch (Exception t)
                { }
            }
            public class ElementTypeValue
            {
                public string type = "";
                public string value = "";
                public ElementTypeValue(string Type, string Value)
                {
                    type = Type;
                    value = Value;
                }

                public long averageRealRating = 0;
                public long averagePrice = 0;
                public TimeSpan averageminimumLifeTime = new TimeSpan();

                public void calculateAverages()
                {
                    long generalRR = 0;
                    long generalPrice = 0;
                    long count = 0;
                    for (int i = 0; i < elements.Count; i++)
                        try
                        {
                            generalPrice += elements[i].estimatedPrice;
                            generalRR += elements[i].realRatingOfElement;
                            count++;
                        }
                        catch { }
                    if (count == 0) return;
                    averagePrice = generalPrice / count;
                    averageRealRating = generalRR / count;
                    //rr = price*1000/minutes
                    if (averageRealRating == 0) return;
                    int minutes = Convert.ToInt32(averagePrice * 1000 / averageRealRating);
                    averageminimumLifeTime = new TimeSpan(0, minutes, 0);
                }
                public List<Ad.Rating.Element> elements = new List<Ad.Rating.Element>();
                public List<Ad> owners = new List<Ad>();
            }
        }
    }
}


