using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using My;

namespace Avito
{
    public partial class AdsManagement : Form
    {

        #region formLoading
        public AdsManagement(ref AvitoClass.Pattern Pattern)
        {
            activeSearch = true;
            InitializeComponent();
            pattern = Pattern;
        }
        public AvitoClass.Pattern pattern;
        private void AdsManagement_Load(object sender, EventArgs e)
        {
            try
            {
                CheckForIllegalCrossThreadCalls = false;
                foreach (var v in pattern.categories)
                    comboBox12.Items.Add(v.name);
                sortedAds = SortedAds.getList(pattern);
                for (int i = 0; i < sortedAds.Count; i++)
                {
                    patternAdsBetterThanBoth += sortedAds[i].coolerThanBothAverages.Count;
                    patternAdsBetterThanCategory += sortedAds[i].coolerThanCategoryAverage.Count;
                    patternAdsBetterThanPattern += sortedAds[i].coolerThanPatternAverage.Count;
                }
                label10.ForeColor = Color.Red; label18.ForeColor = Color.Red;
                for (int i = 0; i < pattern.categories.Count; i++)
                {
                    patternExistingNonCoolAds += pattern.categories[i].existingAds.Count;
                    patternPastAdsCount += pattern.categories[i].dissapearedAds.Count;
                    patternAdsCount += pattern.categories[i].existingAds.Count;
                    patternAdsCount += pattern.categories[i].dissapearedAds.Count;
                    patternCoolAdsAllCount += pattern.categories[i].coolExistingAds.Count;
                    patternAdsCount += pattern.categories[i].coolExistingAds.Count;
                }
                this.Text += " для " + pattern.name +
                    "(" + DateTime.Now.ToString(My.ThreadSeeker.dateTimeFormat) + ")";
                label4.Text = patternAdsCount.ToString();
                label5.Text = patternCoolAdsAllCount.ToString();
                comboBox12_SelectedIndexChanged(sender, e);
                activeSearch = false;
            }
            catch (Exception re)
            {
                this.Text = "Нет информации. Откройте меня позже.";
            }
            new Thread(close).Start();
        }
        void close()
        {
            if (AvitoClass.sleep(60 * 60, false)) return;
            this.Close();
        }
        #endregion
        #region fields
        public int comboPhotoRating = 0;
        public int patternAdsCount = 0;

        public int patternExistingNonCoolAds = 0;
        public int patternPastAdsCount = 0;

        public int patternCoolAdsAllCount = 0;
        public int patternAdsBetterThanCategory = 0;
        public int patternAdsBetterThanPattern = 0;
        public int patternAdsBetterThanBoth = 0;
        

        public int categoryAdsCount = 0;
        public int categoryExistingNonCoolAds = 0;
        public int categoryPastAdsCount = 0;

        public int categoryCoolAdsAllCount = 0;
        public int categoryAdsBetterThanCategory = 0;
        public int categoryAdsBetterThanPattern = 0;
        public int categoryAdsBetterThanBoth = 0;
        

        List<SortedAds> sortedAds = null;//categories-dates-cools
        PopularInfo absolutePopularInfo = null;//possibly including patternInfo
        PopularInfo popularInfoForSearched = null;
                
        #endregion
        #region classes
        class SortedAds
        {
            //categories - dates - cools
            public string type = "";
            public string value = "";
            public List<AdToFind> dissapearedAds = new List<AdToFind>();
            public List<AdToFind> existingNonCoolAds = new List<AdToFind>();

            public List<AdToFind> coolerThanPatternAverage = new List<AdToFind>();
            public List<AdToFind> coolerThanCategoryAverage = new List<AdToFind>();
            public List<AdToFind> coolerThanBothAverages = new List<AdToFind>();

