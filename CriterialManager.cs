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
    public partial class CriterialManager : Form
    {
        public CriterialManager(AvitoClass.Pattern Pattern)
        {
            InitializeComponent();
            pattern = Pattern;
        }
        public AvitoClass.Pattern pattern;

        public long comboPhotoRating = 0;
        public long patternAdsCount = 0;
        public long patternCoolAdsCount = 0;
        private void CriterialManager_Load(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < pattern.categories.Count; i++)
                {
                    comboBox3.Items.Add(pattern.categories[i].name);
                    patternAdsCount += pattern.categories[i].existingAds.Count;
                    patternAdsCount += pattern.categories[i].dissapearedAds.Count;
                    patternCoolAdsCount += pattern.categories[i].coolExistingAds.Count;
                }
                this.Text += " для " + pattern.name +
                    "(" + DateTime.Now.ToString(My.ThreadSeeker.dateTimeFormat) + ")";
            }
            catch (Exception re)
            { }
        }        
    }
}
