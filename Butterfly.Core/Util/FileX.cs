/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.Core.Util {
    public static class FileX {
        public static string GetCurrentDirectory() {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (currentDirectory.EndsWith(Path.PathSeparator.ToString())) {
                return currentDirectory.Substring(0, currentDirectory.Length - 1);
            }
            else {
                return currentDirectory;
            }
        }

        public static string Resolve(string path) {
            return Path.GetFullPath(new Uri(Path.Combine(path)).LocalPath);
        }

        public static async Task WriteTextAsync(string filePath, string text) {
            byte[] encodedText = Encoding.Default.GetBytes(text);

            using (FileStream sourceStream = GetWriteFileStream(filePath)) {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        public static async Task<string> ReadTextAsync(string filePath) {
            using (FileStream sourceStream = GetReadFileStream(filePath)) {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0) {
                    string text = Encoding.Default.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }

                return sb.ToString();
            }
        }

        public static FileStream GetReadFileStream(string file, bool async = true) {
            FileOptions options = async ? FileOptions.Asynchronous | FileOptions.SequentialScan : FileOptions.SequentialScan;
            return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, options);
        }

        public static FileStream GetWriteFileStream(string file, bool async = true) {
            FileOptions options = async ? FileOptions.Asynchronous | FileOptions.SequentialScan : FileOptions.SequentialScan;
            return new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, options);
        }

        public static string LoadResourceAsText(Assembly assembly, string file) {
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(file))) {
                return reader.ReadToEnd();
            }
        }

        public static async Task<string> LoadResourceAsTextAsync(Assembly assembly, string file) {
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(file))) {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
