using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using My;

namespace Avito
{
    public partial class DownloadedExample : Form
    {
        public DownloadedExample(Ad Example)
        {
            example = Example;
            InitializeComponent();
        }
        Ad example;

        private void DownloadedExample_Load(object sender, EventArgs e)
        {
            this.Text += " " + DateTime.Now.ToString(ThreadSeeker.dateTimeFormat);
            if (example != null)
                initialiseAll(example);
            else
                this.Text = "Нет данных по последнему объевлению. Откройте меня позже.";
        }
        void initialiseAll(Ad ad)
        {
            getPhoto(ad);
            getPrimary(ad);
            getDetails(ad);
            getBirth(ad);
            getRating(ad);
        }
        void getPhoto(Ad ad)
        {
            try
            {
                pictureBox1.Image = Image.FromFile(Ad.WritingAndReading.getAdPicPath(ad, false));
            }
            catch { }
        }
        void getPrimary(Ad ad)
        {
            try
            {
                var p = ad.primary;
                textBox2.Text = p.pattern;
                textBox3.Text = p.category;
                textBox6.Text = p.source;
                textBox8.Text = p.name;
                textBox10.Text = p.id;
                textBox12.Text = p.place;
                textBox14.Text = p.price.ToString();
                textBox16.Text = p.amountPhotos.ToString();
                //textBox18.Text = p.UserPhotoRating.ToString();
            }
            catch { }
        }
        void getDetails(Ad ad)
        {
            try
            {
                var d = ad.details;
                textBox20.Text = d.minimumLifeTime.ToString(ThreadSeeker.timeSpanFormat);
                textBox22.Text = d.existing.ToString();
                foreach (var v in d.checkings)
                    listBox1.Items.Add(v.ToString(ThreadSeeker.dateTimeFormat));
                textBox25.Text = d.description;
            }
            catch { }
        }
        void getBirth(Ad ad)
        {
            try
            {
                var b = ad.birth;
                textBox29.Text = b.Month.ToString();
                textBox31.Text = b.Day.ToString();
                textBox27.Text = b.Hour.ToString();
                textBox33.Text = b.Minute.ToString();
            }
            catch { }
        }
        void getRating(Ad ad)
        {
            try
            {
                var r = ad.rating;
                textBox35.Text = r.realRatingOfAd.ToString();
                textBox37.Text = r.estimatedRating.ToString();
                textBox39.Text = r.estimatedPrice.ToString();
                textBox41.Text = r.estimatedMinimumLifeTime.ToString(ThreadSeeker.timeSpanFormat);
            }
            catch { }
        }
    }
}
