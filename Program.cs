using System;
using System.Windows.Forms;

namespace HtmlParserApp // Make ABSOLUTELY sure this matches the namespace of Form1
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware); // For better scaling on high DPI displays
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());
        }
    }
}