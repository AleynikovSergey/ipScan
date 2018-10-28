using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.Configuration;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

namespace AVS1
{
    public partial class Form1 : Form
    {
        private Input _input = new Input();
        long time = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int x, y, w, z;
            System.Threading.ThreadPool.GetMinThreads(out x, out y);
            System.Threading.ThreadPool.SetMinThreads(1000, x);
            System.Threading.ThreadPool.GetMaxThreads(out w, out z);
            System.Threading.ThreadPool.SetMaxThreads(2000, z);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            table.Rows.Clear();
            _input._ipList.Clear();
            _input = new Input();
            string str = maskedTextBox1.Text.Replace(" ", string.Empty); //удаляем пробелы
            string[] strBegin = str.Split(','); //делим строку на 4 строки
            str = maskedTextBox2.Text.Replace(" ", string.Empty); //аналогично
            string[] strEnd = str.Split(',');
            bool flag = false;
            for (int i = 0; i < 4; ++i)
            {
                if (Int32.Parse(strBegin[i]) > 255 || Int32.Parse(strBegin[i]) < 0)
                {
                    MessageBox.Show("Неверно введен первый адрес");
                    flag = true;
                    break;
                }
                if (Int32.Parse(strEnd[i]) > 255 || Int32.Parse(strEnd[i]) < 0)
                {
                    MessageBox.Show("Неверно введен второй адрес");
                    flag = true;
                    break;
                }
            }
            if (flag == true)
                return;
                _input.InputAdress(strBegin, strEnd);
                progressBar1.Value = 0;
            progressBar1.Maximum = _input._ipList.Count * 2;
            Scan(_input._ipList);
        }

        Stopwatch stopWatch = new Stopwatch();
        private void Scan(List<IPAddress> list)
        {
            stopWatch = new Stopwatch();
            labelTimer.Text = "0";
            stopWatch.Start();
            timer1.Start();
            labelStatus.Text = "выполнение...";
            AutoResetEvent waiter = new AutoResetEvent(false);
            byte[] buffer = Encoding.ASCII.GetBytes("тестируем Ping"); //записываем в массив байт сконвертированную в байтовый массив строку для отправки в пинг запросе
            foreach (IPAddress ip in list)
            {
                table.Rows.Add();
                table.Rows[table.Rows.Count - 1].Cells[0].Value = ip.ToString();
            }

            long startIP = _input.ConvertBytes(list[0].GetAddressBytes());
            long endIP = _input.ConvertBytes(list[list.Count - 1].GetAddressBytes());         
           
            foreach (IPAddress ip in list)
            {                
                var task = new Task(() =>
                {
                    
                    string hostName = "...";
                    string str = ip.ToString();
                    try
                    {                        
                        IPHostEntry dns = Dns.GetHostEntry(ip);
                        hostName = dns.HostName;
                    }
                    catch
                    {
                        hostName = "-";
                    }

                    Invoke(new MethodInvoker(() =>
                    {
                        ++progressBar1.Value;
                    })); 
                    for (int i = 0; i < table.Rows.Count; ++i)
                        if (table.Rows[i].Cells[0].Value.ToString() == str)
                        {
                            table.Rows[i].Cells[1].Value = hostName;
                            table.Invalidate();
                        }
                    Invoke(new MethodInvoker(() =>
                    {
                        ++progressBar1.Value;
                    }));
                    Ping p = new Ping();
                    try
                    {
                        p_PingCompleted(ip, p.Send(ip, 1500));
                    }
                    catch
                    {
                        for (int i = 0; i < table.Rows.Count; ++i)
                            if (table.Rows[i].Cells[0].Value.ToString() == ip.ToString())
                                table.Rows[i].Cells[2].Value = "Ошибка";
                    }
                });
                task.Start();
            }           
        }

        private void p_PingCompleted(IPAddress ip, PingReply e)
        {
            if (e.Status == IPStatus.Success)
            {
                for(int i = 0; i < table.Rows.Count; ++i)
                    if(table.Rows[i].Cells[0].Value.ToString() == e.Address.ToString())
                        table.Rows[i].Cells[2].Value = "Доступен (" + e.RoundtripTime.ToString() + " мс)" + e.Address.ToString();
            }
            else
            {
                for (int i = 0; i < table.Rows.Count; ++i)
                    if (table.Rows[i].Cells[0].Value.ToString() == ip.ToString())
                        table.Rows[i].Cells[2].Value = "Не доступен";
            }
            //((AutoResetEvent)e.UserState).Set();
        }

