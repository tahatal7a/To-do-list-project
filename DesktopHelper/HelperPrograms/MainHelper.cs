using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
namespace DesktopHelper
{
    //
    public static class MainHelper
    {
        public static bool HelperEnable = true;  // Global enable variable for the helper to be turned on and off
        public static void Update(PaintEventArgs e) // Update is called by render and vice versa from HelperWindow.cs
        {
            Time.TickTime(); // Currently Unused, calls Time.cs for future features to check how long it has been since last reminder.
            Helper.Render(e);
        }
    }
}
