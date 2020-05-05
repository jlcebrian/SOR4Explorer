using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SOR4Explorer
{
    static class Program
    {
        public static Icon Icon { get; private set; }
        public static Image FolderIcon { get; private set; }
        public static Image FolderIconSmall { get; private set; }
        public static Image BarsImage { get; private set; }
        public static Image SaveImage { get; private set; }
        public static Image TrashImage { get; private set; }
        public static Image SheetIconSmall { get; private set; }

        static void LoadImages()
        {
            var assembly = typeof(Program).Assembly;
            Icon = new Icon(assembly.GetManifestResourceStream("SOR4Explorer.Images.SOR4Explorer.ico"));
            FolderIcon = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.FolderIcon.png"));
            FolderIconSmall = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.FolderIconSmall.png"));
            SheetIconSmall = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.SheetIconSmall.png"));
            BarsImage = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.bars.png"));
            SaveImage = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.save.png"));
            TrashImage = Image.FromStream(assembly.GetManifestResourceStream("SOR4Explorer.Images.trash.png"));
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoadImages();

            Application.Run(new ExplorerForm());
        }
    }
}
