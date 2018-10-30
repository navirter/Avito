using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace Avito
{
    public partial class SearchSettings : Form
    {
        public SearchSettings()
        {
            InitializeComponent();
        }
        private void SearchSettings_Load(object sender, EventArgs e)
        {
            list.ReadInFile();
            renewShowLB1();
            this.FormClosing += new FormClosingEventHandler(close);
            try
            {
                textBox3.Text = File.ReadAllText(Directory.GetCurrentDirectory() + "\\settings\\whatToSerach.txt", Encoding.GetEncoding(1251));
            }
            catch
            { }
        }
        void close(object sender, EventArgs e)
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\settings");
            File.WriteAllText(Directory.GetCurrentDirectory() + "\\settings\\whatToSerach.txt", textBox3.Text, Encoding.GetEncoding(1251));
        }
        public class Unit
        {
            public Unit(UnitList Host)
            {
                host = Host;
            }
            public string pattern = "";
            public CategoryList categories = new CategoryList();
            public long currentCategory = 0;
            public long processedPages = 0;
            public class Category
            {               
                public string value;//в какой категории искать
                public string link;
                public Unit host;
                public Category(string Value, string Link,  Unit Host)
                {
                    value = Value;
                    this.link = Link;
                    host = Host;
                }
            }
            private bool categoryChanged = false;
            public bool CategoryChanged
            {
                get { return categoryChanged; }
                set { categoryChanged = value;
                    if (categoryChanged) try { host.saveRenewed(); }
                        catch 
                        { }
                    categoryChanged = false; }
            }
            public class CategoryList : List<Category>
            {
                public void AddAnywhere(Category category)
                {
                    this.Add(category);
                    category.host.CategoryChanged = true;
                }
                public void RemoveAnyWhere(Category category)
                {
                    foreach (var v in this)
                        if (v.value == category.value)
                        {
                            this.Remove(v);
                            break;
                        }
                    category.host.CategoryChanged = true;
                }
            }
            public UnitList host;
            //additional info
            public DateTime start;
            public long crashedAds;
            public long processedAds;
            //public AvitoClass.AvitoPatternInfo info;
            public long pagesCount;
            public long currentPage = 1;
            public string currentLink;
            public long dissapearedAds = 0;
        }
        public class UnitList : List<Unit>
        {
            public void AddAnywhere(Unit unit)
            {
                try
                {
                    this.Add(unit);
                    saveRenewed();
                }
                catch { }
            }
            public void RemoveAnywhere(Unit unit)
            {
                try
                {
                    string path = Directory.GetCurrentDirectory() + "\\data\\avito\\" + unit.pattern;
                    Directory.Delete(path, true);
                }
                catch { }
                this.Remove(unit);
                saveRenewed();

            }
            public void ReadInFile()
            {
                try
                {
                    this.Clear();
                    string file = Directory.GetCurrentDirectory() + "\\settings\\searchSettings.txt";
                    string text = File.ReadAllText(file).Replace("\r\n","");
                    string[] patterns = text.Replace("\r\n", "").Split(new string[] { "pattern=" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < patterns.Length; i++)
                    {
                        string[] categories = patterns[i].Split(new string[] { "category=" }, StringSplitOptions.RemoveEmptyEntries);
                        string pattern = categories[0].Trim();
                        Unit unit = new Unit(this);
                        unit.pattern = pattern;
                        for (int j = 1; j < categories.Length; j++)
                            try
                            {
                                string[] valueAndLink = categories[j].Split(new string[] { "link=" }, StringSplitOptions.RemoveEmptyEntries);
                                string value = valueAndLink[0].Trim();
                                string link = valueAndLink[1].Trim();
                                Unit.Category category = new Unit.Category(value, link, unit);
                                unit.categories.Add(category);
                            }
                            catch { }
                        this.Add(unit);
                    }
                }
                catch { }
            }
            public void saveRenewed()
            {
                try
                {
                    string folder = Directory.GetCurrentDirectory() + "\\settings";
                    Directory.CreateDirectory(folder);
                    string file = folder + "\\searchSettings.txt";
                    List<string> tosave = new List<string>();
                    for (int i = 0; i < this.Count; i++)
                    {
                        tosave.Add("pattern=" + this[i].pattern);
                        for (int j = 0; j < this[i].categories.Count; j++)
                        {
                            tosave.Add("category=" + this[i].categories[j].value);
                            tosave.Add("link=" + this[i].categories[j].link);
                        }
                    }
                    File.WriteAllLines(file, tosave.ToArray());
                }
                catch { }
            }
        }
        public UnitList list = new UnitList();

        void renewShowLB1()
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            foreach (Unit u in list)            
                listBox1.Items.Add(u.pattern);            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {                
                foreach (Unit u in list)
                    if (u.pattern == selectedUnit.pattern)
                        return;
                list.AddAnywhere(selectedUnit);
                renewShowLB1();
            }
            catch { }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите удалить поисковой запрос и собранные данные?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            try
            {
                foreach (Unit u in list)
                    if (u.pattern == selectedUnit.pattern)
                    {
                        list.RemoveAnywhere(selectedUnit);
                        break;
                    }
                renewShowLB1();
            }
            catch { }
        }

        public Unit selectedUnit;
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selected = listBox1.SelectedItem.ToString();
                foreach (Unit unit in list)                
                    if (unit.pattern == selected)
                    {
                        selectedUnit = unit;
                        break;
                    }
                textBox1.Text = selectedUnit.pattern;
                renewShowLB2();
            }
            catch { }
        }
        void renewShowLB2()
        {
            listBox2.Items.Clear();
            textBox4.Text = "";
            textBox2.Text = "";
            try
            {
                foreach (var v in selectedUnit.categories)
                    listBox2.Items.Add(v.value + "===" + v.link);
            }
            catch { }
        }

        public Unit.Category selectedCategory;
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string txt = listBox2.SelectedItem.ToString();
                string[] txts = txt.Split(new string[] { "===" }, StringSplitOptions.RemoveEmptyEntries);
                string name = txts[0];
                foreach (var v in selectedUnit.categories)
                {
                    if (name == v.value)
                    {
                        selectedCategory = v;
                        break;
                    }
                }
                textBox2.Text = selectedCategory.link;
                textBox4.Text = selectedCategory.value;
            }
            catch
            {

            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                selectedUnit = new Unit(list);
                selectedUnit.pattern = textBox1.Text;
                foreach (var v in list)
                    if (v.pattern == selectedUnit.pattern)
                    {
                        selectedUnit = v;
                        return;
                    }
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedUnit.pattern == "")
                    return;
                selectedCategory = new Unit.Category(textBox4.Text, textBox2.Text, selectedUnit);

                foreach (Unit.Category c in selectedUnit.categories)
                    if (c.value == selectedCategory.value)
                        return;
                selectedUnit.categories.AddAnywhere(selectedCategory);
                renewShowLB2();
            }
            catch
            {
                label9.Text = "Пожалуйста, убедитесь в соблюдении верного формата ссылки и настроек поиска";
                Application.DoEvents();
                if (AvitoClass.sleep(2000, false)) return;
                label9.Text = "";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {

                if (selectedUnit.pattern == "")
                    return;
                selectedCategory = new Unit.Category(textBox4.Text, textBox2.Text, selectedUnit);                                
                foreach (Unit.Category c in selectedUnit.categories)
                    if (c.value == selectedCategory.value)
                    {
                        selectedUnit.categories.RemoveAnyWhere(selectedCategory);
                        break;
                    }
                selectedCategory = null;
                renewShowLB2();
            }
            catch { }
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            char x = e.KeyChar;
            if (Char.IsDigit(x) == false && Char.IsControl(x) == false)
                e.Handled = true;
        }

        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            char x = e.KeyChar;
            if (Char.IsDigit(x) == false && Char.IsControl(x) == false)
                e.Handled = true;
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                string x = listBox2.SelectedItem.ToString();
                string[] xs = x.Split(new string[] { "===" }, StringSplitOptions.RemoveEmptyEntries);
                Clipboard.SetText(xs[1]);
            }
            catch
            {
                Clipboard.SetText(listBox2.SelectedItem.ToString());
            }
        }
    }
}
;