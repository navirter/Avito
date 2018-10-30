using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net;
using System.Xml.Serialization;

namespace Avito
{
    public class Ad
    {
        public Primary primary = new Primary();
        public DateTime birth = new DateTime();
        public Details details = new Details();
        public Rating rating = new Rating();
        
        public class Primary
        {
            public string pattern = "";
            public string category = "";
            public string source = "";
            public string name = "";
            public string id = "";
            public string place = "";
            public int price = 0;
            public int amountPhotos = 0;
            [XmlIgnore]public Image mainPhoto;
            public bool photoDownloaded = false;
            //[XmlIgnore]private int userPhotoRating = 0;
            //public int UserPhotoRating { get { return userPhotoRating; }set { if (value < 0) value = 0;if (value > 5) value = 5; userPhotoRating = value; } }//0 = norating
        }
        public class Details
        {
            public TimeSpan minimumLifeTime = new TimeSpan();
            public void renewminimumLifeTime(DateTime birth)
            {
                try
                {
                        minimumLifeTime = checkings[checkings.Count - 1] - birth;
                    if (minimumLifeTime.TotalMinutes < 0)
                        minimumLifeTime = birth - checkings[checkings.Count - 1];
                }
                catch { }
            }            
            public List<DateTime> checkings = new List<DateTime>();
            public bool existing  = true;
            public string description = "";
            //public long views = 0;
            //public long viewsPerMinute;
            
           
            public Ad tryGetDescriptionAndRatingAndCheckings(Ad ad)
            {
                try
                {
                    string filePath = Ad.WritingAndReading.getAdInfoPath(ad);
                    if (File.Exists(filePath))
                    {
                        var last = Ad.WritingAndReading.ReadFromXmlFile<Ad>(filePath);
                        if (last != null)
                        {
                            if (last.details.description != "")
                                ad.details = last.details;
                            //ad.primary.UserPhotoRating = last.primary.UserPhotoRating;
                            ad.details.checkings = last.details.checkings;
                            return ad;
                        }
                    }
                    string uri = "http://www.avito.ru/" + ad.primary.id;
                    Pagedownloader.WebClient wc = new Pagedownloader.WebClient();
                    wc.downloadPage(uri, Encoding.UTF8, null);
                    if (AvitoClass.sleep(new Random().Next(3, 6), true)) return ad;
                    string txt = wc.htmlstring;
                    wc = null;
                    string[] descriptions = FinderInfoInPage.get_strings(txt, new string[] { "item-description\">", "item-view-socials" }, "itemprop=");
                    string description = "";
                    try
                    {
                        string desc = descriptions[1].Replace("</br>", " ").Replace("<br>", " ").Replace("br/>", " ").Replace("<br />", " ");
                        descriptions = desc.Split(new[] { "<p>", "</p>" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < descriptions.Length; i++)
                            if (descriptions[i].Contains("</div") == false && descriptions[i].Contains("<div") == false)
                                description += " " + descriptions[i] + " ";
                    }
                    catch
                    { }
                    description = description.ToLower();                    
                    char[] chars = description.ToCharArray();
                    description = "";
                    foreach (char ch in chars)
                        if (!Char.IsDigit(ch) && !Char.IsLetter(ch)) description += " ";
                        else description += ch;
                    ad.details.description = string.Join(" ", description.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)).Trim();
                }
                catch
                { }                     
                return ad;
            }
        }
        public class Rating
        {
            /*сначала расчитывать реальный рейтинг каждого элемента после его смерти поделив цену на время жизни
            затем подсчитать среднеий рейтинг на элемент
            затем отсортировать элементы для быстрого поиска
            на основе реального рейтинга высчитывать ожидаемый
            */
            public long realRatingOfAd = 0;//=price*1000/existingTime in minutes;
            public long estimatedRating = 0;
            public int estimatedPrice = 0;
            public TimeSpan estimatedMinimumLifeTime = new TimeSpan();            
            public List<Element> elements = new List<Element>();
            
            public bool betterThanPatternEstimationsStrictly = false;
            public bool betterThanPatternEstimationsStrictlyNot = false;
            public bool betterThanCategoryEstimationsStrictly = false;
            public bool betterThanCategoryEstimationsStrictlyNot = false;