        private void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Reply.Status == IPStatus.Success)
            {
                for (int i = 0; i < table.Rows.Count; ++i)
                    if (table.Rows[i].Cells[0].Value.ToString() == e.Reply.Address.ToString())
                        table.Rows[i].Cells[2].Value = "Доступен (" + e.Reply.RoundtripTime.ToString() + " мс)" + e.Reply.Address.ToString();
            }
            else
            {

            }
            //((AutoResetEvent)e.UserState).Set();
        }

        private void analyze_Click(object sender, EventArgs e)
        {
            IPAddress mask = CalcMask(IPAddress.Parse(table.SelectedCells[0].Value.ToString()), IPAddress.Parse(table.SelectedCells[table.SelectedCells.Count - 3].Value.ToString()));
            label8.Text = mask.ToString();

            IPAddress netAddress = CalcNetAddress(IPAddress.Parse(table.SelectedCells[0].Value.ToString()), mask);
            label10.Text = netAddress.ToString();

            IPAddress broadcastAddress = CalcBrodcastAddress(netAddress, mask);
            label9.Text = broadcastAddress.ToString();

            IPAddress gateAddress = CalsGateAddress(netAddress, mask);
            label11.Text = gateAddress.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            table.Rows.Clear();
            _input._ipList.Clear();
            progressBar1.Value = 0;
        }

        private IPAddress CalcMask(IPAddress IPstart, IPAddress IPend)
        {
            byte[] mask = new byte[4];

            for (int i = 0; i <= 3; i++)
            {
                if (IPstart.GetAddressBytes()[i] == IPend.GetAddressBytes()[i])
                {
                    mask[i] = 255;
                }
                else
                {
                    string currentStartByteBits = Convert.ToString(IPstart.GetAddressBytes()[i], 2).PadLeft(8, '0');
                    string currentEndByteBits = Convert.ToString(IPend.GetAddressBytes()[i], 2).PadLeft(8, '0');
                    StringBuilder oneByte = new StringBuilder("00000000");
                    for (int j = 0; j <= 7; j++)
                    {
                        if (currentStartByteBits[j] == currentEndByteBits[j])
                        {
                            oneByte[j] = '1';
                            mask[i] = Convert.ToByte(oneByte.ToString(), 2);
                        }
                        else
                        {
                            return new IPAddress(mask);
                        }
                    }

                }
            }
            return new IPAddress(mask);
        }

        private IPAddress CalcNetAddress(IPAddress ip, IPAddress mask)
        {
            byte[] netAddress = new byte[4];

            for (int i = 0; i <= 3; i++)
            {
                string maskbyte = Convert.ToString(mask.GetAddressBytes()[i], 2).PadLeft(8, '0');
                string ipbyte = Convert.ToString(ip.GetAddressBytes()[i], 2).PadLeft(8, '0');
                string networkbyte = "";
                for (int j = 0; j < 8; j++)
                {
                    networkbyte += (maskbyte[j] == ipbyte[j] && ipbyte[j] == '1') ? '1' : '0';
                }
                netAddress[i] = Convert.ToByte(networkbyte, 2);
            }
            return new IPAddress(netAddress);
        }

        private IPAddress CalsGateAddress(IPAddress mask, IPAddress netAddress)
        {
            byte[] maskByte = mask.GetAddressBytes();
            byte[] netAddressByte = netAddress.GetAddressBytes();

            int[] result = new int[4];
            for (int i = 0; i < 4; ++i)
            {
                result[i] = (int)maskByte[i] & (int)netAddressByte[i];
            }
            byte[] gateAddress = _input.ConvertLong(_input.ConvertBytes(result) + 1);
            return new IPAddress(gateAddress);
        }

        private IPAddress CalcBrodcastAddress(IPAddress netAddress, IPAddress mask)
        {
            byte[] broadcastAddress = new byte[4];

            for (int i = 0; i <= 3; i++)
            {
                string maskbyte = Convert.ToString(mask.GetAddressBytes()[i], 2).PadLeft(8, '0');
                string networkbyte = Convert.ToString(netAddress.GetAddressBytes()[i], 2).PadLeft(8, '0');
                string broadcastbyte = "";
                for (int j = 0; j < 8; j++)
                {
                    broadcastbyte += (networkbyte[j] == '1' || maskbyte[j] == '0') ? '1' : '0';
                }
                broadcastAddress[i] = Convert.ToByte(broadcastbyte, 2);
            }
            return new IPAddress(broadcastAddress);
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            
            Invoke(new MethodInvoker(() =>
            {
                if (progressBar1.Value == progressBar1.Maximum)
                {
                    stopWatch.Stop();
                    timer1.Stop();
                    this.Cursor = Cursors.Default;
                    table.Cursor = Cursors.Default;
                    labelStatus.Text = "завершено";
                    progressBar1.Value = 0;
                }
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}.{1:00}",
                                                    ts.Seconds,
                                                    ts.Milliseconds / 10);
                labelTimer.Text = elapsedTime;
            }));
        }
    }

    public class Input
    {
        int[] _begin = new int[4];
        int[] _end = new int[4];
        long _lbegin = 0;
        long _lend = 0;
        public List<IPAddress> _ipList = new List<IPAddress>();

        public Input()
        {
            for (int i = 0; i <= 3; ++i)
            {
                _begin[i] = 0;
                _end[i] = 0;
            }
        }

        public void InputAdress(string[] begin, string[] end)
        {
            for (int i = 0; i < begin.Length; ++i)
            {
                _begin[i] = Byte.Parse(begin[i]);
                _end[i] = Byte.Parse(end[i]);
            }
            Initialize();
        }

        public void Initialize() 
        {
            _lbegin = ConvertBytes(_begin);
            _lend = ConvertBytes(_end);
            for (int i = 0; i <= (_lend - _lbegin); ++i)
                _ipList.Add(new IPAddress(ConvertLong(_lbegin + i)));
        }

        public long ConvertBytes(int[] bytes)
        {
            return (bytes[0] * (long)Math.Pow(256, 3) + bytes[1] * (long)Math.Pow(256, 2) + bytes[2] * (long)Math.Pow(256, 1) + bytes[3]);
        }

        public long ConvertBytes(byte[] bytes)
        {
            return (bytes[0] * (long)Math.Pow(256, 3) + bytes[1] * (long)Math.Pow(256, 2) + bytes[2] * (long)Math.Pow(256, 1) + bytes[3]);
        }

        public byte[] ConvertLong(long ipAddress)
        {
            byte[] bytes = new byte[4];
            int k = 3;
            while (ipAddress > 0)
            {
                bytes[k--] = (byte)(ipAddress % 256);
                ipAddress = ipAddress / 256;
            }
            return bytes;
        }
    }

}
