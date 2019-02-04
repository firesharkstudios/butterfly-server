using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Butterfly.Core.Util {
    public static class SecureUtil {

        // Based on https://stackoverflow.com/questions/35845076/x509certificate2-createfromcertfile-on-net-core
        public static X509Certificate2 CreateFromFiles(string certificateFile, string keyFile) {
            if (string.IsNullOrWhiteSpace(certificateFile)) throw new FileNotFoundException("Certificate file cannot be blank");
            if (!File.Exists(certificateFile)) throw new FileNotFoundException("Certificate file not found", certificateFile);

            if (string.IsNullOrWhiteSpace(certificateFile)) throw new FileNotFoundException("Key file cannot be blank");
            if (!File.Exists(keyFile)) throw new FileNotFoundException("Key file not found", keyFile);

            var cert = new X509Certificate2(certificateFile);
            cert.PrivateKey = CreateRSAFromFile(keyFile);
            return cert;
        }

        static RSACryptoServiceProvider CreateRSAFromFile(string filename) {
            byte[] pvk = null;
            using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                pvk = new byte[fs.Length];
                fs.Read(pvk, 0, pvk.Length);
            }

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(pvk);
            return rsa;
        }
    }
}
