using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SOR4Explorer
{
    public partial class ImagePreviewForm : Form
    {
        public ImagePreviewForm(Bitmap image, string name)
        {
            Screen screen = Screen.FromControl(this);
            int maxWidth = screen.WorkingArea.Width;
            int maxHeight = screen.WorkingArea.Height;

            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = name;

            ClientSize = image.Size;
            while (ClientSize.Width < 400 || ClientSize.Height < 400)
            {
                if (ClientSize.Width > maxWidth / 2 || ClientSize.Height > maxHeight / 2)
                    break;
                ClientSize *= 2;
            }
            Location = Cursor.Position - Size/2;
            Location = new Point(
                Math.Clamp(Location.X, screen.WorkingArea.X, Math.Max(screen.WorkingArea.X, screen.WorkingArea.Right - Width)),
                Math.Clamp(Location.Y, screen.WorkingArea.Y, Math.Max(screen.WorkingArea.Y, screen.WorkingArea.Bottom - Height))
            );

            Paint += (sender, ev) =>
            {
                ev.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                ev.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                ev.Graphics.DrawImage(image, ClientRectangle,
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            };
            MouseClick += (sender, ev) =>
            {
                switch (ev.Button)
                {
                    case MouseButtons.Left:
                        Close();
                        break;
                    case MouseButtons.Right:
                        ContextMenuStrip buttonMenu = ContextMenu.FromImage(name, image);
                        buttonMenu.Show(this, ev.Location);
                        break;
                }
            };
        }
    }
}
