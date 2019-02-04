/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using NLog;

namespace Butterfly.Core.WebApi {

    /// <inheritdoc/>
    /// <summary>
    /// Base class implementing <see cref="IWebApi"/>. New implementations will normally extend this class.
    /// </summary>
    public abstract class BaseWebApi : IWebApi {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected readonly List<WebHandler> webHandlers = new List<WebHandler>();

        public void OnDelete(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnDelete():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Delete,
                path = path,
                listener = listener
            });
        }

        public void OnGet(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnGet():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Get,
                path = path,
                listener = listener
            });
        }

        public void OnPost(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnPost():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Post,
                path = path,
                listener = listener
            });
        }

        public void OnPut(string path, Func<IHttpRequest, IHttpResponse, Task> listener) {
            logger.Debug($"OnPut():path={path}");
            webHandlers.Add(new WebHandler {
                method = HttpMethod.Put,
                path = path,
                listener = listener
            });
        }

        public static async Task<string[]> FileUploadHandlerAsync(IHttpRequest req, string tempPath, string finalPath, Func<string, string> getFileName, int chunkDelayInMillis = 0) {
            var fileStreamByName = new Dictionary<string, FileStream>();
            var uploadFileNameByName = new Dictionary<string, string>();

            // Parse stream
            req.ParseAsMultipartStream(
                onData: (name, rawFileName, type, disposition, buffer, bytes) => {
                    logger.Debug($"FileUploadHandlerAsync():onData():rawFileName={rawFileName}");

                    string fileName = Path.GetFileName(rawFileName);
                    logger.Debug($"FileUploadHandlerAsync():onData():name={name},fileName={fileName},type={type},disposition={disposition},bytes={bytes}");

                    if (!fileStreamByName.TryGetValue(name, out FileStream fileStream)) {
                        string uploadFileName = getFileName(fileName);
                        string uploadFile = Path.Combine(tempPath, uploadFileName);
                        logger.Debug($"FileUploadHandlerAsync():onData():uploadFile={uploadFile}");
                        fileStream = new FileStream(uploadFile, FileMode.CreateNew);
                        uploadFileNameByName[name] = uploadFile;
                        fileStreamByName[name] = fileStream;
                    }
                    fileStream.Write(buffer, 0, bytes);
                    if (chunkDelayInMillis > 0) {
                        Thread.Sleep(chunkDelayInMillis);
                    }
                },
                onParameter: (name, value) => {
                    logger.Debug($"FileUploadHandlerAsync():onParameter():name={name},value={value}");
                }
            );

            // Move files from tempPath to finalPath
            List<string> mediaFileNames = new List<string>();
            foreach (var pair in fileStreamByName) {
                await pair.Value.FlushAsync();
                pair.Value.Close();
                var uploadFileName = uploadFileNameByName[pair.Key];
                var mediaFileName = Path.Combine(finalPath, Path.GetFileName(uploadFileName));
                mediaFileNames.Add(Path.GetFileName(mediaFileName));
                logger.Debug($"FileUploadHandlerAsync():Move {uploadFileName} to {mediaFileName}");
                File.Move(uploadFileName, mediaFileName);
            }

            logger.Debug($"FileUploadHandler():Uploaded media files: {string.Join(", ", mediaFileNames)}");
            return mediaFileNames.ToArray();
        }

        public List<WebHandler> WebHandlers {
            get {
                return this.webHandlers;
            }
        }

        public abstract void Compile();
        public abstract void Dispose();

    }
}
