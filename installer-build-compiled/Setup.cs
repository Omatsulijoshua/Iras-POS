using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace IRASSpotPOSSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IRAS SPOT POS");
                string tempInstallDir = installDir + ".installing";
                string zipPath = Path.Combine(Path.GetTempPath(), "IRAS_SPOT_POS_app.zip");
                string backupConfigDir = Path.Combine(Path.GetTempPath(), "IRAS_SPOT_POS_config_backup");

                CloseRunningApp(installDir);
                BackupDatabaseSettings(installDir, backupConfigDir);

                using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.zip"))
                {
                    if (resource == null)
                        throw new InvalidOperationException("Installer payload is missing.");

                    using (FileStream file = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                        resource.CopyTo(file);
                }

                DeleteDirectoryIfExists(tempInstallDir);
                Directory.CreateDirectory(tempInstallDir);
                ZipFile.ExtractToDirectory(zipPath, tempInstallDir);
                File.Delete(zipPath);

                ValidateInstall(tempInstallDir);

                DeleteDirectoryIfExists(installDir);
                Directory.Move(tempInstallDir, installDir);
                RestoreDatabaseSettings(installDir, backupConfigDir);

                string exePath = Path.Combine(installDir, "POSales.exe");
                string iconPath = Path.Combine(installDir, "app_logo.ico");

                string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "IRAS SPOT POS.lnk");
                DeleteFileIfExists(desktopShortcut);
                CreateShortcut(desktopShortcut, exePath, installDir, iconPath);

                string startDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "IRAS SPOT POS");
                DeleteDirectoryIfExists(startDir);
                Directory.CreateDirectory(startDir);
                CreateShortcut(Path.Combine(startDir, "IRAS SPOT POS.lnk"), exePath, installDir, iconPath);

                MessageBox.Show("IRAS SPOT POS has been installed with the logo icon and all report files.", "Installation complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IRAS SPOT POS Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = 1;
            }
        }

        private static void CloseRunningApp(string installDir)
        {
            foreach (Process process in Process.GetProcessesByName("POSales"))
            {
                try
                {
                    string modulePath = string.Empty;
                    try { modulePath = process.MainModule.FileName; } catch { }

                    bool sameInstall = modulePath.StartsWith(installDir, StringComparison.OrdinalIgnoreCase);
                    if (!sameInstall && !string.IsNullOrEmpty(modulePath))
                        continue;

                    if (process.CloseMainWindow())
                        process.WaitForExit(5000);

                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch { }
            }
        }

        private static void ValidateInstall(string installDir)
        {
            string exePath = Path.Combine(installDir, "POSales.exe");
            string iconPath = Path.Combine(installDir, "app_logo.ico");
            string reportsDir = Path.Combine(installDir, "Reports");
            string[] reports = {
                "rptCancelled.rdlc",
                "rptInventory.rdlc",
                "rptRecept.rdlc",
                "rptSoldItems.rdlc",
                "rptSoldReport.rdlc",
                "rptStockInHist.rdlc",
                "rptTopSell.rdlc"
            };

            if (!File.Exists(exePath))
                throw new FileNotFoundException("POSales.exe was not installed.", exePath);

            if (!File.Exists(iconPath))
                throw new FileNotFoundException("Logo icon was not installed.", iconPath);

            foreach (string report in reports)
            {
                string reportPath = Path.Combine(reportsDir, report);
                if (!File.Exists(reportPath))
                    throw new FileNotFoundException("Report file was not installed: " + report, reportPath);
            }
        }

        private static void BackupDatabaseSettings(string installDir, string backupConfigDir)
        {
            DeleteDirectoryIfExists(backupConfigDir);
            Directory.CreateDirectory(backupConfigDir);

            if (!Directory.Exists(installDir))
                return;

            foreach (string pattern in new[] { "dbconnection*.txt", "license*.lic" })
            {
                foreach (string file in Directory.GetFiles(installDir, pattern, SearchOption.TopDirectoryOnly))
                    File.Copy(file, Path.Combine(backupConfigDir, Path.GetFileName(file)), true);
            }
        }

        private static void RestoreDatabaseSettings(string installDir, string backupConfigDir)
        {
            if (!Directory.Exists(backupConfigDir))
                return;

            foreach (string pattern in new[] { "dbconnection*.txt", "license*.lic" })
            {
                foreach (string file in Directory.GetFiles(backupConfigDir, pattern, SearchOption.TopDirectoryOnly))
                    File.Copy(file, Path.Combine(installDir, Path.GetFileName(file)), true);
            }

            DeleteDirectoryIfExists(backupConfigDir);
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (!Directory.Exists(path))
                return;

            ClearAttributes(path);
            Exception lastError = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(500);
                }
            }

            throw new IOException("Could not replace the existing installation. Close IRAS SPOT POS and try again. Details: " + lastError.Message, lastError);
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!File.Exists(path))
                return;

            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

        private static void ClearAttributes(string path)
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(dir, FileAttributes.Directory);

            File.SetAttributes(path, FileAttributes.Directory);
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "IRAS SPOT POS" });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
    }
}
