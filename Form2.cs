using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using My;
using System.Runtime.InteropServices;

namespace Avito
{
    public partial class Form2 : Form
    {
        #region outer search

        public Form2(bool testing, bool soundless, bool GoImmidiately, bool ohneAnschluss)
        {
            InitializeComponent();
            isTesting = testing;
            isSoundless = soundless;
            immidiateFullSearch = GoImmidiately;
            withoutInternet = ohneAnschluss;
            if (testing)
            {
                Text = "AllBetter режим отладки";
            }
            CheckForIllegalCrossThreadCalls = false;
            ThreadSeeker.ThreadSeekerColorisationSetting[] colosettings =
            {
                new ThreadSeeker.ThreadSeekerColorisationSetting("avito","Запрос обработан", System.Drawing.Color.Red),
                //new ThreadSeeker.ThreadSeekerColorisationSetting("avito","Обработана страница", System.Drawing.Color.Blue),
                new ThreadSeeker.ThreadSeekerColorisationSetting("getting statistics", ", исчезнувших=", System.Drawing.Color.Green),
                //new ThreadSeeker.ThreadSeekerColorisationSetting("avito", " пропавш. объявл.", System.Drawing.Color.Crimson),
                new ThreadSeeker.ThreadSeekerColorisationSetting("avito", " критериев", System.Drawing.Color.Crimson),
                //new ThreadSeeker.ThreadSeekerColorisationSetting("avito", " самых лучших объявлений", System.Drawing.Color.Gold)
                ////new ThreadSeeker.ThreadSeekerColorisationSetting("avito","", System.Drawing.Color.Blue),
                ////new ThreadSeeker.ThreadSeekerColorisationSetting("avito","", System.Drawing.Color.Brown),
                ////new ThreadSeeker.ThreadSeekerColorisationSetting("avito","", System.Drawing.Color.Aqua),
                ////new ThreadSeeker.ThreadSeekerColorisationSetting("avito","", System.Drawing.Color.DarkGreen),
                //new ThreadSeeker.ThreadSeekerColorisationSetting("avito","ошибка в обработке стриницы ", System.Drawing.Color.DeepPink)
            };
            int x = groupBox1.Location.X + groupBox1.Size.Width + 5;
            int y = groupBox1.Location.Y;  
            var g = new ThreadSeeker.Graphics(x, y,
                AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom);
            ThreadSeeker ts = new ThreadSeeker(colosettings, 15, 750, g, this);
            this.FormClosing += new FormClosingEventHandler(closing);
        }
        void closing(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        public bool immidiateFullSearch;
        public bool withoutInternet;
        SearchSettings.UnitList patterns = new SearchSettings.UnitList();
        public Sender senderell = null;
        public AvitoClass avito = new AvitoClass();
        private void Form2_Load(object sender, EventArgs e)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + "\\temp\\lookAtLog.txt"))
            {
                MessageBox.Show("Проверьте предыдущие логи на ошибки");
                File.Delete(Directory.GetCurrentDirectory() + "\\temp\\lookAtLog.txt");
            }
            try
            {
                string curdir = Directory.GetCurrentDirectory() + "\\settings\\timing.txt";
                textBox17.Lines = File.ReadAllLines(curdir);
            }
            catch { }
            this.Text += " запуск " + DateTime.Now.ToString(Form1.dateTimeFormat);
            if (isSoundless)
                this.Text += ". Режим без звука";
            if (withoutInternet) this.Text += ". Без интернета";
            this.FormClosing += new FormClosingEventHandler(close);
            patterns.ReadInFile();
            List<string> spatterns = new List<string>();
            patterns.ForEach(s => spatterns.Add(s.pattern));
            //spatterns.Sort();
            patternListBox.Items.AddRange(spatterns.ToArray());
            avito = new AvitoClass();
            Thread tavito = new Thread(avito.main);
            tavito.Name = "Avito.OneAfterOne";
            senderell = new Sender("Navirter", isTesting, textBox17,
                new MediaPlayer(), isSoundless, patternListBox, textBox19,
                 immidiateFullSearch, withoutInternet, button3, button5);
            tavito.Start(senderell);
            Thread ebay = new Thread(EbayClass.Searcher.oneByOne);
            ebay.SetApartmentState(ApartmentState.STA);
            ebay.Start(senderell);
            try { Directory.Delete(Directory.GetCurrentDirectory() + "\\temp", true); }
            catch 
            { }
            tryHideSettings();
            new Thread(autoUnpause).Start();
        }
        void tryHideSettings()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\settings");
                di.Attributes = FileAttributes.Hidden;
            }
            catch { }
        }




        public bool isTesting;
        public bool isSoundless;
        public class Sender
        {
            public string user = "";
            public TextBox OneRoundTime = null;
            public MediaPlayer mp = new MediaPlayer();
            public bool withoutSound = false;
            public ListBox lb = null;
            public TextBox Tb = null;
            public bool goFullSearch = false;
            public bool withoutInternet = false;
            public Button SettingsButton = null;
            public Button dataButton = null;
            public Sender(string user1,  bool test, TextBox oneroundtimetb, MediaPlayer MP, bool soundless, 
                ListBox listBox1, TextBox tb,  bool GoFullSearch,
                bool WithoutInternet, Button but3, Button DataButton )
            {
                user = user1;
                testing = test;
                OneRoundTime = oneroundtimetb;
                mp = MP;
                withoutSound = soundless;
                lb = listBox1;
                Tb = tb;
                goFullSearch = GoFullSearch;
                withoutInternet = WithoutInternet;
                SettingsButton = but3;
                dataButton = DataButton;
            }
            public bool testing;
        }
        void wait(object sender)
        {
            SearchSettings ss = sender as SearchSettings;
            while (ss.IsDisposed != true)
            {
                if (AvitoClass.sleep(500, false))return;
            }
            patterns.ReadInFile();
            List<string> spatterns = new List<string>();
            patterns.ForEach(s => spatterns.Add(s.pattern));
            patternListBox.Items.Clear();
            patternListBox.Items.AddRange(spatterns.ToArray());
        }
        public string selectedPattern = "";

        private void button3_Click(object sender, EventArgs e)
        {
            SearchSettings ss = new SearchSettings();
            ss.Show();
            Thread w = new Thread(wait);
            w.Start(ss);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer", Directory.GetCurrentDirectory() + "\\logs");
            }
            catch
            { }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "\\logs\\log.txt");
            }
            catch { }
        }
        private void patternListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string[] selects = patternListBox.SelectedItem.ToString().Split(new string[] { " Время: " }, StringSplitOptions.RemoveEmptyEntries);
                selectedPattern = selects[0];
                foreach (var v in avito.patterns)
                    if (v.name == selectedPattern)
                    {
                        avito.selectedPattern = v;
                        break;
                    }
                Application.DoEvents();
            }
            catch { }
        }

        public delegate Ad[] adstring(string[] s, int i, int i1);
        Ad[] getTenth(string[] files, int start, int end)
        {
            List<Ad> ads = new List<Ad>();
            for (int i = start; i < end; i++)
            {
                ads.Add(Ad.WritingAndReading.ReadFromXmlFile<Ad>(files[i]));
            }
            return ads.ToArray();
        }

        #endregion

        #region newWindows


        System.Drawing.Color b6color = new System.Drawing.Color();
        private void button6_Click(object sender, EventArgs e)
        {
            DownloadedExample de = new DownloadedExample(AvitoClass.lastAd);
            de.Show();
            button6.Text = "Образец скаченного объявления";
            button6.ForeColor = b6color;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (selectedPattern != "")
                try
                {
                    if (!avito.selectedPattern.dataIsComplete)
                    {
                        ThreadSeeker.addThread("данные не готовы", avito.selectedPattern + ".showing data", true, false, false);
                        return;
                    }
                    long sum = 0;
                    foreach (var v in avito.selectedPattern.categories)
                    { sum += v.dissapearedAds.Count; sum += v.existingAds.Count; if (sum != 0) break; }
                    if (sum == 0)
                    {
                        ThreadSeeker.addThread("Нет объявлений", avito.selectedPattern + ".showing data", true, false, false);
                        return;
                    }
                    AdsManagement am = new AdsManagement(ref avito.selectedPattern);
                    am.Show();
                }
                catch(Exception eb)
                { }
            else
                ThreadSeeker.addThread("Выберите запрос", avito.selectedPattern + ".showing data", true, false, false);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (selectedPattern != "")
                try
                {
                    long sum = 0;
                    //foreach (var v in avito.selectedPattern.averageData)
                        sum += avito.selectedPattern.averageData.typeValues.Count;
                    if (sum == 0)
                    {
                        ThreadSeeker.addThread("Нет критериев", avito.selectedPattern + ".showing data", true, false, false);
                        return;
                    }
                    CriterialManager am = new CriterialManager(avito.selectedPattern);
                    am.Show();
                }
                catch (Exception eb)
                { }            
        }
        public static bool closed = false;
        void close(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                //устанавливает флаг отмены события в истину
                e.Cancel = true;
                //спрашивает стоит ли завершится
                if (MessageBox.Show("Вы уверены что хотите закрыть программу?", "Выйти?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    closed = true;
                    //и после этого только завершается работа приложения                    
                    tryHideSettings();
                    try { senderell.mp.Close(); } catch { }
                    try
                    {
                        Directory.Delete(Directory.GetCurrentDirectory() + "\\temp", true);
                    }
                    catch { }
                    ThreadSeeker.addApplicationCloseMessage();
                    Process.GetCurrentProcess().Close();
                    Application.Exit();
                }
            }
        }
        #endregion
        
        void autoUnpause()
        {
            while (true)
                try
                {
                    if (AvitoClass.sleep(10000, false)) return;
                    var timespent = ThreadSeeker.getIdleTime();
                    if (timespent.TotalMinutes > 30)
                        ThreadSeeker.pause = false;
                }
                catch { }
        }
    }
}
;