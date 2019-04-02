using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NLog;

namespace Butterfly.Core.Util {
    public static class ProcessX {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
                var args = $"http add urlacl url={url} user=Everyone";
                logger.Warn($"AddHttpUrlAcl():Executing 'netsh {args}'");
                ProcessStartInfo processStartInfo = new ProcessStartInfo("netsh", args);
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

        public static async Task<int> WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken)) {
            var tcs = new TaskCompletionSource<bool>();

            void Process_Exited(object sender, EventArgs e) {
                tcs.TrySetResult(true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            try {
                if (process.HasExited) {
                    return process.ExitCode;
                }

                using (cancellationToken.Register(() => tcs.TrySetCanceled())) {
                    await tcs.Task;
                }
            }
            finally {
                process.Exited -= Process_Exited;
            }

            return process.ExitCode;
        }

        public static async Task<(string, int)> WaitAndCaptureAsync(string fileName, string args, CancellationToken cancellationToken = default(CancellationToken)) {
            ProcessStartInfo start = new ProcessStartInfo() {
                FileName = fileName,
                Arguments = args
            };

            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;

            using (MemoryStream memoryStream = new MemoryStream()) {
                int exitCode;
                using (StreamWriter streamWriter = new StreamWriter(memoryStream)) {
                    using (Process process = Process.Start(start)) {
                        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                        var standardOutputTask = Task.Run(() => CopyStreamAsync(process.StandardOutput, streamWriter), cancellationTokenSource.Token);
                        var standardErrorTask = Task.Run(() => CopyStreamAsync(process.StandardError, streamWriter), cancellationTokenSource.Token);

                        exitCode = await process.WaitForExitAsync();
                        cancellationTokenSource.Cancel();
                    }
                }
                return (Encoding.ASCII.GetString(memoryStream.ToArray()), exitCode);
            }
        }

        static async Task CopyStreamAsync(StreamReader reader, StreamWriter streamWriter) {
            while (!reader.EndOfStream) {
                string line = await reader.ReadLineAsync();
                await streamWriter.WriteLineAsync(line);
                //await streamWriter.FlushAsync();
            }
        }

    }
}