            public List<SortedAds> children = new List<SortedAds>();
            public static List<SortedAds> getList(AvitoClass.Pattern source)
            {
                List<SortedAds> res = new List<SortedAds>();
                try
                {
                    #region split categories
                    foreach (var v in source.categories)
                    {
                        var sa = new SortedAds() { type = "category", value = v.name };
                        for (int i = 0; i < v.existingAds.Count; i++) sa.existingNonCoolAds.Add(new AdToFind(v.existingAds[i], i));
                        for (int i = 0; i < v.dissapearedAds.Count; i++) sa.dissapearedAds.Add(new AdToFind(v.dissapearedAds[i], i));
                        for (int i = 0; i < v.coolExistingAds.Count; i++)
                        {
                            var r = v.coolExistingAds[i].rating;
                            if (r.betterThanCategoryEstimationsStrictlyNot && r.betterThanPatternEstimationsStrictlyNot)
                                sa.coolerThanBothAverages.Add(new AdToFind(v.coolExistingAds[i], i));
                            else
                            {
                                if (r.betterThanPatternEstimationsStrictlyNot)
                                    sa.coolerThanPatternAverage.Add(new AdToFind(v.coolExistingAds[i], i));
                                if (r.betterThanCategoryEstimationsStrictlyNot)
                                    sa.coolerThanCategoryAverage.Add(new AdToFind(v.coolExistingAds[i], i));
                            }
                        }
                        res.Add(sa);
                    }
                    #endregion
                    #region split dates
                    foreach (var oneCategory in res)
                    {
                        #region process dissapered
                        foreach (var diss in oneCategory.dissapearedAds)
                        {
                            bool match = false;
                            foreach (var oneDate in oneCategory.children)
                                if (oneDate.type == "date" && oneDate.value ==ThreadSeeker.convertDateTimeToString(diss.ad.birth, false))
                                {
                                    match = true;
                                    oneDate.dissapearedAds.Add(diss);
                                    break;
                                }
                            if (!match)
                            {
                                var sa = new SortedAds() { type = "date", value = ThreadSeeker.convertDateTimeToString(diss.ad.birth, false) };
                                sa.dissapearedAds.Add(diss);
                                oneCategory.children.Add(sa);
                            }
                        }
                        #endregion
                        #region process existing non cool
                        foreach (var exist in oneCategory.existingNonCoolAds)
                        {
                            bool match = false;
                            foreach (var oneDate in oneCategory.children)
                                if (oneDate.type == "date" && oneDate.value == ThreadSeeker.convertDateTimeToString(exist.ad.birth, false))
                                {
                                    match = true;
                                    oneDate.existingNonCoolAds.Add(exist);
                                    break;
                                }
                            if (!match)
                            {
                                var sa = new SortedAds() { type = "date", value = ThreadSeeker.convertDateTimeToString(exist.ad.birth, false) };
                                sa.existingNonCoolAds.Add(exist);
                                oneCategory.children.Add(sa);
                            }
                        }
                        #endregion
                        #region process both cool
                        foreach (var exist in oneCategory.coolerThanBothAverages)
                        {
                            bool match = false;
                            foreach (var oneDate in oneCategory.children)
                                if (oneDate.type == "date" && oneDate.value == ThreadSeeker.convertDateTimeToString(exist.ad.birth, false))
                                {
                                    match = true;
                                    oneDate.coolerThanBothAverages.Add(exist);
                                    break;
                                }
                            if (!match)
                            {
                                var sa = new SortedAds() { type = "date", value = ThreadSeeker.convertDateTimeToString(exist.ad.birth, false) };
                                sa.coolerThanBothAverages.Add(exist);
                                oneCategory.children.Add(sa);
                            }
                        }
                        #endregion
                        #region process pattern cool
                        foreach (var exist in oneCategory.coolerThanPatternAverage)
                        {
                            bool match = false;
                            foreach (var oneDate in oneCategory.children)
                                if (oneDate.type == "date" && oneDate.value == ThreadSeeker.convertDateTimeToString(exist.ad.birth, false))
                                {
                                    match = true;
                                    oneDate.coolerThanPatternAverage.Add(exist);
                                    break;
                                }
                            if (!match)
                            {
                                var sa = new SortedAds() { type = "date", value = ThreadSeeker.convertDateTimeToString(exist.ad.birth, false) };
                                sa.coolerThanPatternAverage.Add(exist);
                                oneCategory.children.Add(sa);
                            }
                        }
                        #endregion
                        #region process category cool
                        foreach (var exist in oneCategory.coolerThanCategoryAverage)
                        {
                            bool match = false;
                            foreach (var oneDate in oneCategory.children)
                                if (oneDate.type == "date" && oneDate.value ==  ThreadSeeker.convertDateTimeToString(exist.ad.birth, false))
                                {
                                    match = true;
                                    oneDate.coolerThanCategoryAverage.Add(exist);
                                    break;
                                }
                            if (!match)
                            {
                                var sa = new SortedAds() { type = "date", value = ThreadSeeker.convertDateTimeToString(exist.ad.birth, false) };
                                sa.coolerThanCategoryAverage.Add(exist);
                                oneCategory.children.Add(sa);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
                catch(Exception e)
                { }
                return res;
            }
            public class AdToFind
            {
                public Ad ad = null;
                public int number = 0;
                //cool and existing or not can be found in ad                
                public AdToFind(Ad ad, int number)
                {
                    this.ad = ad;
                    this.number = number;
                }
            }
        }
        class PopularInfo
        {
            public string popName = ""; public int popamoutname = 0;                             List<Value> names;
            public string popCategory = ""; public int popamountcategory = 0;                    List<Value> categories;
            public int popPrice = 0;/*average  */                                                List<Value> prices;
            public int popPhoto = 0;/*average*/                                                  List<Value> photos;
            public string popPlace = ""; public int popamountplace = 0;                          List<Value> places;
            public bool popExisting = false; public int popamountexisting = 0;                   List<Value> existings;
            public string popDescription = ""; public int popamountdescription = 0;              List<Value> descriptions;
            public DateTime popBirthDay = new DateTime(); public int popamountbirthday = 0;      List<Value> birthdays;
            public DateTime popLastCheckDay = new DateTime(); public int popamountlastcheckday = 0; List<Value> lastcheckdays;
            public int popRealRating = 0;/*average*/                                             List<Value> realratings;
            public TimeSpan popLifeTime = new TimeSpan();/*average*/                             List<Value> lifetimes;
            public TimeSpan popEstLifeTime = new TimeSpan();/*average*/                          List<Value> estlifetimes;
            public int popEstPrice = 0;/*average*/                                               List<Value> estprices;
            public int popEstRating = 0;/*average*/                                              List<Value> estratings;

            

            public List<SortedAds> allSortedAds = new List<SortedAds>();
            public List<SortedAds.AdToFind> fitableAds = new List<SortedAds.AdToFind>();
            class Value
            {
                public string value;
                public int amount;
            }
            public void findAveragesOfAllAds( Label progressShower)
            {
                #region renewLists                
                names = new List<Value>();
                categories = new List<Value>();
                prices = new List<Value>();
                photos = new List<Value>();
                places = new List<Value>();
                existings = new List<Value>();
                descriptions = new List<Value>();
                birthdays = new List<Value>();
                lastcheckdays = new List<Value>();
                realratings = new List<Value>();
                lifetimes = new List<Value>();
                estlifetimes = new List<Value>();
                estprices = new List<Value>();
                estratings = new List<Value>();
                #endregion
                List<Ad> all = new List<Ad>();
                int plus = 0;
                for (int i = 0; i < allSortedAds.Count; i++)
                {
                    allSortedAds[i].existingNonCoolAds.ForEach(s => fillAll(ref plus, progressShower, s.ad, ref all));
                    allSortedAds[i].dissapearedAds.ForEach(s => fillAll(ref plus, progressShower, s.ad, ref all));
                    allSortedAds[i].coolerThanBothAverages.ForEach(s => fillAll(ref plus, progressShower, s.ad, ref all));
                    allSortedAds[i].coolerThanCategoryAverage.ForEach(s => fillAll(ref plus, progressShower, s.ad, ref all));                                        
                    allSortedAds[i].coolerThanPatternAverage.ForEach(s => fillAll(ref plus, progressShower, s.ad, ref all));
                }
                plus = 0;
                for(int i = 0; i< all.Count;i++)
                {                    
                    plus = i + 1;
                    if (plus % 50 == 0)
                        progressShower.Text = "getting info(2/3):" + plus + "/" + all.Count;
                    fillListsOfValues(all[i]);
                }
                getDataToShow( progressShower);
            }
            void fillAll(ref int plus, Label progressShower, Ad ad, ref List<Ad> ads)
            {
                ads.Add(ad);
                plus++;
                if (plus % 50 == 0)
                    progressShower.Text = "getting info(1/3):" + plus + "/" + allSortedAds.Count;
            }
            public void findAveragesOfFitableAds( Label progressShower)
            {
                #region renewLists                
                names = new List<Value>();
                categories = new List<Value>();
                prices = new List<Value>();
                photos = new List<Value>();
                places = new List<Value>();
                existings = new List<Value>();
                descriptions = new List<Value>();
                birthdays = new List<Value>();
                lastcheckdays = new List<Value>();
                realratings = new List<Value>();
                lifetimes = new List<Value>();
                estlifetimes = new List<Value>();
                estprices = new List<Value>();
                estratings = new List<Value>();
                #endregion
                for(int i=0;i< fitableAds.Count;i++)                
                {
                    int plus = i + 1;
                    if (plus % 50 == 0)
                        progressShower.Text = "getting info(2/3):" + plus + "/" + fitableAds.Count;
                    fillListsOfValues(fitableAds[i].ad);
                }
                getDataToShow( progressShower);
            }
            void fillListsOfValues(Ad ad)
            {
                try
                {
                    bool match = false;
                    #region process names
                    string[] adnames = ad.primary.name.Split();
                    foreach (var an in adnames)
                    {
                        match = false;
                        foreach (var n in names)
                            if (n.value == an)
                            {
                                match = true;
                                n.amount++;
                                break;
                            }
                        if (!match)
                        {
                            Value v = new Value();
                            v.value = an;
                            v.amount = 1;
                            names.Add(v);
                        }
                    }
                    #endregion
                    #region process categories
                    match = false;
                    foreach (var v in categories)
                        if (v.value == ad.primary.category)
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ad.primary.category;
                        v.amount = 1;
                        categories.Add(v);
                    }
                    #endregion
                    #region process prices
                    match = false;
                    foreach (var v in prices)
                        if (v.value == ad.primary.price.ToString())
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ad.primary.price.ToString();
                        v.amount = 1;
                        prices.Add(v);
                    }
                    #endregion
                    #region process photos
                    match = false;
                    foreach (var v in photos)
                        if (v.value == ad.primary.amountPhotos.ToString())
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ad.primary.amountPhotos.ToString();
                        v.amount = 1;
                        photos.Add(v);
                    }
                    #endregion
                    #region process places
                    match = false;
                    foreach (var v in places)
                        if (v.value == ad.primary.place)
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ad.primary.place;
                        v.amount = 1;
                        places.Add(v);
                    }
                    #endregion
                    #region process existings
                    match = false;
                    foreach (var v in existings)
                        if (v.value == ad.details.existing.ToString())
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ad.details.existing.ToString();
                        v.amount = 1;
                        existings.Add(v);
                    }
                    #endregion
                    #region process descriptions
                    string[] addescs = ad.details.description.Split();
                    foreach (var an in addescs)
                    {
                        match = false;
                        foreach (var n in descriptions)
                            if (n.value == an)
                            {
                                match = true;
                                n.amount++;
                                break;
                            }
                        if (!match)
                        {
                            Value v = new Value();
                            v.value = an;
                            v.amount = 1;
                            descriptions.Add(v);
                        }
                    }
                    #endregion
                    #region process birthdays
                    match = false;
                    foreach (var v in birthdays)
                        if (v.value == ThreadSeeker.convertDateTimeToString(ad.birth, false))
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ThreadSeeker.convertDateTimeToString(ad.birth, false);
                        v.amount = 1;
                        birthdays.Add(v);
                    }
                    #endregion
                    #region process lastcheckday
                    match = false;
                    foreach (var v in lastcheckdays)
                        try
                        {
                            if (v.value == ThreadSeeker.convertDateTimeToString(ad.details.checkings[ad.details.checkings.Count - 1], false))
                            {
                                match = true;
                                v.amount++;
                                break;
                            }
                        }
                        catch { }
                    if (!match)
                        try
                        {
                            Value v = new Value();
                            v.value = ThreadSeeker.convertDateTimeToString(ad.details.checkings[ad.details.checkings.Count - 1], false);
                            v.amount = 1;
                            lastcheckdays.Add(v);
                        }
                        catch { }
                    #endregion
                    #region process realratings
                    match = false;
                    foreach (var v in realratings)
                        if (v.value == ad.rating.realRatingOfAd.ToString())
                        {
                            match = true;
                            v.amount++;
                            break;
                        }
                    if (!match)
                    {
                        Value v = new Value();
                        v.value = ad.rating.realRatingOfAd.ToString();
                        v.amount = 1;
                        realratings.Add(v);
                    }
                    #endregion
                    #region process lifetimes
                    ad.details.renewminimumLifeTime(ad.birth);
                    Value ltv = new Value();
                    try
                    {
                        ltv.value = ThreadSeeker.convertTimeSpanToString(ad.details.minimumLifeTime);
                    }
                    catch(Exception e)
                    { }
                    ltv.amount = 1;
                    lifetimes.Add(ltv);
                    #endregion
                    try
                    {
                        #region process estlifetimes                    
                        Value eltv = new Value();
                        eltv.value = ThreadSeeker.convertTimeSpanToString(ad.rating.estimatedMinimumLifeTime);
                        eltv.amount = 1;
                        estlifetimes.Add(eltv);
                        #endregion
                        #region process estprices
                        match = false;
                        foreach (var v in estprices)
                            if (v.value == ad.rating.estimatedPrice.ToString())
                            {
                                match = true;
                                v.amount++;
                                break;
                            }
                        if (!match)
                        {
                            Value v = new Value();
                            v.value = ad.rating.estimatedPrice.ToString();
                            v.amount = 1;
                            estprices.Add(v);
                        }
                        #endregion
                        #region process estratings
                        match = false;
                        foreach (var v in estratings)
                            if (v.value == ad.rating.estimatedRating.ToString())
                            {
                                match = true;
                                v.amount++;
                                break;
                            }
                        if (!match)
                        {
                            Value v = new Value();
                            v.value = ad.rating.estimatedRating.ToString();
                            v.amount = 1;
                            estratings.Add(v);
                        }
                        #endregion
                    }
                    catch { }
                }
                catch (Exception e)
                { }
            }
            void getDataToShow( Label progressShower)
            {
                try
                {
                    #region names
                    progressShower.Text = "getting info(3/3):1/14";
                    foreach (var v in names) if (v.amount > popamoutname)
                        { popName = v.value; popamoutname = v.amount; }
                    #endregion
                    #region categories
                    progressShower.Text = "getting info(3/3):2/14";
                    foreach (var v in categories) if (v.amount > popamountcategory)
                        { popCategory = v.value; popamountcategory = v.amount; }
                    #endregion
                    #region prices
                    try
                    {
                        progressShower.Text = "getting info(3/3):3/14";
                        int genprice = 0; int divprice = 0; foreach (var v in prices) { divprice += v.amount; int gp = v.amount * int.Parse(v.value); genprice += gp; }
                        if (divprice != 0) popPrice = genprice / divprice;
                    }
                    catch { }
                    #endregion
                    #region photos
                    try
                    {
                        progressShower.Text = "getting info(3/3):4/14";
                        int genphoto = 0; int divphoto = 0; foreach (var v in photos) { divphoto += v.amount; int gp = v.amount * int.Parse(v.value); genphoto += gp; }
                        if (divphoto != 0) popPhoto = genphoto / divphoto;
                    }
                    catch
                    { }
                    #endregion
                    #region places
                    try
                    {
                        progressShower.Text = "getting info(3/3):5/14";
                        foreach (var v in places) if (v.amount > popamountplace)
                            { popPlace = v.value; popamountplace = v.amount; }
                    }
                    catch
                    { }
                    #endregion
                    #region existings
                    try
                    {
                        progressShower.Text = "getting info(3/3):6/14";
                        foreach (var v in existings) if (v.amount > popamountexisting)
                            { popExisting = bool.Parse(v.value); popamountexisting = v.amount; }
                    }
                    catch
                    { }
                    #endregion
                    #region descriptions
                    try
                    {
                        progressShower.Text = "getting info(3/3):7/14";
                        foreach (var v in descriptions) if (v.amount > popamountdescription)
                            { popDescription = v.value; popamountdescription = v.amount; }
                    }
                    catch
                    { }
                    #endregion
                    #region birthdays
                    try
                    {
                        progressShower.Text = "getting info(3/3):8/14";
                        foreach (var v in birthdays)
                        {
                            if (v.amount <= popamountbirthday) continue;
                            DateTime dt = ThreadSeeker.convertStringToDateTime(v.value,false);
                            popBirthDay = dt;
                            popamountbirthday = v.amount;
                        }
                    }
                    catch (Exception e)
                    { }
                    #endregion
                    #region lastchecks
                    try
                    {
                        progressShower.Text = "getting info(3/3):9/14";
                        foreach (var v in lastcheckdays)
                        {
                            if (v.amount <= popamountlastcheckday) continue;
                            DateTime dt = ThreadSeeker.convertStringToDateTime(v.value, false);
                            popLastCheckDay = dt;
                            popamountlastcheckday = v.amount;
                        }
                    }
                    catch
                    { }
                    #endregion
                    #region realratings
                    try
                    {
                        progressShower.Text = "getting info(3/3):10/14";
                        int genrr = 0; int divrr = 0; foreach (var v in realratings) { divrr += v.amount; int ta = v.amount * int.Parse(v.value); genrr += ta; }
                        if (divrr != 0) popRealRating = genrr / divrr;
                    }
                    catch
                    { }
                    #endregion
                    #region lifetimes
                    try
                    {
                        progressShower.Text = "getting info(3/3):11/14";
                        long mins = 0; long lftdivs = 0;
                        foreach (var v in lifetimes)
                        {                            
                            TimeSpan ts = ThreadSeeker.convertStringToTimeSpan(v.value);
                            mins += Convert.ToInt64(ts.TotalMinutes);
                            lftdivs += v.amount;
                        }
                        if (lftdivs != 0)
                        {
                            long avlt = mins / lftdivs;
                            popLifeTime = new TimeSpan(0, Convert.ToInt32(avlt), 0);
                        }
                    }
                    catch
                    { }
                    #endregion
                    #region estlifetimes
                    try
                    {
                        progressShower.Text = "getting info(3/3):12/14";
                        long estmins = 0; long estlftdivs = 0;
                        foreach (var v in estlifetimes)
                        {
                            TimeSpan ts = ThreadSeeker.convertStringToTimeSpan(v.value);
                            estmins += Convert.ToInt64(ts.TotalMinutes);
                            estlftdivs += v.amount;
                        }
                        if (estlftdivs != 0)
                        {
                            long avlt = estmins / estlftdivs;
                            popEstLifeTime = new TimeSpan(0, Convert.ToInt32(avlt), 0);
                        }
                    }
                    catch
                    { }
                    #endregion
                    #region estprices
                    try
                    {
                        progressShower.Text = "getting info(3/3):13/14";
                        int genestprice = 0; int divestprice = 0; foreach (var v in estprices) { divestprice += v.amount; int gp = v.amount * int.Parse(v.value); genestprice += gp; }
                        if (divestprice != 0) popEstPrice = genestprice / divestprice;
                    }
                    catch
                    { }
                    #endregion
                    #region estratings
                    try
                    {
                        progressShower.Text = "getting info(3/3):14/14";
                        int genestr = 0; int divestr = 0; foreach (var v in estratings) { divestr += v.amount; int ta = v.amount * int.Parse(v.value); genestr += ta; }
                        if (divestr != 0) popEstRating = genestr / divestr;
                    }
                    catch
                    { }
                    #endregion
                }
                catch (Exception e)
                { }
            }
        }
        #endregion
        #region service
        void coloriseSearch(object sender, EventArgs e)
        {
            button1.BackColor = Color.Yellow;
        }

        void enableSomeintActivityControls(bool enable)
        {
            comboBox12.Enabled = enable;
            button2.Enabled = enable;
            button1.Enabled = enable;
            dataGridView1.Enabled = enable;
            label14.Visible = enable;
            label11.Visible = enable;         
        }

        private void button03_Click(object sender, EventArgs e)
        {
            if (button03.Text == "x>")
                button03.Text = "x>=";
            else
            {
                if (button03.Text == "x>=")
                    button03.Text = "x=";
                else
                {
                    if (button03.Text == "x=")
                        button03.Text = "x<=";
                    else
                    {
                        if (button03.Text == "x<=")
                            button03.Text = "x<";
                        else
                            button03.Text = "x>";
                    }
                }
            }
        }
        #endregion
        #region categoryChanged
        private void comboBox12_SelectedIndexChanged(object sender, EventArgs e)
        {
            coloriseSearch(sender, e);
            enableSomeintActivityControls(false);
            new Thread(calculateSelectedCategory).Start();
        }
        //get selectedCategorySubsearcheds
       
        void calculateSelectedCategory()
        {
            try
            {
                label15.Text = "0"; label14.Text = "0";
                categoryAdsCount = 0; categoryExistingNonCoolAds = 0; categoryPastAdsCount = 0;
                categoryCoolAdsAllCount = 0; categoryAdsBetterThanBoth = 0;
                categoryAdsBetterThanCategory = 0; categoryAdsBetterThanPattern = 0;
                absolutePopularInfo = new PopularInfo();
                #region fill absolute popular info
                List<string> sselectedCategories = new List<string>();
                string x = comboBox12.SelectedItem.ToString();
                if (x == "Все") for (int i = 1; i < comboBox12.Items.Count; i++) sselectedCategories.Add(comboBox12.Items[i].ToString());
                else sselectedCategories.Add(comboBox12.SelectedItem.ToString());

                List<SortedAds> selectedCategories = new List<SortedAds>();
                for (int i = 0; i < sselectedCategories.Count; i++)                
                    foreach (var category in sortedAds)                    
                        if (category.value == sselectedCategories[i])
                            selectedCategories.Add(category);
                #region drop ads to absolute popular info
                foreach (var cat in selectedCategories)
                {
                    absolutePopularInfo.allSortedAds.Add(cat);
                    categoryExistingNonCoolAds += cat.existingNonCoolAds.Count;
                    categoryAdsCount += cat.existingNonCoolAds.Count;
                    categoryPastAdsCount += cat.dissapearedAds.Count;
                    categoryAdsCount += cat.dissapearedAds.Count;
                    int both = cat.coolerThanBothAverages.Count;
                    categoryAdsBetterThanBoth += both;
                    categoryCoolAdsAllCount += both;
                    categoryAdsCount += both;
                    int pat = cat.coolerThanPatternAverage.Count;
                    categoryAdsBetterThanPattern += pat;
                    categoryCoolAdsAllCount += pat;
                    categoryAdsCount += pat;
                    int ca = cat.coolerThanCategoryAverage.Count;
                    categoryAdsBetterThanCategory += ca;
                    categoryCoolAdsAllCount += ca;
                    categoryAdsCount += ca;
                }
                #endregion
                absolutePopularInfo.findAveragesOfAllAds( label15);
                label15.Text = categoryAdsCount.ToString(); label14.Text = categoryCoolAdsAllCount.ToString();
                #endregion
                dataGridView1.SuspendLayout();
                dataGridView1.Rows.Clear();
                dataGridView1.ResumeLayout();
                reshowCategoryMetaData();
                button1.BackColor = Color.Red;
            }
            catch (Exception e)
            { }
            enableSomeintActivityControls(true);
        }        
        void processAdGroupAndGetAmount(SortedAds category)
        {
            for (int j = 0; j < category.dissapearedAds.Count; j++)
            {
                if (category.dissapearedAds[j].ad.rating.betterThanPatternEstimationsStrictly || category.dissapearedAds[j].ad.rating.betterThanCategoryEstimationsStrictly)
                { }
            }
            for (int j = 0; j < category.existingNonCoolAds.Count; j++)
            {
                if (category.existingNonCoolAds[j].ad.rating.betterThanCategoryEstimationsStrictly || category.existingNonCoolAds[j].ad.rating.betterThanPatternEstimationsStrictly)
                { }
            }
            absolutePopularInfo.allSortedAds.Add(category);
        }
        void reshowCategoryMetaData()
        {
            var info = absolutePopularInfo;
            try { textBox18.Text = info.popName + "(" + info.popamoutname + ")"; } catch { }
            try { textBox19.Text = info.popCategory + "(" + info.popamountcategory + ")"; } catch { }
            try { textBox20.Text = info.popPrice.ToString(); } catch { }
            try { textBox22.Text = info.popPhoto.ToString(); } catch { }
            try { textBox21.Text = info.popPlace + "(" + info.popamountplace + ")"; } catch { }
            try { textBox26.Text = info.popExisting.ToString() + "(" + info.popamountexisting + ")"; } catch { }
            try { textBox28.Text = info.popDescription + "(" + info.popamountdescription + ")"; } catch { }
            try { textBox23.Text = ThreadSeeker.convertDateTimeToString(info.popBirthDay, false) + "(" + info.popamountbirthday + ")"; } catch { }
            try { textBox24.Text = ThreadSeeker.convertDateTimeToString(info.popLastCheckDay, false) + "(" + info.popamountlastcheckday + ")"; } catch { }
            try { textBox27.Text = info.popRealRating.ToString(); } catch { }
            try { textBox25.Text = ThreadSeeker.convertTimeSpanToString(info.popLifeTime); } catch { }
            try { textBox29.Text = ThreadSeeker.convertTimeSpanToString(info.popEstLifeTime); } catch { }
            try { textBox30.Text = info.popEstPrice.ToString(); } catch { }
            try { textBox31.Text = info.popEstRating.ToString(); } catch { }
        }
        #endregion 
        #region resetSettings
        private void button2_Click(object sender, EventArgs e)
        {
            resetSettings();
        }
        void resetSettings()
        {
            checkBox2.Checked = false;
            checkBox1.Checked = false;
            textBox00.Text = "";
            textBox09.Text = "";
            checkBox11.Checked = true;
            checkBox111.Checked = true;
            button03.Text = "x>=";
            textBox03.Text = "00.00.00";
            textBox02.Text = "93.00.00";
            textBox04.Text = "0";
            textBox041.Text = "100000000";
            textBox05.Text = "0";
            textBox051.Text = "100000000";
            textBox07.Text = "0";
            textBox071.Text = "100000000";
            textBox10.Text = "0";
            textBox101.Text = "100000000";
            //textBox06.Text = "0";
            //textBox061.Text = "10";
            textBox17.Text = "00.00.00";
            textBox4.Text = "93.00.00";
            textBox01.Text = "";
            textBox08.Text = "0";
            textBox081.Text = "100";
        }
        #endregion
        #region search
        public bool activeSearch = false;

        public const int percentageOfPossibleDispersionOfRatings = 20;
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.BackColor == Color.Green)
                return;
            button1.BackColor = Color.DarkViolet;
            enableSomeintActivityControls(false);
            new Thread(search).Start();
        }
        void search()
        {
            activeSearch = true;
            try
            {
                popularInfoForSearched = new PopularInfo();
                innerSearchInAbsoluteSortedAds(label6);
                popularInfoForSearched.findAveragesOfFitableAds(label6);
                showResults();
                enableSomeintActivityControls(true);
                button1.BackColor = Color.Green;
            }
            catch(Exception e)
            { }
            activeSearch = false;
        }
        void innerSearchInAbsoluteSortedAds(Label progressShower)
        {
            #region read form
            #region primary
            string name = textBox00.Text;
            string place = textBox09.Text;
            string sforLastDays = textBox02.Text; 
            TimeSpan forLastDays = new TimeSpan(); try { forLastDays = ThreadSeeker.convertStringToTimeSpan(sforLastDays); } catch { }
            string sminPrice = textBox04.Text; int minprice = 0; int.TryParse(sminPrice, out minprice);
            string smaxPrice = textBox041.Text; int maxprice = 0; int.TryParse(smaxPrice, out maxprice);
            string sminAmoPhot = textBox08.Text; int minPA = 0; int.TryParse(sminAmoPhot, out minPA);
            string smaxAmoPhot = textBox081.Text; int maxPA = 0; int.TryParse(smaxAmoPhot, out maxPA);
            #endregion
            #region details
            bool existing = checkBox11.Checked;
            bool past = checkBox111.Checked;
            string sMoreOrLess = button03.Text;
            string slifetime = textBox03.Text; 
            TimeSpan lifeTime = new TimeSpan(); try { lifeTime = ThreadSeeker.convertStringToTimeSpan(slifetime);  } catch { }
            string description = textBox01.Text;
            #endregion
            #region rating
            bool regular = checkBox4.Checked;
            bool patternCool = checkBox2.Checked;
            bool categoryCool = checkBox3.Checked;
            bool relativelyAccurateRating = checkBox1.Checked;
            string sminRR = textBox07.Text; int minRR = 0; int.TryParse(sminRR, out minRR);
            string smaxRR = textBox071.Text; int maxRR = 0; int.TryParse(smaxRR, out maxRR);
            string sminER = textBox10.Text; int minER = 0; int.TryParse(sminER, out minER);
            string smaxER = textBox101.Text; int maxER = 0; int.TryParse(smaxRR, out maxER);
            string sminEP = textBox05.Text; int minEP = 0; int.TryParse(sminEP, out minEP);
            string smaxEP = textBox051.Text; int maxEP = 0; int.TryParse(smaxEP, out maxEP);
            //string sminphotoRating = textBox06.Text; int minPR = 0; int.TryParse(sminphotoRating, out minPR);
            //string smaxphotoRating = textBox061.Text; int maxPR = 0; int.TryParse(smaxphotoRating, out maxPR);
            string sminELifeTime = textBox17.Text;
            TimeSpan minELT = new TimeSpan(); try { minELT = ThreadSeeker.convertStringToTimeSpan(sminELifeTime); } catch { }
            string smaxELifeTime = textBox4.Text;
            TimeSpan maxELT = new TimeSpan(); try { maxELT = ThreadSeeker.convertStringToTimeSpan(smaxELifeTime); } catch { }
            bool potentiallyProfitable = checkBox5.Checked;
            #endregion
            #endregion
            List<SortedAds> selectedDates = new List<SortedAds>();
            #region get ads for selected days
            foreach (var selectedCategory in absolutePopularInfo.allSortedAds)            
                foreach (var oneDate in selectedCategory.children)
                {
                    DateTime date = ThreadSeeker.convertStringToDateTime(oneDate.value, false);
                    if (DateTime.Now - forLastDays <= date)
                        selectedDates.Add(oneDate);
                }
            SortedAds sorteds = new SortedAds() { type = "datetime", value = "selected days"};
            foreach (var v in selectedDates)
            {
                sorteds.coolerThanBothAverages.AddRange(v.coolerThanBothAverages);
                sorteds.coolerThanCategoryAverage.AddRange(v.coolerThanCategoryAverage);
                sorteds.coolerThanPatternAverage.AddRange(v.coolerThanPatternAverage);
                sorteds.dissapearedAds.AddRange(v.dissapearedAds);
                sorteds.existingNonCoolAds.AddRange(v.existingNonCoolAds);
            }
            #endregion
            List<SortedAds.AdToFind> checkedCoolAndExistingAndPast = new List<SortedAds.AdToFind>();
            #region check for cool and existing and past
            if (existing)
            {
                if (patternCool && categoryCool)
                    checkedCoolAndExistingAndPast.AddRange(sorteds.coolerThanBothAverages);
                else
                {
                    if (patternCool)
                        checkedCoolAndExistingAndPast.AddRange(sorteds.coolerThanPatternAverage);
                    if (categoryCool)
                        checkedCoolAndExistingAndPast.AddRange(sorteds.coolerThanCategoryAverage);                
                }
                if (regular)
                    checkedCoolAndExistingAndPast.AddRange(sorteds.existingNonCoolAds); 
            }
            if (past && regular)
                checkedCoolAndExistingAndPast.AddRange(sorteds.dissapearedAds);
            #endregion
            for(int i=0;i< checkedCoolAndExistingAndPast.Count;i++)                            
            {
                var adf = checkedCoolAndExistingAndPast[i];
                int plus = i + 1;
                if (plus % 50 == 0)
                    progressShower.Text = "getting info(1/3):" + plus + "/" + checkedCoolAndExistingAndPast.Count;
                var ad = adf.ad;
                #region check primary           
                if (DateTime.Now - forLastDays > ad.birth) continue;
                if (!ad.primary.place.Contains(place.ToLower())) continue;
                if (!ad.primary.name.Contains(name.ToLower())) continue;
                if (ad.primary.price < minprice) continue;
                if (ad.primary.price > maxprice) continue;
                //if (ad.primary.UserPhotoRating < minPR) continue;
                //if (ad.primary.UserPhotoRating > maxPR) continue;
                if (ad.primary.amountPhotos > maxPA) continue;
                if (ad.primary.amountPhotos < minPA) continue;
                #endregion
                #region check details 
                if (lifeTime != new TimeSpan())
                {
                    bool add = true;
                    switch (sMoreOrLess)
                    {
                        default: break;
                        case "x>": { if (ad.details.minimumLifeTime <= lifeTime) add = false; break; }
                        case "x>=": { if (ad.details.minimumLifeTime < lifeTime) add = false; break; }
                        case "x=": { if (ad.details.minimumLifeTime != lifeTime) add = false; break; }
                        case "x<=": { if (ad.details.minimumLifeTime > lifeTime) add = false; break; }
                        case "x<": { if (ad.details.minimumLifeTime >= lifeTime) add = false; break; }
                    }
                    if (!add) continue;
                }
                if (!ad.details.description.Contains(description)) continue;
                #endregion
                #region check rating
                if (ad.rating.realRatingOfAd < minRR || ad.rating.realRatingOfAd > maxRR) continue;
                if (relativelyAccurateRating)
                {
                    double maxPossibleER = ad.rating.realRatingOfAd * 120 / 100;
                    double minPossibleER = ad.rating.realRatingOfAd * 80 / 100;
                    if (ad.rating.estimatedRating > maxPossibleER || ad.rating.estimatedRating < minPossibleER) continue;
                }
                if (ad.rating.estimatedRating < minER || ad.rating.estimatedRating > maxER) continue;
                if (ad.rating.estimatedPrice < minEP || ad.rating.estimatedPrice > maxEP) continue;
                if (ad.rating.estimatedMinimumLifeTime < minELT || ad.rating.estimatedMinimumLifeTime > maxELT) continue;
                if (potentiallyProfitable && ad.rating.estimatedPrice <= ad.primary.price) continue;
                #endregion
                //if meets all requiremnts
                popularInfoForSearched.fitableAds.Add(adf);
            }
        }
        public int adsShowedCount = 0;
        public int rowsAdded = 0;
        void showResults()
        {
            #region clear things
            try
            {
                dataGridView1.SuspendLayout(); label11.Text = "0";
                dataGridView1.Rows.Clear();
                label6.Text = "0";
                for (int i = 5; i > 0; i--)
                {
                    label11.Text = "sleep " + i;
                    AvitoClass.sleep(1, false);
                }
            }
            catch(Exception e)
            { }
            #endregion
            #region add ads
            try
            {
                List<Ad> adsToShow = new List<Ad>();
                foreach (var ats in popularInfoForSearched.fitableAds)
                    adsToShow.Add(ats.ad);
                //dataGridView1.DataSource = adsToShow;
                adsShowedCount = adsToShow.Count;
                int cooladscount = 0;
                rowsAdded = 0;
                for (int i = 0; i < adsToShow.Count; i++)
                {
                    var ad = adsToShow[i];
                    if (ad.rating.betterThanCategoryEstimationsStrictlyNot || ad.rating.betterThanPatternEstimationsStrictlyNot)
                        cooladscount++;
                    try
                    {
                        #region build row
                        /* this.name,
                this.source,
                this.category,
                this.cool,
                this.price,
                this.estprice,
                this.photoAmount,
                this.place,
                this.existing,
                this.description,
                this.birth,
                this.checking,
                this.lifetime,
                this.estlifetime,
                this.lefttominestlifetime,
                this.lefttomaxestlifetime,
                this.realRating,
                this.estimatedRating,
                this.Navigation});*/
                        string name = ad.primary.name + "(" + ad.primary.id + ")";
                        string estprice = ad.rating.estimatedPrice.ToString();
                        string estlt = ThreadSeeker.convertTimeSpanToString(ad.rating.estimatedMinimumLifeTime);
                        long rrminminusestr = ad.rating.realRatingOfAd - ad.rating.estimatedRating;
                        string better = "";
                        if (ad.rating.betterThanCategoryEstimationsStrictlyNot && ad.rating.betterThanPatternEstimationsStrictlyNot)
                            better = "лучшее";
                        else
                        {
                            if (ad.rating.betterThanPatternEstimationsStrictlyNot)
                                better = "запрос";
                            if (ad.rating.betterThanCategoryEstimationsStrictlyNot)
                                better = "категория";
                            if (!ad.rating.betterThanCategoryEstimationsStrictlyNot && !ad.rating.betterThanPatternEstimationsStrictlyNot)
                                better = "обычное";
                        }

                        dataGridView1.Rows.Add(
                            i.ToString(),
                            name,
                            ad.primary.source,
                            ad.primary.category,
                            better,
                            ad.primary.price,
                            estprice,
                            ad.primary.amountPhotos,
                            ad.primary.place
                            , ad.details.existing,
                            ad.details.description,
                            ThreadSeeker.convertDateTimeToString(ad.birth, true)
                            , ThreadSeeker.convertDateTimeToString(ad.details.checkings[ad.details.checkings.Count - 1], true)
                            , ThreadSeeker.convertTimeSpanToString(ad.details.minimumLifeTime),
                            estlt
                            , ad.rating.realRatingOfAd,
                            ad.rating.estimatedRating,
                            "Сайт");
                        rowsAdded++;
                        #endregion
                    }
                    catch (Exception e)
                    { }
                    label6.Text = rowsAdded.ToString() + "(" + adsShowedCount.ToString() + ")";
                }
                label11.Text = cooladscount.ToString();
                dataGridView1.ResumeLayout();
                showSearchMetaData();
            }
            catch
            { }
            #endregion
        }
        void showSearchMetaData()
        {
            var info = popularInfoForSearched;
            try { textBox1.Text = info.popName + "(" + info.popamoutname + ")"; } catch { }
            try { textBox2.Text = info.popCategory + "(" + info.popamountcategory + ")"; } catch { }
            try { textBox3.Text = info.popPrice.ToString(); } catch { }
            try { textBox5.Text = info.popPhoto.ToString(); } catch { }
            try { textBox6.Text = info.popPlace + "(" + info.popamountplace + ")"; } catch { }
            try { textBox7.Text = info.popExisting.ToString() + "(" + info.popamountexisting + ")"; } catch { }
            try { textBox8.Text = info.popDescription + "(" + info.popamountdescription + ")"; } catch { }
            try { textBox9.Text = ThreadSeeker.convertDateTimeToString(info.popBirthDay, false) + "(" + info.popamountbirthday + ")"; } catch { }
            try { textBox11.Text = ThreadSeeker.convertDateTimeToString(info.popLastCheckDay, false) + "(" + info.popamountlastcheckday + ")"; } catch { }
            try { textBox12.Text = info.popRealRating.ToString(); } catch { }
            try { textBox13.Text = ThreadSeeker.convertTimeSpanToString(info.popLifeTime); } catch { }
            try { textBox14.Text = ThreadSeeker.convertTimeSpanToString(info.popEstLifeTime); } catch { }
            try { textBox16.Text = info.popEstPrice.ToString(); } catch { }
            try { textBox15.Text = info.popEstRating.ToString(); } catch { }
        }
        #endregion
        #region show and hide columns
        /*
             this.number,            this.name,            this.source,            this.category,
            this.cool,            this.price,            this.estprice,            this.photoAmount,
            this.place,            this.existing,            this.description,            this.birth,
            this.checking,            this.lifetime,            this.estlifetime,            this.realRating,
            this.estimatedRating,            this.Navigation
             */
        void do_hide(string columName, bool hide)
        {
            foreach (DataGridViewColumn v in dataGridView1.Columns)
            {
                if (v.Name == columName)
                {
                    v.Visible = hide;
                    break;
                }
            }
        }
        private void namecb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("name", cb.Checked);
        }
        private void categorycb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("category", cb.Checked);
        }
        private void pricecb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("price", cb.Checked);
        }
        private void placecb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("place", cb.Checked);
        }
        private void photocb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("photoAmount", cb.Checked);
        }
        private void lascheckcb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("checking", cb.Checked);
        }
        private void creationcb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("birth", cb.Checked);
        }
        private void lifetimecb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("lifetime", cb.Checked);
        }
        private void existingcb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("existing", cb.Checked);
        }
        private void descriptioncb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("description", cb.Checked);
        }
        private void realratingcb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("realRating", cb.Checked);
        }
        private void estlifetimecb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("estlifetime", cb.Checked);
        }
        private void estpricecb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("estprice", cb.Checked);
        }
        private void estratingcb_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            do_hide("estimatedRating", cb.Checked);
        }
        #endregion
        #region table interface
        SortedAds.AdToFind selectedAd = null;
        #region navigation
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedCells[0].OwningColumn.Name == "Navigation")
                {
                    string uri = "https://www.avito.ru/" + selectedAd.ad.primary.id;
                    Process.Start("chrome.exe", uri);
                    var dr = MessageBox.Show("Есть ли на еще на сайте это объявление?", "", MessageBoxButtons.YesNo);
                    if (dr == DialogResult.Yes)
                    {
                        dataGridView1.SelectedCells[0].OwningRow.Cells[9].Value = true;
                        bool was = selectedAd.ad.details.existing;
                        selectedAd.ad.details.existing = true;
                        selectedAd.ad.details.checkings.Add(DateTime.Now);
                        selectedAd.ad.details.renewminimumLifeTime(selectedAd.ad.birth);
                        if (was != true)
                            renewInfoInPattern();
                    }
                    if (dr == DialogResult.No)
                    {
                        dataGridView1.SelectedCells[0].OwningRow.Cells[9].Value = false;
                        bool was = selectedAd.ad.details.existing;
                        selectedAd.ad.details.existing = false;
                        if (was != false)
                            renewInfoInPattern();
                    }                  
                }
            }
            catch
            {

            }
        }
        #endregion
        #region select ad
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (activeSearch) return;
            selectAd();
            showPhoto();
        }
        void selectAd()
        {
            try
            {
                var cells = dataGridView1.SelectedCells[0].OwningRow.Cells;
                DataGridViewCell cell = null;
                foreach (DataGridViewCell v in cells) if (v.Value.ToString().Contains("(") && v.Value.ToString().Contains(")")) { cell = v;  break; }
                string[] ids = cell.Value.ToString().Split(new[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
                string id = ids[ids.Length - 1];
                foreach (var v in popularInfoForSearched.fitableAds)
                    if (v.ad.primary.id == id)
                    {
                        selectedAd = v;
                        break;
                    }
            }
            catch(Exception e)
            { }         
        }
        void showPhoto()
        {
            try
            {
                pictureBox1.Image = Image.FromFile(Ad.WritingAndReading.getAdPicPath(selectedAd.ad, false));
            }
            catch { }
        }
        #endregion
        #region change containments
      
        private void dataGridView1_CellLeave(object sender, DataGridViewCellEventArgs e)
        {            
            if (activeSearch) return;
            try
            {
                var cell = dataGridView1.SelectedCells[0];
                dataGridView1.PerformLayout();
                if (cell.OwningColumn.Name == "photoRating")
                {
                    changeUserRating(cell);
                    renewInfoInPattern();
                }
            }
            catch { }
        }
        void changeUserRating(DataGridViewCell cell)
        {
                string value = cell.Value.ToString();
                string res = new string(value.ToCharArray().ToList().FindAll(s => Char.IsDigit(s)).ToArray());
                dataGridView1.SelectedCells[0].Value = res;
                if (res == "") return;
                //selectedAd.ad.primary.UserPhotoRating = int.Parse(res);
                Ad.WritingAndReading.saveOrRenewAd(selectedAd.ad);
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (activeSearch) return;
            var cell = dataGridView1.SelectedCells[0];
            if (cell.OwningColumn.Name == "existing")
            {
                changeExisting(cell);
                renewInfoInPattern();
            }
        }
        void changeExisting(DataGridViewCell cell)
        {
            string value = cell.Value.ToString();
            bool existing = (bool)cell.Value;

            selectedAd.ad.details.existing = existing;
            Ad.WritingAndReading.saveOrRenewAd(selectedAd.ad);
        }

        /// <summary>
        /// if not performed after changing properties, avitoClass will overwrite new info
        /// </summary>
        void renewInfoInPattern()
        {            
            string cat = selectedAd.ad.primary.category;
            
            foreach (var v in pattern.categories)
            {
                if (v.name != cat) continue;
                if ((selectedAd.ad.rating.betterThanCategoryEstimationsStrictlyNot || selectedAd.ad.rating.betterThanPatternEstimationsStrictlyNot)
                    && selectedAd.ad.details.existing)
                {
                    //v.coolExistingAds[selectedAd.number].primary.UserPhotoRating = selectedAd.ad.primary.UserPhotoRating;
                    v.coolExistingAds[selectedAd.number].details.existing = selectedAd.ad.details.existing;
                }
                else
                {
                    if (selectedAd.ad.details.existing)
                    {
                        //v.existingAds[selectedAd.number].primary.UserPhotoRating = selectedAd.ad.primary.UserPhotoRating;
                        v.existingAds[selectedAd.number].details.existing = selectedAd.ad.details.existing;
                    }
                    else
                    {
                        //v.dissapearedAds[selectedAd.number].primary.UserPhotoRating = selectedAd.ad.primary.UserPhotoRating;
                        v.dissapearedAds[selectedAd.number].details.existing = selectedAd.ad.details.existing;
                    }
                }

                //if (findAd(ref v.coolExistingAds))
                //    break;
                //if (selectedAd.ad.details.existing)                
                //    if (findAd(ref v.existingAds))
                //        break;                
                //else
                //    if (findAd(ref v.dissapearedAds))
                //    break;
            }
        }
        bool findAd(ref List<Ad> ads)
        {
            foreach (var v in ads)
                if (v.primary.id == selectedAd.ad.primary.id)
                {
                    //v.primary.UserPhotoRating = selectedAd.ad.primary.UserPhotoRating;
                    v.details.existing = selectedAd.ad.details.existing;
                    return true;
                }
            return false;
        }

        private void dataGridView1_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            #region ints
            /*
            this.photoRating,
            this.price,
            this.estprice,
            this.photoAmount,        
            this.realRating,
            this.estimatedRating,
            this.rrminusminestr,
            this.rrminusmaxer,       
             */
            //Suppose your interested column has index 1/name price
            if (e.Column.Name == "price" || e.Column.Name == "photoRating" || e.Column.Name == "estprice"
                || e.Column.Name == "photoAmount" || e.Column.Name == "realRating" ||
                e.Column.Name == "estimatedRating" || e.Column.Name == "rrminusminestr"
                || e.Column.Name == "rrminusmaxer")
                try
                {
                    string[] ikommas1 = e.CellValue1.ToString().Split(new[] { "," }, StringSplitOptions.None);
                    string[] ikommas2 = e.CellValue2.ToString().Split(new[] { "," }, StringSplitOptions.None);
                    string ivalue1 = ikommas1[0];
                    string ivalue2 = ikommas2[0];
                    e.SortResult = int.Parse(ivalue1).CompareTo(int.Parse(ivalue2));
                    e.Handled = true;//pass by the default sorting
                }
                catch (Exception er)
                { }
            #endregion
            #region datetimes
            /*            
            this.birth,
            this.checking,      
             */
            if (e.Column.Name == "birth" || e.Column.Name == "checking")
                try
                {
                    string[] dtkommas1 = e.CellValue1.ToString().Split(new[] { "," }, StringSplitOptions.None);
                    string[] dtkommas2 = e.CellValue2.ToString().Split(new[] { "," }, StringSplitOptions.None);
                    string[] dtvalue1 = dtkommas1[0].Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    DateTime dtv1 = new DateTime(int.Parse(dtvalue1[0]), int.Parse(dtvalue1[1]), int.Parse(dtvalue1[2]),
                        int.Parse(dtvalue1[3]), int.Parse(dtvalue1[4]), 0);
                    string[] dtvalue2 = dtkommas2[0].Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    DateTime dtv2 = new DateTime(int.Parse(dtvalue2[0]), int.Parse(dtvalue2[1]), int.Parse(dtvalue2[2]),
                        int.Parse(dtvalue2[3]), int.Parse(dtvalue2[4]), 0);
                    e.SortResult = dtv1.CompareTo(dtv2);
                    e.Handled = true;//pass by the default sorting
                }
                catch (Exception er)
                { }
            #endregion
            #region timespans
            /*               
             * this.lifetime,
            this.estlifetime,
            this.lefttominestlifetime,
            this.lefttomaxestlifetime,
             */
            if (e.Column.Name == "lifetime" ||
                e.Column.Name == "estlifetime" ||
                e.Column.Name == "lefttominestlifetime"
                || e.Column.Name == "lefttomaxestlifetime")
                try
                {
                    string[] tskommas1 = e.CellValue1.ToString().Split(new[] { "," }, StringSplitOptions.None);
                    string[] tskommas2 = e.CellValue2.ToString().Split(new[] { "," }, StringSplitOptions.None);
                    string[] tsvalue1 = tskommas1[0].Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    TimeSpan tsv1 = new TimeSpan(int.Parse(tsvalue1[0]), int.Parse(tsvalue1[1]), int.Parse(tsvalue1[2]),
                        int.Parse(tsvalue1[3]), 0);
                    string[] tsvalue2 = tskommas2[0].Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    TimeSpan tsv2 = new TimeSpan(int.Parse(tsvalue2[0]), int.Parse(tsvalue2[1]), int.Parse(tsvalue2[2]),
                        int.Parse(tsvalue2[3]), 0);
                    e.SortResult = tsv1.CompareTo(tsv2);
                    e.Handled = true;//pass by the default sorting
                }
                catch (Exception er)
                { }
            #endregion
        }
        private void button3_Click(object sender, EventArgs e)
        {
            dataGridView1_CellValueChanged(sender, e as DataGridViewCellEventArgs);
            dataGridView1_CellLeave(sender, e as DataGridViewCellEventArgs);
        }
        #endregion

        #endregion
        #region mouse enters for pattern and category ads count texts
        #region pattern
        private void PatternAllAds_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                string message = string.Format("Существующих об.={0}\nИсчезнувших об.={1}",
                    patternExistingNonCoolAds.ToString(), patternPastAdsCount.ToString());
                toolTip1.Show(message, (Control)sender, 3000);
            }
            catch { }
        }

        private void PatternCoolAds_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                string message = string.Format("Лучших в запросе={0}\nЛучших в категории={1}\nСамых лучших={2}",
                    patternAdsBetterThanPattern.ToString(), patternAdsBetterThanCategory.ToString(), patternAdsBetterThanBoth.ToString());
                toolTip1.Show(message, (Control)sender, 3000);
            }
            catch { }
        }
        #endregion
        #region category
        private void CategoryAllAds_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                string message = string.Format("Существующих об.={0}\nИсчезнувших об.={1}",
                    categoryExistingNonCoolAds.ToString(), categoryPastAdsCount.ToString());
                toolTip1.Show(message, (Control)sender, 3000);
            }
            catch { }
        }

        private void CategoryCoolAds_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                string message = string.Format("Лучших в запросе={0}\nЛучших в категории={1}\nСамых лучших={2}",
                    categoryAdsBetterThanPattern.ToString(), categoryAdsBetterThanCategory.ToString(), categoryAdsBetterThanBoth.ToString());
                toolTip1.Show(message, (Control)sender, 3000);
            }
            catch { }
        }
        #endregion

        #endregion

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }
    }
}
