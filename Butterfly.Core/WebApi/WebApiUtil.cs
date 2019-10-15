using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Butterfly.Core.WebApi {
    public class WebApiUtil {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
                        if (File.Exists(uploadFile)) File.Delete(uploadFile);
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
                if (File.Exists(mediaFileName)) File.Delete(mediaFileName);
                File.Move(uploadFileName, mediaFileName);
            }

            logger.Debug($"FileUploadHandler():Uploaded media files: {string.Join(", ", mediaFileNames)}");
            return mediaFileNames.ToArray();
        }

    }
}
