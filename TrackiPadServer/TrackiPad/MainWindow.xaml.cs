﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cursor = System.Windows.Input.Cursor;
using MessageBox = System.Windows.MessageBox;

namespace TrackiPad
{
    
    public partial class MainWindow : Window
    {

        private TcpWorker tcp = new TcpWorker();
        List<ITrackCommandHandler> trackCommandHandlers = new List<ITrackCommandHandler>();
        private bool isCon = false;
 

        public MainWindow()
        { 
            trackCommandHandlers.Add(new KeyboardTrackCommandHandler());
            trackCommandHandlers.Add(new PressTrackCommandHandler());
            trackCommandHandlers.Add(new MoveTrackCommandHandler());
            trackCommandHandlers.Add(new ScrollTrackCommandHandler());
            trackCommandHandlers.Add(new LongPressTrackCommandHandler());
            trackCommandHandlers.Add(new RotateVolumeTrackCommandHandler());
            trackCommandHandlers.Add(new SenseTrackCommandHandler());
            trackCommandHandlers.Add(new PinchTrackCommandHandler());
            InitializeComponent();
            tcp.OnRecieve += (name) =>
            {

                if (name != "Сервер")
                {

                    Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(
                            delegate()
                            {
                                while (tcp.CommandQueue.Count > 0)
                                {
                                    string command = tcp.CommandQueue.Dequeue();
                                    string mode = command.Substring(0, 2);

                                    string[] args = command.Split(new[] {"_"}, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (ITrackCommandHandler handler in trackCommandHandlers)
                                        handler.Execute(new WindowInteropHelper(this).Handle, args);


                                }

                                //foreach (ITrackCommandHandler handler in trackCommandHandlers)
                                //    handler.Execute(new WindowInteropHelper(this).Handle, new[] {"e"});
                            }
                            ));

                }
                else
                {
                    image2.Dispatcher.Invoke(
                       System.Windows.Threading.DispatcherPriority.Normal,
                       new Action(
                           delegate ()
                           {
                               image2.Source = new BitmapImage(new Uri("cr.png", UriKind.RelativeOrAbsolute));

                           }));

                    image.Dispatcher.Invoke(
                       System.Windows.Threading.DispatcherPriority.Normal,
                       new Action(
                           delegate ()
                           {
                          //     image.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500)));

                           }));
                }
               

            };

        }


 