            public class Element
            {
                public Element() { }
                public Element(string TypeOfValue, string Value, Ad Owner)
                {
                    typeOfValue = TypeOfValue;
                    value = Value;
                    owner = Owner;        
                }
                public string typeOfValue;
                public string value;        
                public long realRatingOfElement;
                public long estimatedRating;
                public long estimatedPrice;
                [XmlIgnore]public Ad owner;                
            }
            public void estimateThings(ref Ad ad, AvitoClass.Pattern.Category itsCategory)
            {
                ad.rating.getElements(ad);
                if (itsCategory.criteria == null || itsCategory.criteria.typeValues.Count == 0) return;
                //ad.rating.getRealRating(ad);
                var adElems = ad.rating.elements;
                var crits = itsCategory.criteria;
                for (int i = 0; i < adElems.Count; i++)
                    for (int j = 0; j < crits.typeValues.Count; j++)
                        if (adElems[i].typeOfValue == crits.typeValues[j].type &&
                            adElems[i].value == crits.typeValues[j].value)
                        {
                            adElems[i].estimatedRating = crits.typeValues[j].averageRealRating;
                            adElems[i].estimatedPrice = crits.typeValues[j].averagePrice;                           
                        }
                #region getEstimations
                long generalRating = 0;
                long generalPrice = 0;
                int count = 0;
                for (int i = 0; i < adElems.Count; i++)
                {
                    if (adElems[i].estimatedRating == 0)
                        continue;
                    count++;
                    generalRating += adElems[i].estimatedRating;
                    generalPrice += adElems[i].estimatedPrice;
                }
                long avrat = generalRating / count;
                int avprice = Convert.ToInt32(generalPrice / count);
                ad.rating.estimatedRating = avrat;
                ad.rating.estimatedPrice = avprice;
                int minutes = Convert.ToInt32(avprice * 1000 / avrat);
                ad.rating.estimatedMinimumLifeTime = new TimeSpan(0, minutes, 0);
                #endregion
                if (estimatedRating != 0)
                    calculateCoolStaff(itsCategory.owner.averageData, crits);
            }
            public void calculateCoolStaff(AvitoClass.Criteria patternCriteria, AvitoClass.Criteria categoryCriteria)
            {
                betterThanPatternEstimationsStrictlyNot = false;
                betterThanPatternEstimationsStrictly = false;
                betterThanCategoryEstimationsStrictlyNot = false;
                betterThanCategoryEstimationsStrictly = false;
                if (estimatedRating == 0 || estimatedPrice == 0 || estimatedMinimumLifeTime == new TimeSpan())
                    return;
                #region process pattern averages
                if (patternCriteria != null && patternCriteria.typeValues.Count > 0)
                {
                    bool patternAll = true;
                    if (estimatedRating >= patternCriteria.averageRealRating)
                        betterThanPatternEstimationsStrictlyNot = true;
                    else patternAll = false;
                    if (estimatedPrice <= patternCriteria.averagePrice)
                        betterThanPatternEstimationsStrictlyNot = true;
                    else patternAll = false;
                    if (estimatedMinimumLifeTime <= patternCriteria.averageminimumLifeTime)
                        betterThanPatternEstimationsStrictlyNot = true;
                    else patternAll = false;
                    if (patternAll) betterThanPatternEstimationsStrictly = true;
                }
                #endregion
                #region process category averages
                if (categoryCriteria != null && categoryCriteria.typeValues.Count > 0)
                {
                    bool categoryAll = true;
                    if (estimatedRating >= categoryCriteria.averageRealRating)
                        betterThanCategoryEstimationsStrictlyNot = true;
                    else categoryAll = false;
                    if (estimatedPrice <= categoryCriteria.averagePrice)
                        betterThanCategoryEstimationsStrictlyNot = true;
                    else categoryAll = false;
                    
                    if (estimatedMinimumLifeTime <= categoryCriteria.averageminimumLifeTime)//no mlt
                        betterThanCategoryEstimationsStrictlyNot = true;
                    else categoryAll = false;
                    if (categoryAll) betterThanCategoryEstimationsStrictly = true;
                }
                #endregion
            }

