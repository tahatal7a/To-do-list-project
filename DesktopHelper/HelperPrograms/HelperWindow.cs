using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopHelper
{
    //START HERE FOR ON SCREEN HELPER CODE
    
    internal static class HelperWindow
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        private static extern int PeekMessage(out HelperWindow.NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);
        
        
        public static void HelperMain()
        {
            //Set up Form for on screen helper to display. 

            //Makes Form Transparent and adapted to screen size, to allow helper to be drawn anywhere on screen in front of other active applications.

            HelperWindow.helperForm = new Form();
            HelperWindow.helperForm.BackColor = HelperWindow.TranspColor;
            HelperWindow.helperForm.FormBorderStyle = FormBorderStyle.None;
            HelperWindow.helperForm.Size = Screen.PrimaryScreen.WorkingArea.Size;
            HelperWindow.helperForm.StartPosition = FormStartPosition.Manual;
            HelperWindow.helperForm.Location = new Point(0, 0);
            HelperWindow.helperForm.TopMost = true;
            HelperWindow.helperForm.AllowTransparency = true;
            HelperWindow.helperForm.BackColor = HelperWindow.TranspColor;
            HelperWindow.helperForm.TransparencyKey = HelperWindow.TranspColor;
            HelperWindow.helperForm.ShowIcon = false;
            HelperWindow.helperForm.ShowInTaskbar = false;
            HelperWindow.OriginalWindowStyle = (IntPtr)((long)((ulong)HelperWindow.GetWindowLong(HelperWindow.helperForm.Handle, -20)));
            HelperWindow.PassthruWindowStyle = (IntPtr)((long)((ulong)(HelperWindow.GetWindowLong(HelperWindow.helperForm.Handle, -20) | 524288U | 32U)));
            HelperWindow.SetWindowP(true);
            HelperWindow.canvas = new BufferedPanel();
            HelperWindow.canvas.Dock = DockStyle.Fill;
            HelperWindow.canvas.BackColor = Color.Transparent;
            HelperWindow.canvas.BringToFront();
            HelperWindow.canvas.Paint += HelperWindow.Render;
            HelperWindow.helperForm.Controls.Add(HelperWindow.canvas);

            Application.Idle += HelperWindow.HandleIdle;
            Application.EnableVisualStyles();
            Application.Run(HelperWindow.helperForm);
        }

        //Match window size to screen
        private static void SetWindowP(bool passthrough)
        {
            if (passthrough)
            {
                HelperWindow.SetWindowLong(HelperWindow.helperForm.Handle, -20, HelperWindow.PassthruWindowStyle);
                return;
            }
            HelperWindow.SetWindowLong(HelperWindow.helperForm.Handle, -20, HelperWindow.OriginalWindowStyle);
        }

        private static bool IsIdle()
        {
            HelperWindow.NativeMessage nativeMessage;
            return HelperWindow.PeekMessage(out nativeMessage, IntPtr.Zero, 0U, 0U, 0U) == 0;
        }

        //Set Helper on top of other applications
        private static void HandleIdle(object sender, EventArgs e)
        {
            while (HelperWindow.IsIdle())
            {
                HelperWindow.helperForm.TopMost = true; 
                HelperWindow.canvas.BringToFront();
                HelperWindow.canvas.Invalidate();
                Thread.Sleep(8);
            }
       }
        
        //First Call to MainHelper.cs after finishing setup of Form, starts displaying helper.
        private static void Render(object sender, PaintEventArgs e)
        {
            MainHelper.Update(e);
        }


        private static IntPtr OriginalWindowStyle;
        private static IntPtr PassthruWindowStyle;
        private static BufferedPanel canvas;
        public static Color TranspColor = Color.Black; //Choose outline + color to be keyed out into transparency.
        public static Form helperForm;
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }
    }
}
