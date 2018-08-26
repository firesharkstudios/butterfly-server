using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Butterfly.Core.Util {
    public static class ProcessX {
        // Per https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
        public static void OpenBrowser(string url) {
            try {
                Process.Start(url);
            }
            catch {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                }
                else {
                    throw;
                }
            }
        }

        public static void AddHttpUrlAclIfNeeded(string url) {
            bool hasHttpUrlAcl = HasHttpUrlAcl(url);
            if (!hasHttpUrlAcl) {
                AddHttpUrlAcl(url);
            }
        }

        public static void AddHttpUrlAcl(string url) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                ProcessStartInfo processStartInfo = new ProcessStartInfo("netsh", $"http add urlacl url={url} user=Everyone");
                processStartInfo.Verb = "runas";
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processStartInfo.UseShellExecute = true;

                Process.Start(processStartInfo).WaitForExit();
            }
        }

        public static bool HasHttpUrlAcl(string url) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                ProcessStartInfo processStartInfo = new ProcessStartInfo("netsh", $"http show urlacl url={url}");
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;

                StringBuilder sb = new StringBuilder();
                var process = new Process();
                process.StartInfo = processStartInfo;
                process.OutputDataReceived += (sender, data) => sb.AppendLine(data.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();

                return sb.ToString().Contains(@"User: \Everyon");
            }
            else {
                return true;
            }
        }

    }
}
