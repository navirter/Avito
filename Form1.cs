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


namespace Avito
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public const string dateTimeFormat = "yyyy.MM.dd.HH.mm.ss";
        public const string timeSpanFormat = "dd.hh.mm.ss";
        public static DateTime ApplicationStart = DateTime.Now;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormClosing += new FormClosingEventHandler(fc);
            this.Hide();   
        }

        void fc(object sender, FormClosingEventArgs e)
        {
            //при попытке закрытия программы пользователем
            if (e.CloseReason == CloseReason.UserClosing)
            {
                //устанавливает флаг отмены события в истину
                e.Cancel = true;
                //спрашивает стоит ли завершится
                if (MessageBox.Show("Вы уверены что хотите закрыть программу?", "Выйти?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    //и после этого только завершается работа приложения
                    Application.Exit();
                }
            }
            File.Delete("active_user.txt");
        }

        public class User
        {
            public string login="";
            public string password = "";
            public User (string log, string pas)
            {
                login = log;
                password = pas;                
            }
        }
        public User[] users = 
        {
        new User("xxx","yyy"),
        new User("www","vvv")        
        };
        public long x;
        private void button1_Click(object sender, EventArgs e)
        {
            bool avoid = false;
            for (int i = 0; i < users.Length; i++)
            {
                if (textBox2.Text.ToLower() == users[i].login)
                {
                    if (textBox4.Text == users[i].password)
                    {
                        Form2 f2 = new Form2(checkBox1.Checked, checkBox2.Checked,
                            checkBox3.Checked, checkBox4.Checked);
                        f2.Show();
                        avoid = true;
                        this.Hide();
                        break;
                    }
                }
            }
            if (avoid)
                return;
            MessageBox.Show("Неизвестная комбинация");
            textBox4.Text = "";
        }
        
    }
}
