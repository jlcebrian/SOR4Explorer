using System.Drawing;
using System.Windows.Forms;

namespace SOR4Explorer
{
    class ToolStripColorTable : ProfessionalColorTable
    {
        static readonly Color greyBackground = Color.FromArgb(48, 48, 48);

        public override Color ToolStripBorder => Color.Black;

        public override Color ButtonSelectedGradientBegin => greyBackground;
        public override Color ButtonSelectedGradientEnd => greyBackground;
        public override Color ButtonSelectedHighlight => greyBackground;
        public override Color ButtonSelectedBorder => greyBackground;
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