        private void image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isCon) return;
            image.Source = new BitmapImage(new Uri("button_p.png", UriKind.RelativeOrAbsolute));
        }

        private void image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isCon) return;
       
            image.Source = new BitmapImage(new Uri("button_r.png", UriKind.RelativeOrAbsolute));
            tcp.Start(Convert.ToInt32("29330"));

            string ip = TcpWorker.GetIpAddress();
            string[] stbytes = ip.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            byte[] bytes = { (Convert.ToByte(stbytes[3])), (Convert.ToByte(stbytes[2])), (Convert.ToByte(stbytes[1])), (Convert.ToByte(stbytes[0])) };

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            int i = BitConverter.ToInt32(bytes, 0);
            label_Copy.Content = "" + i;
            isCon = true;
            label_Copy.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500)));
            image2.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500)));
        }
    }


    class TcpWorker
    {
        public static bool IsStart = false;
        public static bool IsConnected = false;
        public static string LastMode = "e";
        Thread ListenerThread, SocketThread;
        TcpListener listener;
        TcpAuthClient client;
        List<TcpAuthClient> lst = new List<TcpAuthClient>();
        byte[] myReadBuffer = new byte[8128];
        int isBeginTransfer = 0;
        int x = 0, y = 0, w = 0, h = 0;
        private NetworkStream netStream;
        public Queue<string> CommandQueue = new Queue<string>();

        public delegate void Recieve(string name);
    
        public Recieve OnRecieve;
        //  Bitmap bitmap;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetIpAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start(int port)
        {
            listener = new TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
             
            SocketThread = new Thread(AcceptClient);
            SocketThread.IsBackground = true;
            SocketThread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        void AcceptClient()
        {
            while (true)
            {

                // SetText1("Поиск...");
                TcpClient tcpc = listener.AcceptTcpClient();
                IsConnected = true;
                client = new TcpAuthClient(tcpc, TcpAuthClient.GetName());
                lst.Add(new TcpAuthClient(client));
                CommandQueue.Enqueue("Соединение с " + lst[lst.Count - 1].Name + " установлено");
                OnRecieve?.Invoke("Сервер");
                //  SetText1();
                //client = null;
                ParameterizedThreadStart p1 = new ParameterizedThreadStart(DoListen);
                ListenerThread = new Thread(p1);
                ListenerThread.IsBackground = true;
                ListenerThread.Start(client);

                // Получение адреса клиента
                //MessageBox.Show(IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()).ToString());

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client2"></param>
        void DoListen(object client2)
        {
            TcpAuthClient innerClient = (TcpAuthClient)client2;
            netStream = innerClient.Tcp.GetStream();
            while (true)
            {

                if (netStream.CanRead)
                {

                    int numberOfBytesRead = 0;
                    string str1 = "", str2 = "";

                    try
                    {
                        str1 = innerClient.Name;
                        numberOfBytesRead = netStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                        str2 += Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead);
                        if (str2.Length > 0)
                        {

                            foreach (string s in str2.Split(new[] {"_e"}, StringSplitOptions.RemoveEmptyEntries))
                            {
                                CommandQueue.Enqueue(s);
                            }

                            OnRecieve?.Invoke(str1);
                            //MessageBox.Show("str2");
                        }//SetText2(str1 + ": " + str2);

                        
                        myReadBuffer = new byte[8128];

                    }
                    catch (Exception e)
                    {
                        //SetText1(str1 + " отключился от сервера");
                        //lst.Remove(client);
                        break;
                    }

                }
                else
                {
                    MessageBox.Show("Can`t read!!!");
                }

            }
            netStream.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        public TcpWorker()
        {
            IsStart = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostString"></param>
        public static void GetIpAddressList(String hostString)
        {
            try
            {
                // Get 'IPHostEntry' object containing information like host name, IP addresses, aliases for a host.
                IPHostEntry hostInfo = Dns.GetHostByName(hostString);
                Console.WriteLine("Host name : " + hostInfo.HostName);
                Console.WriteLine("IP address List : ");
                for (int index = 0; index < hostInfo.AddressList.Length; index++)
                {
                    MessageBox.Show(hostInfo.AddressList[index].ToString());
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        private void Send(TcpClient client, string msg = "")
        {
            netStream = client.GetStream();

            if (netStream.CanWrite)
            {
                byte[] buf;
                if (msg.Length == 0) buf = Encoding.UTF8.GetBytes(msg);
                else buf = Encoding.UTF8.GetBytes(msg);
                netStream.Write(buf, 0, buf.Length);
             //   netStream.Flush();
            }
            else
            {
                //SetText1("Error!!!");
            }
        }

        public void SendToAll(string msg)
        {
            foreach (TcpAuthClient authClient in lst)
            {
                Send(authClient.Tcp, msg);
            }
        }
  

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        string BufferToMessage(string[] buffer)
        {
            string ret = "speechbuffer";
            for (int i = 0; i < buffer.Length; i++) ret += "_" + buffer[i];
            return ret;
        }

    }

    class TcpAuthClient
    {
        private TcpClient tcp;
        private string name;
        private static int client = 0;
        private string info;

        public string Info
        {
            get { return info; }
            set { info = value; }
        }

        public TcpClient Tcp
        {
            get { return tcp; }
            set { tcp = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public TcpAuthClient(TcpClient tcp, string name)
        {
            this.tcp = tcp;
            this.name = name;
            this.Info = "";
        }

        public TcpAuthClient(TcpAuthClient ctpaClient)
        {
            this.name = ctpaClient.Name;
            this.tcp = ctpaClient.Tcp;
            this.Info = "";
        }

        public static string GetName()
        {
            client++;
            return "tcpclient" + client;

        }



    }


    interface ITrackCommandHandler
    {
        bool Execute(IntPtr handle, string[] args);
    }




    class PressTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "p";

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command)
            {
                InputEmulation.Press(new[] {MouseEventFlags.LEFTDOWN, MouseEventFlags.LEFTUP});
                TcpWorker.LastMode = args[0];
                return true;
            }
            return false;
        }
    }

    class SenseTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "s";

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command)
            {
                InputEmulation.Sensitivity = Convert.ToDouble(args[1].Replace(".", ","));
                return true;
            }
            return false;
        }
    }

    class LongPressTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "l";
        private bool isLong = false;

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command && !isLong)
            {
                isLong = true;
                InputEmulation.Press(new[] { MouseEventFlags.RIGHTDOWN, MouseEventFlags.RIGHTUP });
                return true;
            }
            if (args[0] != command) isLong = false;
            return false;
        }
    }

    class RotateVolumeTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "v";
        private int count = 0;
        private double delta = 0;

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command)
            {
                if (count > Convert.ToInt32(10*(1 - InputEmulation.Sensitivity)))
                {
                    count = 0;
                    double d = Convert.ToDouble(args[1].Replace(".", ","));
                   double result = delta - d;
                    if (result > 0) InputEmulation.VolDown(handle);
                    else InputEmulation.VolUp(handle);
                    delta = d;
                }
                 count ++;
                return true;
            }
            count = 0;
            delta = 0;

            return false;
        }
    }

    class MoveTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "m";
        private int count = 0;
        private double x = -1, y = -1;
        private bool isPress = false;

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command)
            {
                if (count > 0)
                {
                    if (!isPress && TcpWorker.LastMode == "p")
                    {
                        TcpWorker.LastMode = args[0];
                        //InputEmulation.Press(new[] { MouseEventFlags.LEFTDOWN, });
                        isPress = true;
                    }

                    double cx = Convert.ToDouble(args[1].Replace(".", ","));
                    double cy = Convert.ToDouble(args[2].Replace(".", ","));

                    if (x == -1 && y == -1)
                    {
                        x = cx;
                        y = cy;
                    }


                    else if(x != cx || y != cy)
                    {
                        double dx = (x - cx) * InputEmulation.Sensitivity*2.1;
                        double dy = (y - cy)*InputEmulation.Sensitivity*2.1;
                        int ix = Convert.ToInt32(dx);
                        int iy = Convert.ToInt32(dy);
                        System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
                        System.Windows.Forms.Cursor.Position = new System.Drawing.Point(p.X - ix, p.Y - iy);
                        x = cx;
                        y = cy;
                    }

                }
              else  count++;
                return true;
            }

            if (isPress)
            {
                isPress = false;
              //  InputEmulation.Press(new[] {MouseEventFlags.LEFTUP, });
            }
            count = 0;
            x = -1;
            y = -1;

            return false;
        }
    }

    class ScrollTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "c";
        private int count = 0;
        private double x = -1, y = -1;

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command)
            {
                if (count > 0)
                {
                    double cx = Convert.ToDouble(args[1].Replace(".", ","));
                    double cy = Convert.ToDouble(args[2].Replace(".", ","));

                    if (x == -1 && y == -1)
                    {
                        x = cx;
                        y = cy;
                    }


                    else if (x != cx || y != cy)
                    {
                        double dx = (x - cx) * InputEmulation.Sensitivity * 2.1;
                        double dy = (y - cy) * InputEmulation.Sensitivity * 2.1;
                        int ix = Convert.ToInt32(dx);
                        int iy = Convert.ToInt32(dy);
                       // System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
                       // System.Windows.Forms.Cursor.Position = new System.Drawing.Point(p.X - ix, p.Y - iy);

                        if (dy > 20 || dy < -20)
                        {
                            InputEmulation.Press(new[] {MouseEventFlags.WHEEL }, dy > 0? 60: -60);
                            x = cx;
                            y = cy;
                        }


                        
                    }

                }
                else count++;
                return true;
            }
            count = 0;
            x = -1;
            y = -1;

            return false;
        }
    }

    class PinchTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "i";
        private int count = 10000;
        private double scale = -1;

        public bool Execute(IntPtr handle, string[] args)
        {
            if (args[0] == command)
            {
                if (count > 3)
                {
                    count = 0;
                    double cscale = Convert.ToDouble(args[1].Replace(".", ","));

                    if (cscale == -1)
                        scale = cscale;
                    


                    else if (scale != cscale)
                    {
                        double dscale = (scale - cscale)*InputEmulation.Sensitivity*2.1;
                        int iscale = Convert.ToInt32(dscale);
                        // System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
                        // System.Windows.Forms.Cursor.Position = new System.Drawing.Point(p.X - ix, p.Y - iy);
                       
                            InputEmulation.KeyDown(KeyboardImulation.VK_LCONTROL);
                            InputEmulation.Press(new[] {MouseEventFlags.WHEEL, }, -Convert.ToInt32(dscale/Math.Abs(dscale))*3);
                            InputEmulation.KeyUp(KeyboardImulation.VK_LCONTROL);
                            scale = cscale;
                        
                    
                }


                }
                else count++;
                return true;
            }
            count = 0;
            scale = -1;

            return false;
        }
    }

    class KeyboardTrackCommandHandler : ITrackCommandHandler
    {

        private string command = "k";

        public bool Execute(IntPtr handle, string[] args)
        {

           

            if (args[0] == command)
            {
                char c = args[1][args[1].IndexOf("\"") + 1];

                if ("abcdefghijklmnopqrstuvwxyz".Contains(c))
                    InputLanguage.CurrentInputLanguage = KeyboardImulation.GetInputLanguageByName("eng");
                         

                else if ("абвгдеёжзийклмнопрстуфхцчшщъыьэюя".Contains(c))
                    InputLanguage.CurrentInputLanguage = KeyboardImulation.GetInputLanguageByName("ru");

                KeyboardImulation.Print(c + "");
                return true;
            }
            return false;
        }
    }





    /// <summary>
    /// Флаги кодов событий мыши
    /// </summary>
    [Flags]
    public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            WHEEL = 0x0800
        }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Red;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Green;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public UInt16[] Blue;
    }

    /// <summary>
    /// Эмуляция пользовательского ввода
    /// </summary>
    public static class InputEmulation
    {

        public static double Sensitivity = .5;

        const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        const int APPCOMMAND_VOLUME_UP = 0xA0000;
        const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        const int WM_APPCOMMAND = 0x319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);



        [DllImport("gdi32.dll")]
        public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        static RAMP ramp = new RAMP();

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);


        public static void SetGamma(double gamma)
        {
            ramp.Red = new ushort[256];
            ramp.Green = new ushort[256];
            ramp.Blue = new ushort[256];

            for (int i = 1; i < 256; i++)
            {
                // gamma is a value between 3 and 44
                ramp.Red[i] = ramp.Green[i] = ramp.Blue[i] = (ushort)(Math.Min(65535, Math.Max(0, Math.Pow((i + 1) / 256.0, gamma * 0.1) * 65535 + 0.5)));
            }
            SetDeviceGammaRamp(GetDC(IntPtr.Zero), ref ramp);
        }




        public static void Mute(IntPtr handle)
        {
            SendMessageW(handle, WM_APPCOMMAND, handle,
                (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }

        public static void VolDown(IntPtr handle)
        {
            SendMessageW(handle, WM_APPCOMMAND, handle,
                (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        public static void VolUp(IntPtr handle)
        {
            SendMessageW(handle, WM_APPCOMMAND, handle,
                (IntPtr)APPCOMMAND_VOLUME_UP);
        }


        //public static InputSimulator Simulator = new InputSimulator();
        [DllImport("user32.dll")]
            public static extern bool GetAsyncKeyState(Keys vKey);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

            /// <summary>
            /// Нажать мышей
            /// </summary>
            /// <param name="list">список нажатий</param>
            /// <param name="data"></param>
            public static void Press(MouseEventFlags[] list, int data = 0)
            {
                for (var i = 0; i < list.Length; i++)
                {
                    System.Drawing.Point pos = System.Windows.Forms.Cursor.Position;
                    mouse_event((uint)list[i], Convert.ToUInt32(pos.X), Convert.ToUInt32(pos.Y), unchecked((uint)data), 0);
                }
            }

            /// <summary>
            /// Нажать клавишу
            /// </summary>
            /// <param name="key">код клавиши</param>
            public static void KeyDown(byte key)
            {
                try
                {
                    keybd_event(key, 0, 0, 0);
                }
                catch
                {
                }
            }

            /// <summary>
            /// Отпустить клавишу
            /// </summary>
            /// <param name="key">код клавиши</param>
            public static void KeyUp(byte key)
            {
                try
                {
                    keybd_event(key, 0, 0x2, 0);
                }
                catch
                {
                }
            }

            /// <summary>
            /// Нажатие "горячей клавиши"
            /// </summary>
            /// <param name="key"></param>
            public static void SendCtrlhotKey(char key)
            {
                keybd_event(0x11, 0, 0, 0);
                keybd_event((byte)key, 0, 0, 0);
                keybd_event((byte)key, 0, 0x2, 0);
                keybd_event(0x11, 0, 0x2, 0);
            }
        }

    /// <summary>
        /// Эмулятор клавиатурного ввода
        /// </summary>
    public static class KeyboardImulation
        {
           // public static GlobalKeyboard GlobalKeyboard = new GlobalKeyboard();
            public static bool ProgrammingInput;
            public static byte VK_ABNT_C1 = 0xC1; //	Abnt C1
            public static byte VK_ABNT_C2 = 0xC2; //	Abnt C2
            public static byte VK_ADD = 0x6B; //	Numpad +
            public static byte VK_ATTN = 0xF6; //	Attn
            public static byte VK_BACK = 0x08; //	Backspace
            public static byte VK_CANCEL = 0x03; //	Break
            public static byte VK_CLEAR = 0x0C; //	Clear
            public static byte VK_CRSEL = 0xF7; //	Cr Sel
            public static byte VK_DECIMAL = 0x6E; //	Numpad .
            public static byte VK_DIVIDE = 0x6F; //	Numpad /
            public static byte VK_EREOF = 0xF9; //	Er Eof
            public static byte VK_ESCAPE = 0x1B; //	Esc
            public static byte VK_EXECUTE = 0x2B; //	Execute
            public static byte VK_EXSEL = 0xF8; //	Ex Sel
            public static byte VK_ICO_CLEAR = 0xE6; //	IcoClr
            public static byte VK_ICO_HELP = 0xE3; //	IcoHlp
            public static byte VK_KEY_0 = 0x30; // ('0')	0
            public static byte VK_KEY_1 = 0x31; // ('1')	1
            public static byte VK_KEY_2 = 0x32; // ('2')	2
            public static byte VK_KEY_3 = 0x33; // ('3')	3
            public static byte VK_KEY_4 = 0x34; // ('4')	4
            public static byte VK_KEY_5 = 0x35; // ('5')	5
            public static byte VK_KEY_6 = 0x36; // ('6')	6
            public static byte VK_KEY_7 = 0x37; // ('7')	7
            public static byte VK_KEY_8 = 0x38; // ('8')	8
            public static byte VK_KEY_9 = 0x39; // ('9')	9
            public static byte VK_KEY_A = 0x41; // ('A')	A
            public static byte VK_KEY_B = 0x42; // ('B')	B
            public static byte VK_KEY_C = 0x43; // ('C')	C
            public static byte VK_KEY_D = 0x44; // ('D')	D
            public static byte VK_KEY_E = 0x45; // ('E')	E
            public static byte VK_KEY_F = 0x46; // ('F')	F
            public static byte VK_KEY_G = 0x47; // ('G')	G
            public static byte VK_KEY_H = 0x48; // ('H')	H
            public static byte VK_KEY_I = 0x49; // ('I')	I
            public static byte VK_KEY_J = 0x4A; // ('J')	J
            public static byte VK_KEY_K = 0x4B; // ('K')	K
            public static byte VK_KEY_L = 0x4C; // ('L')	L
            public static byte VK_KEY_M = 0x4D; // ('M')	M
            public static byte VK_KEY_N = 0x4E; // ('N')	N
            public static byte VK_KEY_O = 0x4F; // ('O')	O
            public static byte VK_KEY_P = 0x50; // ('P')	P
            public static byte VK_KEY_Q = 0x51; // ('Q')	Q
            public static byte VK_KEY_R = 0x52; // ('R')	R
            public static byte VK_KEY_S = 0x53; // ('S')	S
            public static byte VK_KEY_T = 0x54; // ('T')	T
            public static byte VK_KEY_U = 0x55; // ('U')	U
            public static byte VK_KEY_V = 0x56; // ('V')	V
            public static byte VK_KEY_W = 0x57; // ('W')	W
            public static byte VK_KEY_X = 0x58; // ('X')	X
            public static byte VK_KEY_Y = 0x59; // ('Y')	Y
            public static byte VK_KEY_Z = 0x5A; // ('Z')	Z
            public static byte VK_MULTIPLY = 0x6A; //	Numpad *
            public static byte VK_NONAME = 0xFC; //	NoName
            public static byte VK_NUMPAD0 = 0x60; //	Numpad 0
            public static byte VK_NUMPAD1 = 0x61; //	Numpad 1
            public static byte VK_NUMPAD2 = 0x62; //	Numpad 2
            public static byte VK_NUMPAD3 = 0x63; //	Numpad 3
            public static byte VK_NUMPAD4 = 0x64; //	Numpad 4
            public static byte VK_NUMPAD5 = 0x65; //	Numpad 5
            public static byte VK_NUMPAD6 = 0x66; //	Numpad 6
            public static byte VK_NUMPAD7 = 0x67; //	Numpad 7
            public static byte VK_NUMPAD8 = 0x68; //	Numpad 8
            public static byte VK_NUMPAD9 = 0x69; //	Numpad 9
            public static byte VK_OEM_1 = 0xBA; //	OEM_1 (: ;)
            public static byte VK_OEM_102 = 0xE2; //	OEM_102 (> <)
            public static byte VK_OEM_2 = 0xBF; //	OEM_2 (? /)
            public static byte VK_OEM_3 = 0xC0; //	OEM_3 (~ `)
            public static byte VK_OEM_4 = 0xDB; //	OEM_4 ({ [)
            public static byte VK_OEM_5 = 0xDC; //	OEM_5 (| \)
            public static byte VK_OEM_6 = 0xDD; //	OEM_6 (} ])
            public static byte VK_OEM_7 = 0xDE; //	OEM_7 (" ')
            public static byte VK_OEM_8 = 0xDF; //	OEM_8 (§ !)
            public static byte VK_OEM_ATTN = 0xF0; //	Oem Attn
            public static byte VK_OEM_AUTO = 0xF3; //	Auto
            public static byte VK_OEM_AX = 0xE1; //	Ax
            public static byte VK_OEM_BACKTAB = 0xF5; //	Back Tab
            public static byte VK_OEM_CLEAR = 0xFE; //	OemClr
            public static byte VK_OEM_COMMA = 0xBC; //	OEM_COMMA (< ,)
            public static byte VK_OEM_COPY = 0xF2; //	Copy
            public static byte VK_OEM_CUSEL = 0xEF; //	Cu Sel
            public static byte VK_OEM_ENLW = 0xF4; //	Enlw
            public static byte VK_OEM_FINISH = 0xF1; //	Finish
            public static byte VK_OEM_FJ_LOYA = 0x95; //	Loya
            public static byte VK_OEM_FJ_MASSHOU = 0x93; //	Mashu
            public static byte VK_OEM_FJ_ROYA = 0x96; //	Roya
            public static byte VK_OEM_FJ_TOUROKU = 0x94; //	Touroku
            public static byte VK_OEM_JUMP = 0xEA; //	Jump
            public static byte VK_OEM_MINUS = 0xBD; //	OEM_MINUS (_ -)
            public static byte VK_OEM_PA1 = 0xEB; //	OemPa1
            public static byte VK_OEM_PA2 = 0xEC; //	OemPa2
            public static byte VK_OEM_PA3 = 0xED; //	OemPa3
            public static byte VK_OEM_PERIOD = 0xBE; //	OEM_PERIOD (> .)
            public static byte VK_OEM_PLUS = 0xBB; //	OEM_PLUS (+ =)
            public static byte VK_OEM_RESET = 0xE9; //	Reset
            public static byte VK_OEM_WSCTRL = 0xEE; //	WsCtrl
            public static byte VK_PA1 = 0xFD; //	Pa1
            public static byte VK_PACKET = 0xE7; //	Packet
            public static byte VK_PLAY = 0xFA; //	Play
            public static byte VK_PROCESSKEY = 0xE5; //	Process
            public static byte VK_RETURN = 0x0D; //	Enter
            public static byte VK_SELECT = 0x29; //	Select
            public static byte VK_SEPARATOR = 0x6C; //	Separator
            public static byte VK_SPACE = 0x20; //	Space
            public static byte VK_SUBTRACT = 0x6D; //	Num -
            public static byte VK_TAB = 0x09; //	Tab
            public static byte VK_ZOOM = 0xFB; //	Zoom
            public static byte VK__none_ = 0xFF; //	no VK mapping
            public static byte VK_ACCEPT = 0x1E; //	Accept
            public static byte VK_APPS = 0x5D; //	Context Menu
            public static byte VK_BROWSER_BACK = 0xA6; //	Browser Back
            public static byte VK_BROWSER_FAVORITES = 0xAB; //	Browser Favorites
            public static byte VK_BROWSER_FORWARD = 0xA7; //	Browser Forward
            public static byte VK_BROWSER_HOME = 0xAC; //	Browser Home
            public static byte VK_BROWSER_REFRESH = 0xA8; //	Browser Refresh
            public static byte VK_BROWSER_SEARCH = 0xAA; //	Browser Search
            public static byte VK_BROWSER_STOP = 0xA9; //	Browser Stop
            public static byte VK_CAPITAL = 0x14; //	Caps Lock
            public static byte VK_CONVERT = 0x1C; //	Convert
            public static byte VK_DELETE = 0x2E; //	Delete
            public static byte VK_DOWN = 0x28; //	Arrow Down
            public static byte VK_END = 0x23; //	End
            public static byte VK_F1 = 0x70; //	F1
            public static byte VK_F10 = 0x79; //	F10
            public static byte VK_F11 = 0x7A; //	F11
            public static byte VK_F12 = 0x7B; //	F12
            public static byte VK_F13 = 0x7C; //	F13
            public static byte VK_F14 = 0x7D; //	F14
            public static byte VK_F15 = 0x7E; //	F15
            public static byte VK_F16 = 0x7F; //	F16
            public static byte VK_F17 = 0x80; //	F17
            public static byte VK_F18 = 0x81; //	F18
            public static byte VK_F19 = 0x82; //	F19
            public static byte VK_F2 = 0x71; //	F2
            public static byte VK_F20 = 0x83; //	F20
            public static byte VK_F21 = 0x84; //	F21
            public static byte VK_F22 = 0x85; //	F22
            public static byte VK_F23 = 0x86; //	F23
            public static byte VK_F24 = 0x87; //	F24
            public static byte VK_F3 = 0x72; //	F3
            public static byte VK_F4 = 0x73; //	F4
            public static byte VK_F5 = 0x74; //	F5
            public static byte VK_F6 = 0x75; //	F6
            public static byte VK_F7 = 0x76; //	F7
            public static byte VK_F8 = 0x77; //	F8
            public static byte VK_F9 = 0x78; //	F9
            public static byte VK_FINAL = 0x18; //	Final
            public static byte VK_HELP = 0x2F; //	Help
            public static byte VK_HOME = 0x24; //	Home
            public static byte VK_ICO_00 = 0xE4; //	Ico00 *
            public static byte VK_INSERT = 0x2D; //	Insert
            public static byte VK_JUNJA = 0x17; //	Junja
            public static byte VK_KANA = 0x15; //	Kana
            public static byte VK_KANJI = 0x19; //	Kanji
            public static byte VK_LAUNCH_APP1 = 0xB6; //	App1
            public static byte VK_LAUNCH_APP2 = 0xB7; //	App2
            public static byte VK_LAUNCH_MAIL = 0xB4; //	Mail
            public static byte VK_LAUNCH_MEDIA_SELECT = 0xB5; //	Media
            public static byte VK_LBUTTON = 0x01; //	Left Button **
            public static byte VK_LCONTROL = 0xA2; //	Left Ctrl
            public static byte VK_LEFT = 0x25; //	Arrow Left
            public static byte VK_LMENU = 0xA4; //	Left Alt
            public static byte VK_LSHIFT = 0xA0; //	Left Shift
            public static byte VK_LWIN = 0x5B; //	Left Win
            public static byte VK_MBUTTON = 0x04; //	Middle Button **
            public static byte VK_MEDIA_NEXT_TRACK = 0xB0; //	Next Track
            public static byte VK_MEDIA_PLAY_PAUSE = 0xB3; //	Play / Pause
            public static byte VK_MEDIA_PREV_TRACK = 0xB1; //	Previous Track
            public static byte VK_MEDIA_STOP = 0xB2; //	Stop
            public static byte VK_MODECHANGE = 0x1F; //	Mode Change
            public static byte VK_NEXT = 0x22; //	Page Down
            public static byte VK_NONCONVERT = 0x1D; //	Non Convert
            public static byte VK_NUMLOCK = 0x90; //	Num Lock
            public static byte VK_OEM_FJ_JISHO = 0x92; //	Jisho
            public static byte VK_PAUSE = 0x13; //	Pause
            public static byte VK_PRINT = 0x2A; //	Print
            public static byte VK_PRIOR = 0x21; //	Page Up
            public static byte VK_RBUTTON = 0x02; //	Right Button **
            public static byte VK_RCONTROL = 0xA3; //	Right Ctrl
            public static byte VK_RIGHT = 0x27; //	Arrow Right
            public static byte VK_RMENU = 0xA5; //	Right Alt
            public static byte VK_RSHIFT = 0xA1; //	Right Shift
            public static byte VK_RWIN = 0x5C; //	Right Win
            public static byte VK_SCROLL = 0x91; //	Scrol Lock
            public static byte VK_SLEEP = 0x5F; //	Sleep
            public static byte VK_SNAPSHOT = 0x2C; //	Print Screen
            public static byte VK_UP = 0x26; //	Arrow Up
            public static byte VK_VOLUME_DOWN = 0xAE; //	Volume Down
            public static byte VK_VOLUME_MUTE = 0xAD; //	Volume Mute
            public static byte VK_VOLUME_UP = 0xAF; //	Volume Up
            public static byte VK_XBUTTON1 = 0x05; //	X Button 1 **
            public static byte VK_XBUTTON2 = 0x06; //	X Button 2 **

            /// <summary>
            /// Печатает заданный текст
            /// </summary>
            /// <param name="str">текст</param>
            public static void Print(string str)
            {
                ProgrammingInput = true;
                foreach (var c in str)
                {
                    if (char.IsUpper(c))
                        InputEmulation.KeyDown(VK_LSHIFT);
                    Thread.Sleep(5);

                    var b = (c + "").ToLower()[0];
                    if (b == ' ')
                    {
                        InputEmulation.KeyDown(VK_SPACE);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_SPACE);
                    }
                    else if (b == 'а')
                    {
                        InputEmulation.KeyDown(VK_KEY_F);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_F);
                    }
                    else if (b == 'б')
                    {
                        InputEmulation.KeyDown(VK_OEM_COMMA);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_COMMA);
                    }
                    else if (b == 'в')
                    {
                        InputEmulation.KeyDown(VK_KEY_D);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_D);
                    }
                    else if (b == 'г')
                    {
                        InputEmulation.KeyDown(VK_KEY_U);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_U);
                    }
                    else if (b == 'д')
                    {
                        InputEmulation.KeyDown(VK_KEY_L);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_L);
                    }
                    else if (b == 'е')
                    {
                        InputEmulation.KeyDown(VK_KEY_T);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_T);
                    }
                    else if (b == 'ё')
                    {
                        InputEmulation.KeyDown(VK_OEM_3);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_3);
                    }
                    else if (b == 'ж')
                    {
                        InputEmulation.KeyDown(VK_OEM_1);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_1);
                    }
                    else if (b == 'з')
                    {
                        InputEmulation.KeyDown(VK_KEY_P);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_P);
                    }
                    else if (b == 'и')
                    {
                        InputEmulation.KeyDown(VK_KEY_B);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_B);
                    }
                    else if (b == 'й')
                    {
                        InputEmulation.KeyDown(VK_KEY_Q);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_Q);
                    }
                    else if (b == 'к')
                    {
                        InputEmulation.KeyDown(VK_KEY_R);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_R);
                    }
                    else if (b == 'л')
                    {
                        InputEmulation.KeyDown(VK_KEY_K);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_K);
                    }
                    else if (b == 'м')
                    {
                        InputEmulation.KeyDown(VK_KEY_V);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_V);
                    }
                    else if (b == 'н')
                    {
                        InputEmulation.KeyDown(VK_KEY_Y);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_Y);
                    }
                    else if (b == 'о')
                    {
                        InputEmulation.KeyDown(VK_KEY_J);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_J);
                    }
                    else if (b == 'п')
                    {
                        InputEmulation.KeyDown(VK_KEY_G);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_G);
                    }
                    else if (b == 'р')
                    {
                        InputEmulation.KeyDown(VK_KEY_H);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_H);
                    }
                    else if (b == 'с')
                    {
                        InputEmulation.KeyDown(VK_KEY_C);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_C);
                    }
                    else if (b == 'т')
                    {
                        InputEmulation.KeyDown(VK_KEY_N);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_N);
                    }
                    else if (b == 'у')
                    {
                        InputEmulation.KeyDown(VK_KEY_E);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_E);
                    }
                    else if (b == 'ф')
                    {
                        InputEmulation.KeyDown(VK_KEY_A);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_A);
                    }
                    else if (b == 'х')
                    {
                        InputEmulation.KeyDown(VK_OEM_4);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_4);
                    }
                    else if (b == 'ц')
                    {
                        InputEmulation.KeyDown(VK_KEY_W);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_W);
                    }
                    else if (b == 'ч')
                    {
                        InputEmulation.KeyDown(VK_KEY_X);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_X);
                    }
                    else if (b == 'ш')
                    {
                        InputEmulation.KeyDown(VK_KEY_I);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_I);
                    }
                    else if (b == 'щ')
                    {
                        InputEmulation.KeyDown(VK_KEY_O);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_O);
                    }
                    else if (b == 'ъ')
                    {
                        InputEmulation.KeyDown(VK_OEM_6);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_6);
                    }
                    else if (b == 'ы')
                    {
                        InputEmulation.KeyDown(VK_KEY_S);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_S);
                    }
                    else if (b == 'ь')
                    {
                        InputEmulation.KeyDown(VK_KEY_M);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_M);
                    }
                    else if (b == 'э')
                    {
                        InputEmulation.KeyDown(VK_OEM_7);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_7);
                    }
                    else if (b == 'ю')
                    {
                        InputEmulation.KeyDown(VK_OEM_PERIOD);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_OEM_PERIOD);
                    }
                    else if (b == 'я')
                    {
                        InputEmulation.KeyDown(VK_KEY_Z);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_Z);
                    }
                    else if (b == '0')
                    {
                        InputEmulation.KeyDown(VK_KEY_0);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_0);
                    }
                    else if (b == '1')
                    {
                        InputEmulation.KeyDown(VK_KEY_1);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_1);
                    }
                    else if (b == '2')
                    {
                        InputEmulation.KeyDown(VK_KEY_2);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_2);
                    }
                    else if (b == '3')
                    {
                        InputEmulation.KeyDown(VK_KEY_3);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_3);
                    }
                    else if (b == '4')
                    {
                        InputEmulation.KeyDown(VK_KEY_4);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_4);
                    }
                    else if (b == '5')
                    {
                        InputEmulation.KeyDown(VK_KEY_5);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_5);
                    }
                    else if (b == '6')
                    {
                        InputEmulation.KeyDown(VK_KEY_6);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_6);
                    }
                    else if (b == '7')
                    {
                        InputEmulation.KeyDown(VK_KEY_7);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_7);
                    }
                    else if (b == '8')
                    {
                        InputEmulation.KeyDown(VK_KEY_8);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_8);
                    }
                    else if (b == '9')
                    {
                        InputEmulation.KeyDown(VK_KEY_9);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_9);
                    }
                    else if (b == 'a')
                    {
                        InputEmulation.KeyDown(VK_KEY_A);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_A);
                    }
                    else if (b == 'b')
                    {
                        InputEmulation.KeyDown(VK_KEY_B);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_B);
                    }
                    else if (b == 'c')
                    {
                        InputEmulation.KeyDown(VK_KEY_C);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_C);
                    }
                    else if (b == 'd')
                    {
                        InputEmulation.KeyDown(VK_KEY_D);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_D);
                    }
                    else if (b == 'e')
                    {
                        InputEmulation.KeyDown(VK_KEY_E);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_E);
                    }
                    else if (b == 'f')
                    {
                        InputEmulation.KeyDown(VK_KEY_F);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_F);
                    }
                    else if (b == 'g')
                    {
                        InputEmulation.KeyDown(VK_KEY_G);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_G);
                    }
                    else if (b == 'h')
                    {
                        InputEmulation.KeyDown(VK_KEY_H);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_H);
                    }
                    else if (b == 'i')
                    {
                        InputEmulation.KeyDown(VK_KEY_I);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_I);
                    }
                    else if (b == 'j')
                    {
                        InputEmulation.KeyDown(VK_KEY_J);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_J);
                    }
                    else if (b == 'k')
                    {
                        InputEmulation.KeyDown(VK_KEY_K);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_K);
                    }
                    else if (b == 'l')
                    {
                        InputEmulation.KeyDown(VK_KEY_L);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_L);
                    }
                    else if (b == 'm')
                    {
                        InputEmulation.KeyDown(VK_KEY_M);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_M);
                    }
                    else if (b == 'n')
                    {
                        InputEmulation.KeyDown(VK_KEY_N);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_N);
                    }
                    else if (b == 'o')
                    {
                        InputEmulation.KeyDown(VK_KEY_O);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_O);
                    }
                    else if (b == 'p')
                    {
                        InputEmulation.KeyDown(VK_KEY_P);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_P);
                    }
                    else if (b == 'q')
                    {
                        InputEmulation.KeyDown(VK_KEY_Q);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_Q);
                    }
                    else if (b == 'r')
                    {
                        InputEmulation.KeyDown(VK_KEY_R);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_R);
                    }
                    else if (b == 's')
                    {
                        InputEmulation.KeyDown(VK_KEY_S);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_S);
                    }
                    else if (b == 't')
                    {
                        InputEmulation.KeyDown(VK_KEY_T);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_T);
                    }
                    else if (b == 'u')
                    {
                        InputEmulation.KeyDown(VK_KEY_U);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_U);
                    }
                    else if (b == 'v')
                    {
                        InputEmulation.KeyDown(VK_KEY_V);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_V);
                    }
                    else if (b == 'w')
                    {
                        InputEmulation.KeyDown(VK_KEY_W);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_W);
                    }
                    else if (b == 'x')
                    {
                        InputEmulation.KeyDown(VK_KEY_X);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_X);
                    }
                    else if (b == 'y')
                    {
                        InputEmulation.KeyDown(VK_KEY_Y);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_Y);
                    }
                    else if (b == 'z')
                    {
                        InputEmulation.KeyDown(VK_KEY_Z);
                        Thread.Sleep(5);
                        InputEmulation.KeyUp(VK_KEY_Z);
                    }

                    Thread.Sleep(5);
                    if (char.IsUpper(c))
                        InputEmulation.KeyUp(VK_LSHIFT);
                    Thread.Sleep(50);
                }
                ProgrammingInput = false;
            }

            /// <summary>
            /// Нажатие клавиши
            /// </summary>
            /// <param name="key">Код клавиши</param>
            public static void PressKey(byte key)
            {
                ProgrammingInput = true;
                InputEmulation.KeyDown(VK_RETURN);
                Thread.Sleep(5);
                InputEmulation.KeyUp(VK_RETURN);
                ProgrammingInput = false;
            }

        public static InputLanguage GetInputLanguageByName(string inputName)
        {
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                if (lang.Culture.EnglishName.ToLower().StartsWith(inputName))
                    return lang;
            }
            return null;
        }
    }

  



    

}
