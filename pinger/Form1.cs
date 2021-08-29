using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;

namespace pinger
{
    public partial class Form1 : Form
    {
        private int DOT_SIZE = 300;

        struct Address_status
        {
            public String ip;
            public Boolean isOnline;
        }

        private List<Address_status> addresses = new List<Address_status>();

        private Boolean isRunning = true;

        void init()
        {
            if (File.Exists("config.txt") == false)
            {
                File.Create("config.txt").Close();
            }
            if (File.Exists("results.txt") == false)
            {
                File.Create("results.txt").Close();
            }
        }

        void read_config()
        {
            String line = "";
            System.IO.StreamReader file = new System.IO.StreamReader("config.txt");
            addresses.Clear();
            while ((line = file.ReadLine()) != null)
            {
                Address_status temp;
                temp.ip = line;
                temp.isOnline = false;
                addresses.Add(temp);
            }
            file.Close();
        }

        void check_status()
        {
            for (int i = 0; i < addresses.Count(); i++)
            {
                Address_status temp = addresses[i];
                try
                {
                    Ping pingSender = new Ping();
                    PingReply reply = pingSender.Send(temp.ip);

                    if (reply.Status == IPStatus.Success)
                    {
                        temp.isOnline = true;
                    }
                    else
                    {
                        temp.isOnline = false;
                    }
                }
                catch (IOException)
                {
                    temp.isOnline = false;
                }

                addresses[i] = temp;
            }
        }

        void store_data()
        {
            String line = DateTime.Now.ToString() + ";";
            StreamWriter file = new StreamWriter("results.txt", true);
            for (int i = 0; i < addresses.Count(); i++)
            {
                line += addresses[i].ip + ";";
                if (addresses[i].isOnline)
                {
                    line += "1;";
                }
                else
                {
                    line += "0;";
                }
            }
            file.WriteLine(line);
            file.Close();
        }

        void run()
        {
            while (isRunning)
            {
                read_config();
                check_status();
                store_data();
                Invalidate();
                Thread.Sleep(1000);
            }            
        }

        public Form1()
        {
            InitializeComponent();

            init();
            read_config();
            check_status();
            store_data();

            Thread t = new Thread(run);
            t.Start();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            this.Width = (DOT_SIZE + 20) * addresses.Count();
            this.Height = (DOT_SIZE + 40);
            SolidBrush redBrush = new SolidBrush(Color.Red);
            SolidBrush greenBrush = new SolidBrush(Color.Green);
            for (int i = 0; i < addresses.Count(); i++)
            {
                if (addresses[i].isOnline)
                {
                    e.Graphics.FillEllipse(greenBrush, i * DOT_SIZE, 0, DOT_SIZE, DOT_SIZE);
                }
                else
                {
                    e.Graphics.FillEllipse(redBrush, i * DOT_SIZE, 0, DOT_SIZE, DOT_SIZE);
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            isRunning = false;
        }
    }
}
