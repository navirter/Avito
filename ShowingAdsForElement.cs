using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Avito
{
    public partial class ShowingAdsForElement : Form
    {
        public ShowingAdsForElement(Ad[] Ads, string element)
        {
            ads = Ads;
            this.Text = element;
            InitializeComponent();
            
        }
        public Ad[] ads;
        private void ShowingAdsForElement_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < ads.Length; i++)
            {
                listBox1.Items.Add(ads[i].primary.name);
            }
        }
                
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Visible = true;
            try
            {
                string uri = "https://www.avito.ru/" + ads[listBox1.SelectedIndex].primary.id;
                textBox1.Text = uri;
            }
            catch { }
            try
            {                
                pictureBox2.Image = Image.FromFile(Ad.WritingAndReading.getAdPicPath(ads[listBox1.SelectedIndex], false));
            }
            catch { }
            try
            {
                textBox6.Text = ads[listBox1.SelectedIndex].details.description;
                textBox2.Text = ads[listBox1.SelectedIndex].primary.name;
                textBox3.Text = ads[listBox1.SelectedIndex].primary.price.ToString();
                textBox4.Text = ads[listBox1.SelectedIndex].primary.category;
                textBox5.Text = ads[listBox1.SelectedIndex].primary.place;
                textBox7.Text = ads[listBox1.SelectedIndex].birth.ToString(Form1.dateTimeFormat);
                textBox8.Text = ads[listBox1.SelectedIndex].primary.amountPhotos.ToString();
                //textBox9.Text = ads[listBox1.SelectedIndex].primary.UserPhotoRating.ToString();
                textBox10.Text = ads[listBox1.SelectedIndex].rating.estimatedRating.ToString();
                textBox11.Text = ads[listBox1.SelectedIndex].rating.realRatingOfAd.ToString();
                textBox12.Text = ads[listBox1.SelectedIndex].details.existing.ToString();
                textBox13.Text = ads[listBox1.SelectedIndex].rating.estimatedPrice.ToString();
                textBox14.Text = ads[listBox1.SelectedIndex].details.checkings[ads[listBox1.SelectedIndex].details.checkings.Count - 1].ToString(Form1.dateTimeFormat);
                TimeSpan ts = ads[listBox1.SelectedIndex].details.minimumLifeTime;
                textBox15.Text = ts.Days.ToString() + "." + ts.Hours.ToString() + "." + ts.Minutes.ToString();
            string existing = "существует";
            if (ads[listBox1.SelectedIndex].details.existing == false)
                existing = "не существует";
            textBox12.Text = existing;
            if (existing == "существует")
                button1.Visible = true;
            else
                button1.Visible = false;
            }
            catch { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("chrome.exe", textBox1.Text);
            }
            catch { }
        }
    }
}
