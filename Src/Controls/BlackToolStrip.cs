using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SOR4Explorer
{
    class BlackToolStrip : ToolStrip
    {
        public ToolStripItemAlignment NextAlignment = ToolStripItemAlignment.Left;

        public BlackToolStrip()
        {
            AutoSize = false;
            Stretch = true;
            Dock = DockStyle.Top;
            ForeColor = Color.White;
            BackColor = Color.Black;
            Size = new Size(400, 60);
            Padding = new Padding(0);
            GripStyle = ToolStripGripStyle.Hidden;
            Renderer = new ToolStripCustomRenderer();
            CanOverflow = false;
        }

        public ToolStripLabel AddLabel(string text = "")
        {
            var label = new ToolStripLabel()
            {
                Alignment = NextAlignment,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                Size = new Size(400, 10),
                Margin = new Padding(0, 0, 32, 0),
                Text = text,
            };
            Items.Add(label);
            return label;
        }

        public ToolStripProgressBar AddProgressBar()
        {
            var progressBar = new ToolStripProgressBar()
            {
                Alignment = NextAlignment,
                Size = new Size(200, 8),
                Padding = new Padding(0, 0, 40, 0),
            };
            Items.Add(progressBar);
            return progressBar;
        }

        public ToolStripButton AddButton(Image image, string label = "", Action action = null)
        {
            var button = new ToolStripButton(label, image, (o, s) => action?.Invoke())
            {
                Alignment = NextAlignment,
                AutoToolTip = false,
                Padding = new Padding(16),
                ImageAlign = ContentAlignment.MiddleRight,
            };
            Items.Add(button);
            return button;
        }

        public ToolStripMenuItem AddMenuItem(Image image, string label = "", Action action = null)
        {
            var item = new ToolStripMenuItem(label, image, (o, s) => action?.Invoke())
            {
                Alignment = NextAlignment,
                Padding = new Padding(16),
            };
            Items.Add(item);
            return item;
        }
    }

    class ToolStripColorTable : ProfessionalColorTable
    {
        static readonly Color greyBackground = Color.FromArgb(48, 48, 48);

        public override Color ToolStripBorder => Color.Black;

        public override Color ButtonSelectedGradientBegin => greyBackground;
        public override Color ButtonSelectedGradientEnd => greyBackground;
        public override Color ButtonSelectedHighlight => greyBackground;
        public override Color ButtonSelectedBorder => greyBackground;

        public override Color MenuItemPressedGradientBegin => greyBackground;
        public override Color MenuItemPressedGradientEnd => greyBackground;
        public override Color MenuItemSelectedGradientBegin => SystemColors.Highlight;
        public override Color MenuItemSelectedGradientEnd => SystemColors.Highlight;
        public override Color MenuItemSelected => greyBackground;
        public override Color MenuItemBorder => greyBackground;

        public override Color ToolStripDropDownBackground => greyBackground;
        public override Color ImageMarginGradientBegin => greyBackground;
        public override Color ImageMarginGradientMiddle => greyBackground;
        public override Color ImageMarginGradientEnd => greyBackground;
    }

    class ToolStripCustomRenderer : ToolStripProfessionalRenderer
    {
        public ToolStripCustomRenderer() : base(new ToolStripColorTable() { UseSystemColors = false })
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.White;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            base.OnRenderButtonBackground(e);
            if (e.Item.Pressed)
            {
                Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                Color c = SystemColors.Highlight;
                using SolidBrush brush = new SolidBrush(c);
                e.Graphics.FillRectangle(brush, rc);
            }
        }
    }
}
