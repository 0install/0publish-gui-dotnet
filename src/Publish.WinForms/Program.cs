// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NanoByte.Common;
using NanoByte.Common.Controls;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.WinForms
{
    /// <summary>
    /// Launches a WinForms-based editor for Zero Install feed XMLs.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread] // Required for WinForms
        private static void Main(string[] args)
        {
            ProcessUtils.SanitizeEnvironmentVariables();
            NetUtils.ApplyProxy();
            NetUtils.ConfigureTls();

            WindowsUtils.SetCurrentProcessAppID("ZeroInstall.Publishing");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ErrorReportForm.SetupMonitoring(new Uri("https://0install.de/error-report/"));

            var openPgp = OpenPgp.Signing();

            if (args.Length == 0) Application.Run(new WelcomeForm(openPgp));
            else
            {
                try
                {
                    var files = Paths.ResolveFiles(args, "*.xml");
                    if (files.Count == 1)
                    {
                        string path = files.First().FullName;
                        Application.Run(new MainForm(FeedEditing.Load(path), openPgp));
                    }
                    else MassSignForm.Show(files);
                }
                #region Error handling
                catch (ArgumentException ex)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                }
                catch (IOException ex)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                }
                catch (InvalidDataException ex)
                {
                    Msg.Inform(null, ex.Message + (ex.InnerException == null ? "" : Environment.NewLine + ex.InnerException.Message), MsgSeverity.Warn);
                }
                #endregion
            }
        }
    }
}
