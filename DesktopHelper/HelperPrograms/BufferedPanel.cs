using System;
using System.Windows.Forms;

namespace DesktopHelper
{
	//Panel type that prevents flickering as the helper is drawn each code loop.

	public class BufferedPanel : Panel
	{
		public BufferedPanel()
		{
			this.DoubleBuffered = true;
		}
	}
}
