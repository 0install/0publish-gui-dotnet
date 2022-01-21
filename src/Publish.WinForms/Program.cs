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
                catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException)
                {
                    Msg.Inform(null, ex.GetMessageWithInner(), MsgSeverity.Warn);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Error);
                }
                #endregion
            }
        }
    }
}
