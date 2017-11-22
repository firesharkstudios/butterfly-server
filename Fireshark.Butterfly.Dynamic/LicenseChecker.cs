using System;
using System.IO;
using System.Reflection;

using DotNetLicense;

using Butterfly.Util;

namespace Fireshark.Butterfly.Dynamic {
    internal static class LicenseChecker {

        const string LICENSE_FILE = "license.lic";
        const string ERROR_SUFFIX = " Feel free to evaluate the Fireshark.Butterfly.Dynamic library for up to 90 days without a license key. After the first 90 days, a valid license key is required on each production server to be compliant with our license. Visit https://www.getbutterfly.io to get your license now.";

        static bool hasChecked = false;

        internal static void Check(Assembly assembly) {
            if (!hasChecked) {
                LicenseManager licenseManager = new LicenseManager();
                string publicKey = FileX.LoadResourceAsText(assembly, "public.key");
                licenseManager.LoadPublicKeyFromString(publicKey);

                MyLicense myLicense;

                // Read/write first checked date time
                DateTime? firstChecked = null;
                try {
                    string firstCheckedFile = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".fireshark", "first-checked-license.txt");
                    if (File.Exists(firstCheckedFile)) {
                        string firstCheckedContents = File.ReadAllText(firstCheckedFile);
                        if (DateTime.TryParse(firstCheckedContents, out DateTime value)) {
                            firstChecked = value;
                        }
                        else {
                            File.Delete(firstCheckedFile);
                        }
                    }
                    else {
                        File.WriteAllText(firstCheckedFile, DateTime.Now.ToShortDateString());
                    }
                }
                catch {
                }
                string noValidLicenseDays = firstChecked == null ? "" : $" (no valid license key for last {Math.Floor((DateTime.Now - firstChecked.Value).TotalDays)} days)";

                // Check license
                if (string.IsNullOrWhiteSpace(LICENSE_FILE)) {
                    Console.Error.WriteLine($"No license key configured for Fireshark.Butterfly.Dynamic.{noValidLicenseDays}{ERROR_SUFFIX}");
                }
                else if (!File.Exists(LICENSE_FILE)) {
                    Console.Error.WriteLine($"Could not find the license key at {LICENSE_FILE} for Fireshark.Butterfly.Dynamic.{noValidLicenseDays}{ERROR_SUFFIX}");
                }
                else {
                    try {
                        License baseLicense = licenseManager.LoadLicenseFromDisk(LICENSE_FILE);
                        myLicense = new MyLicense(baseLicense);
                        Console.Out.WriteLine($"Valid license key found for Fireshark.Butterfly.Dyanmic ({myLicense})");
                    }
                    catch (LicenseVerificationException e) {
                        Console.Error.WriteLine($"Invalid license key at {LICENSE_FILE} for Fireshark.Butterfly.Dynamic ({e.Message}).{noValidLicenseDays}{ERROR_SUFFIX}");
                    }
                }
                hasChecked = true;
            }
        }

        internal class MyLicense : License {
            public MyLicense() : base() {
                this.LicensedTo = "";
                this.Reference = "";
                this.ExpirationDate = DateTime.Now;
            }

            public MyLicense(License baseLicense) : base(baseLicense.ToXml()) {
            }

            public string LicensedTo {
                get => base.GetAttribute("LicensedTo");
                set => base.AddOrChangeAttribute("LicensedTo", value);
            }

            public string Reference {
                get => base.GetAttribute("Reference");
                set => base.AddOrChangeAttribute("Reference", value);
            }

            public DateTime ExpirationDate {
                get => DateTime.Parse(base.GetAttribute("ExpirationDate"));
                set => base.AddOrChangeAttribute("ExpirationDate", value.ToShortDateString());
            }

            public override string ToString() {
                return $"licensedTo={this.LicensedTo},reference={this.Reference},expirationDate={this.ExpirationDate}";
            }
        }
    }
}