            public void getRealRating(Ad ad)
            {
                try
                {
                    ad.details.renewminimumLifeTime(ad.birth);
                    long miunte = Convert.ToInt32(ad.details.minimumLifeTime.TotalMinutes);
                    long preRating = ad.primary.price * 1000 / miunte;
                    if (preRating < 0) preRating *= -1;
                    //ad.details.viewsPerMinute = ad.details.views * 1000 / miunte;
                    //realRatingOfAd = preRating * ad.details.viewsPerMinute / 1000;
                    realRatingOfAd = preRating;
                }
                catch
                {
                }
            }
            public void getElements(Ad ad)
            {
                elements.Clear();
                var photo = new Ad.Rating.Element("primary.photos", ad.primary.amountPhotos.ToString(), ad);
                photo.realRatingOfElement = ad.rating.realRatingOfAd;
                photo.estimatedPrice = ad.primary.price;
                photo.estimatedRating = ad.rating.estimatedRating;
                elements.Add(photo);
                var names = ad.primary.name.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < names.Length; i++)
                {
                    var name = new Ad.Rating.Element("primary.name", names[i], ad );
                    name.realRatingOfElement = ad.rating.realRatingOfAd;
                    name.estimatedPrice = ad.primary.price;
                    name.estimatedRating = ad.rating.estimatedRating;
                    elements.Add(name);
                }
                var place = new Ad.Rating.Element("primary.place", ad.primary.place, ad );
                place.realRatingOfElement = ad.rating.realRatingOfAd;
                place.estimatedPrice = ad.primary.price;
                place.estimatedRating = ad.rating.estimatedRating;
                elements.Add(place);
                var descriptions = ad.details.description.Split(new string[] { " " },  StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < descriptions.Length; i++)
                {
                    var desc = new Element("details.description", descriptions[i], ad );
                    desc.realRatingOfElement = ad.rating.realRatingOfAd;
                    desc.estimatedPrice = ad.primary.price;
                    desc.estimatedRating = ad.rating.estimatedRating;
                    elements.Add(desc);
                }
                foreach (var v in ad.rating.elements)
                    v.owner = ad;
                elements = ad.rating.elements;
            }
        }
        public static class WritingAndReading
        {
            /// <summary>
            /// Writes the given object instance to an XML file.
            /// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
            /// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [XmlIgnore] attribute.</para>
            /// <para>Object type must have a parameterless constructor.</para>
            /// </summary>
            /// <typeparam name="T">The type of object being written to the file.</typeparam>
            /// <param name="filePath">The file path to write the object instance to.</param>
            /// <param name="objectToWrite">The object instance to write to the file.</param>
            /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
            public static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
            {
                TextWriter writer = null;
                try
                {
                    var serializer = new XmlSerializer(typeof(T));
                    writer = new StreamWriter(filePath, append);
                    serializer.Serialize(writer, objectToWrite);
                }
                finally
                {
                    if (writer != null)
                        writer.Close();
                }
            }
            public static void saveOrRenewAd(Ad ad)
            {
                string path = getAdInfoPath(ad);
                WriteToXmlFile(path, ad);
            }

            /// <summary>
            /// Reads an object instance from an XML file.
            /// <para>Object type must have a parameterless constructor.</para>
            /// </summary>
            /// <typeparam name="T">The type of object to read from the file.</typeparam>
            /// <param name="filePath">The file path to read the object instance from.</param>
            /// <returns>Returns a new instance of the object read from the XML file.</returns>
            public static T ReadFromXmlFile<T>(string filePath) where T : new()
            {
                TextReader reader = null;
                try
                {
                    var serializer = new XmlSerializer(typeof(T));
                    reader = new StreamReader(filePath);
                    return (T)serializer.Deserialize(reader);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }

            public static string getAdFolderPathAndCreate(Ad ad)
            {
                try
                {
                    string month = ad.birth.Month.ToString(); while (month.Length < 2) month = "0" + month;
                    string day = ad.birth.Day.ToString(); while (day.Length < 2) day = "0" + day;
                    string path = Directory.GetCurrentDirectory() + "\\data\\" + ad.primary.source + "\\"
                        + ad.primary.pattern + "\\" + ad.primary.category + "\\ads\\" + ad.birth.Year + "\\" +
                        month + "\\" + ad.birth.Day + "\\" + ad.primary.id;
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    return path;
                }
                catch { }
                return "";
            }
            public static string getAdInfoPath(Ad ad)
            {
                try
                {
                    string folderpath = getAdFolderPathAndCreate(ad);
                    string infoPath = folderpath + "\\info.txt";
                    return infoPath;
                }
                catch { return ""; }
            }
            public static string getAdPicPath(Ad ad, bool createFolder)
            {
                try
                {
                    string path = getAdFolderPathAndCreate(ad);
                    if (createFolder)
                        Directory.CreateDirectory(path);
                    string filepath = path += "\\mainPic.jpeg";
                    return filepath;
                }
                catch { }
                return "";
            }
            public static string getPhotoPath(Ad ad)
            {
                return getAdFolderPathAndCreate(ad) + "\\mainpic.jpeg";
            }
        }

        public Ad(string name1, string id1, string place1,  int price1
            , int amountPhotos1, string pattern1, string category1)
        {
            primary.pattern = pattern1;
            primary.name = name1;
            primary.id = id1;
            primary.place = place1;
            primary.price = price1;
            primary.amountPhotos = amountPhotos1;
            primary.category = category1;
        }
        public Ad() { }

        public void downloadPhotoIfNotExist(string sourceCodeOfAdCodeInSite)
        {
            string filepath = WritingAndReading.getAdPicPath(this, true);         
            if (File.Exists(filepath))
                return;
            WebClient wc = new WebClient();
            try
            {
                string[] imgs = FinderInfoInPage.get_strings(sourceCodeOfAdCodeInSite, new string[] { "src=\"", "class=" }, ".jpg");
                string uri = imgs[0].Split(new[] { "(//", ")\"\n" }, StringSplitOptions.RemoveEmptyEntries)[1];               
                wc.DownloadFile(uri,filepath);
                if (AvitoClass.sleep(new Random().Next(3, 6), true)) return;
            }
            catch(Exception e) { }
            finally { wc.Dispose(); }
        }        
        public void tryReadPhoto()
        {
            try
            {
                string path = WritingAndReading.getPhotoPath(this);
                primary.mainPhoto = Image.FromFile(path);
            }
            catch
            { }
        }
    }
}
