using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace keys
{


    public static class MouseHook
    {
        public static event EventHandler MouseAction = delegate { };

        public static void Start()
        {
            _hookID = SetHook(_proc);


        }
        public static void stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                  GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
          int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                MouseAction(null, new EventArgs());
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
          LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
          IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


    }
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private unsafe static extern bool SetDeviceGammaRamp(Int32 hdc, void* ramp);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private unsafe static extern bool GetDeviceGammaRamp(Int32 hdc, void* ramp);

        private static bool initialized = false;
        private static Int32 hdc;

        private short bri = 125;

        public short Bri { get => bri; set => SetBrightness(value); }

        private static void InitializeClass()
        {
            if (initialized)
                return;

            //Get the hardware device context of the screen, we can do
            //this by getting the graphics object of null (IntPtr.Zero)
            //then getting the HDC and converting that to an Int32.
            hdc = Graphics.FromHwnd(IntPtr.Zero).GetHdc().ToInt32();

            initialized = true;
        }

        public static unsafe bool SetBrightness(short brightness)
        {
            

            if (brightness > 255)
                brightness = 255;

            if (brightness < 0)
                brightness = 0;

            short* gArray = stackalloc short[3 * 256];
            short* idx = gArray;

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 256; i++)
                {
                    int arrayVal = i * (brightness + 128);

                    if (arrayVal > 65535)
                        arrayVal = 65535;

                    *idx = (short)arrayVal;
                    idx++;
                }
            }

            //For some reason, this always returns false?
            bool retVal = SetDeviceGammaRamp(hdc, gArray);

            //Memory allocated through stackalloc is automatically free'd
            //by the CLR.

            return retVal;
        }

        public static unsafe bool GetBrightness()
        {
            InitializeClass();

         

            short* gArray = stackalloc short[3 * 256];
            short* idx = gArray;
 

            //For some reason, this always returns false?
            bool retVal = GetDeviceGammaRamp(hdc, gArray);

            //Memory allocated through stackalloc is automatically free'd
            //by the CLR.

            return retVal;
        }


        System.Threading.Thread myThread;
        private void OnApplicationExit(object sender, System.Windows.ExitEventArgs e)
        {
            if (myThread.IsAlive)
            {
                myThread.Abort();
            }
            SetBrightness(130);
        }

        [System.Runtime.InteropServices.DllImport("Shell32")]
        public static extern int ExtractIconEx(
            string sFile,
            int iIndex,
            out IntPtr piLargeVersion,
            out IntPtr piSmallVersion,
            int amountIcons);

        public Icon GetExecutableIcon()
        {
            IntPtr large;
            IntPtr small;
            ExtractIconEx(Application.ExecutablePath, 0, out large, out small, 1);
            return Icon.FromHandle(small);
        }


        public Form1()
        {
            InitializeComponent();
            this.myThread = new System.Threading.Thread(new System.Threading.ThreadStart(myStartingMethod));
            MouseHook.Start();
            MouseHook.MouseAction += new EventHandler(Event);
            InitializeClass();
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            this.ShowInTaskbar = false;

            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(Form1_FormClosed);
            this.Icon = this.GetExecutableIcon();
            NotifyIcon icon = new NotifyIcon();
            icon.Icon = this.Icon;
            //this.Controls.Add(icon);
            icon.Visible = true;
            icon.Click += new System.EventHandler(onNotifyIconClick); 
        }

        protected void onNotifyIconClick(object sender, EventArgs e)
        {
            this.Close();
        }


        protected void Form1_FormClosed(object sender, EventArgs e)
        {
            if (myThread.IsAlive)
            {
                myThread.Abort();
            }
            SetBrightness(130);
        }

  
        private void myStartingMethod()
        {
            SetBrightness(0);
            System.Threading.Thread.Sleep(6000);
            SetBrightness(135);
        }

        private void Event(object sender, EventArgs e) {
            Console.WriteLine("Left mouse click!");
            if (myThread.IsAlive)
            {
                myThread.Abort();
            }
            this.myThread = new System.Threading.Thread(new System.Threading.ThreadStart(myStartingMethod));
            myThread.Start(); 
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
    }
}
