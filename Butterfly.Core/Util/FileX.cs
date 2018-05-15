/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Butterfly.Core.Util {
    public static class FileX {
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
