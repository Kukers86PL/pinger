﻿using System;
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
        private int DOT_SIZE = 200;
        private int INTERVAL = 1000;
        private Char SEPAROTOR = ';';
        private String CONFIG_FILE = "config.txt";
        private String RESULTS_FILE = "results.txt";
        private String last_check_date = "";
        private String VERSION = "v1.4";
        private BufferedGraphicsContext context;
        private BufferedGraphics grafx;

        struct Address_status
        {
            public String host;
            public Boolean isOnline;
            public String label;
            public long response_time;
        }

        private List<Address_status> addresses = new List<Address_status>();

        private Boolean isRunning = true;

        void init_config()
        {
            StreamWriter file = new StreamWriter(CONFIG_FILE, false);
            file.WriteLine(VERSION);
            file.WriteLine("# Check interval in miliseconds:");
            file.WriteLine("1000");
            file.WriteLine("# [Host label];[Host to ping [IPv4/IPv6/Name]]:");
            file.WriteLine("Google;www.google.com");
            file.WriteLine("DNS IPv4;8.8.8.8");
            file.WriteLine("DNS IPv6;2001:4860:4860::8888");
            file.WriteLine("# etc...");
            file.Close();
        }

        void init()
        {
            if (File.Exists(CONFIG_FILE) == false)
            {
                File.Create(CONFIG_FILE).Close();

                init_config();
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
                        if (VERSION != line)
                        {
                            file.Close();
                            init_config();
                            MessageBox.Show("Incompatible config file version. Config was reseted. Please rerun application.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                            Application.Exit();
                            return;
                        }
                        break;
                    case 2:
                        INTERVAL = Int32.Parse(line);
                        break;
                    default:
                        String[] subs = line.Split(SEPAROTOR);
                        if (subs.Length == 2)
                        {
                            Address_status temp;
                            temp.label = subs[0];
                            temp.host = subs[1];
                            temp.isOnline = false;
                            temp.response_time = -1;
                            addresses.Add(temp);
                        }
                    break;
                }
            }
            file.Close();
        }

        void check_thread(Object i)
        {
            Address_status temp = addresses[(int)i];
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = pingSender.Send(temp.host);
                if (reply.Status == IPStatus.Success)
                {
                    temp.isOnline = true;
                    temp.response_time = reply.RoundtripTime;
                }
                else
                {
                    temp.isOnline = false;
                    temp.response_time = -1;
                }
            }
            catch
            {
                temp.isOnline =  false;
            }
            addresses[(int)i] = temp;
        }

        void check_status()
        {
            last_check_date = DateTime.Now.ToString();
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < addresses.Count(); i++)
            {
                Thread temp = new Thread(new ParameterizedThreadStart(check_thread));
                temp.Start(i);
                threads.Add(temp);
            }
            for (int i = 0; i < threads.Count(); i++)
            {
                threads[i].Join();
            }
        }

        void store_data()
        {
            String line = last_check_date + SEPAROTOR;
            StreamWriter file = new StreamWriter(RESULTS_FILE, true);
            for (int i = 0; i < addresses.Count(); i++)
            {
                line += addresses[i].label + SEPAROTOR + addresses[i].host + SEPAROTOR;
                if (addresses[i].isOnline)
                {
                    line += addresses[i].response_time.ToString() + SEPAROTOR;
                }
                else
                {
                    line += "-1;";
                }
            }
            file.WriteLine(line);
            file.Close();
        }

        void run()
        {
            int count = INTERVAL;
            while (isRunning)
            {
                if (count >= INTERVAL)
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

            context = BufferedGraphicsManager.Current;
            context.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
            grafx = context.Allocate(this.CreateGraphics(), new Rectangle(0, 0, this.Width, this.Height));

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            Thread t = new Thread(run);
            t.Start();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            grafx.Graphics.FillRectangle(Brushes.Black, 0, 0, this.Width, this.Height);
            int number_of_dots = Math.Max(addresses.Count(), 1);
            DOT_SIZE = Math.Max(Math.Max(this.Width / number_of_dots, this.Height / number_of_dots) - 20, 200);
            this.Text = "Pinger v1.0: Last check date: " + last_check_date;
            SolidBrush redBrush = new SolidBrush(Color.Red);
            SolidBrush greenBrush = new SolidBrush(Color.Green);
            int columns = Math.Max(this.Width / DOT_SIZE, 1);
            int rows = Math.Max(this.Height / DOT_SIZE, 1);
            for (int i = 0; i < addresses.Count(); i++)
            {
                int x = (i % columns) * DOT_SIZE;
                int y = ((i / columns) % rows) * DOT_SIZE;

                if (addresses[i].isOnline)
                {
                    grafx.Graphics.FillEllipse(greenBrush, x, y, DOT_SIZE, DOT_SIZE);
                }
                else
                {
                    grafx.Graphics.FillEllipse(redBrush, x, y, DOT_SIZE, DOT_SIZE);
                }

                Font drawFont = new Font("Arial", DOT_SIZE / 10);
                SolidBrush drawBrush = new SolidBrush(Color.Black);

                StringFormat drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Center;

                string label = addresses[i].label + "\n" + addresses[i].response_time.ToString() + " ms";

                grafx.Graphics.DrawString(label, drawFont, drawBrush, x + DOT_SIZE / 2, y + DOT_SIZE / 2 - DOT_SIZE / 10, drawFormat);
            }
            grafx.Render(e.Graphics);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            isRunning = false;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            context.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
            if (grafx != null)
            {
                grafx.Dispose();
                grafx = null;
            }
            grafx = context.Allocate(this.CreateGraphics(), new Rectangle(0, 0, this.Width, this.Height));

            Invalidate();
        }
    }
}
