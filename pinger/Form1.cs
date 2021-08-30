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
        private int INTERVAL = 1000;
        private String CONFIG_FILE = "config.txt";
        private String RESULTS_FILE = "results.txt";

        struct Address_status
        {
            public String ip;
            public Boolean isOnline;
            public String label;
        }

        private List<Address_status> addresses = new List<Address_status>();

        private Boolean isRunning = true;

        void init()
        {
            if (File.Exists(CONFIG_FILE) == false)
            {
                File.Create(CONFIG_FILE).Close();

                StreamWriter file = new StreamWriter(CONFIG_FILE, false);
                file.WriteLine("# Dot size in pixels:");
                file.WriteLine("200");
                file.WriteLine("# Check interval in miliseconds:");
                file.WriteLine("1000");
                file.WriteLine("# [Host label];[Host to ping]:");
                file.WriteLine("Google;www.google.com");
                file.Close();
            }
            if (File.Exists(RESULTS_FILE) == false)
            {
                File.Create(RESULTS_FILE).Close();
            }
        }

        void read_config()
        {
            String line = "";
            System.IO.StreamReader file = new System.IO.StreamReader(CONFIG_FILE);
            addresses.Clear();
            int count = 0;
            while ((line = file.ReadLine()) != null)
            {
                if (line.Count() == 0)
                {
                    continue;
                }
                if (line[0] == '#')
                {
                    continue;
                }
                count++;
                switch (count)
                {
                    case 1:
                        DOT_SIZE = Int32.Parse(line);
                        break;
                    case 2:
                        INTERVAL = Int32.Parse(line);
                        break;
                    default:
                        String[] subs = line.Split(';');
                        if (subs.Length == 2)
                        {
                            Address_status temp;
                            temp.label = subs[0];
                            temp.ip = subs[1];
                            temp.isOnline = false;
                            addresses.Add(temp);
                        }
                    break;
                }
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
            StreamWriter file = new StreamWriter(RESULTS_FILE, true);
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
            int count = 0;
            while (isRunning)
            {
                if (count > INTERVAL)
                {
                    count = 0;

                    read_config();
                    check_status();
                    store_data();

                    Invalidate();
                }

                Thread.Sleep(1);
                count++;
            }            
        }

        public Form1()
        {
            InitializeComponent();

            init();

            read_config();

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

                Font drawFont = new Font("Arial", DOT_SIZE / 10);
                SolidBrush drawBrush = new SolidBrush(Color.Black);

                StringFormat drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Center;

                e.Graphics.DrawString(addresses[i].label, drawFont, drawBrush, i * DOT_SIZE + DOT_SIZE / 2, DOT_SIZE / 2 - DOT_SIZE / 10, drawFormat);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            isRunning = false;
        }
    }
}
