using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace POSales
{
    public static class LicenseManager
    {
        // Path to local appdata folder and file
        private static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IRAS SPOT POS");
        private static readonly string LicenseFilePath = Path.Combine(AppDataDir, "license.lic");

        // Registry key path
        private static readonly string RegistryKeyPath = @"Software\IRAS_SPOT_POS";
        private static readonly string RegistryValueName = "LicenseData";

        // Current license state
        public static string ActiveKey { get; private set; }
        public static DateTime? ActivationDate { get; private set; }
        public static DateTime? ExpirationDate { get; private set; }
        public static DateTime? LastRunDate { get; private set; }
        public static HashSet<string> UsedKeyHashes { get; private set; }

        // Embedded SHA-256 hashes of valid keys
        public static readonly HashSet<string> Keys6Months = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "9bda19c71d31698b70ad4087fb0cdef6fb9cabbfc68f71a4a8d162da47450984",
            "af2d10d07c85d053ef7377ca1ce0439a166cfdb2ea15e0fb4f049d4c55362c2c",
            "2e762023ad8e2755accecf51e5cabda2b0d50b67dbe05a3a9012d29112e6d67a",
            "62c4cfc2722c3e63f650d93e258bef71d8a176ee8b4e0c25ba913c37189e25c8",
            "20f79d2a046be7a293d63d60c650585cc5fe94c0b2a618a831045bae2adc8c66",
            "37189d5d0e01e670376add5870c7ded36c70910cec3d78e2757f40e643d9566a",
            "fd788e0781d1a6fa0a730da2644964ac344484a5357981465fe960ecf7ebef28",
            "a8f8784e81bbd9d267b268dc3d90df8d30807e788f937deaa8ab640f58a7ff43",
            "62598df3adeff21337188b45adc2fe9a329df5812593a7480f8f5e4bdac91e2b",
            "1ca8ce46dc92dcbbbbccaf80220fb11c19c4d375393bda090b494c97b9e72d20",
            "afa3ab5373a13e93f4c3e3202e44929993902adbf76533f865564ca95e8721f6",
            "370ea22dbee7efbef6bed48c67c9b5a3f6d6fd8718c34198277edf85d1d51f12",
            "eb3581c1bbb9f16d619798fc4b4e0bcefacde9563cb227854fbaa95ae44613b7",
            "2aaf54688b4136bd1882a3c49d2e68aaf3df77751cc1c415b57aecaae4302eaa",
            "d21cb94b0e354e4475522110e8c125c955ca97c28a85c7027790be257b18cbcd",
            "47559b1972d82153713ba073fd9d820d3bdbcdf5a489c19c754120cd14796ddd",
            "566e51d95be386a86eac5fb9f5bfea09d3b577e2dbe20152673d5689b4700cf9",
            "3fb9b16080f0f1f84e396c62f3cd4827189d5604912d1ed025f06c75f4445219",
            "1b1514a1c360f8ebb685d44652d1f58388e13e99738e71ec1832a009ffdb2615",
            "1d55b5abd9abe0c42c87077735228c921598a5cbf76d5764bec9809e91f2bec3",
            "62cc4afa48e478d6a2d0811e4fe9fa18aaad2e7a378035dce2997d9a0f8742d4",
            "27dea6447ce1cb2a143af979fe4903c4585c22ba7fdc5c1e8a7899f4ad222932",
            "0a9991bb26ed5fcdb8ae4a29608a19e4c7842661030a76876ad1503712a4fa8b",
            "7b3822a92282e7a8a56f8fda2337a5862e2f8b4ec170fab36d596c23473e72a1",
            "a365fc06088060f056332980fbc76ebcb04e8a7a94ece04d30b2d7b4e5e2b423",
            "f83362576c5ba1d6f85bf12632373ea018d64f60bded2f3f8a174c904cfb773d",
            "2f9afa0671d11020ea033c489ddd920543e2cbcc57c18a2e7dc2b587dd0672d7",
            "f0678938979d14f7c1ca48ee041ba2cf63a73868a2859c934d58b946c26948f9",
            "90c596cd12afe3eec634971ac8c8a49b8c736a0a46f90bb48d9b7720893fa560",
            "7c24efab17c11976ec916da07d0f6b74e2488290b331ab379524e914f193ae99",
            "966d93a20ab3476bbf6cab2db28748481be5139fd12810c492487e0a29a7a749",
            "2404556acbb7b50ecb9b30d1dad3a9ac6f8ce41d7f39008a9443ca2223d8e5c9",
            "644780f0c54672b70750120be3549142f59bf3825c6da10c381b746957972ef7",
            "ec4037f21bc0ab721848824b3d8a191bc95f5c7b161b3293f3461c1257f1cbec",
            "1b47182e16c47e5542343a6b2d578ca3133874f6e0fcfa6e253fec9920b09d72",
            "b7f1b60f9f562f0dcb5898a2bb767e022b8379c8715e89ad237a48ca74224e00",
            "e5d56f19ed095cbb6d8c8c7e0a09fb1589b5ac41dc282eb743c0d2ad37b87f5d",
            "c4ef286738b4ebff8036fdaafd9c843ea7446d46579a501305adbf271094403c",
            "6937455ae906ed906b4bc2eafaf76dc2489214e07320eb586af42586d285b635",
            "6435623a3544bc47a820c8116dcc8e5bc1ce106ef6ec6e3f7db74fc2effc8c48",
            "0f3379625bb3ea1faffd2a714dfb50170c2020d0b597fdd1f202852d0ef47d84",
            "3677b3412c315ce093f3c29999c9bf657ab5b9e4de6f569a1780aa4a3fc26015",
            "d7a30a4f59f3a45f792b75c6ed890148e792d972acce3a0f976aee618c0a4ed8",
            "428cb0259137e1b600c9b0fbd2620b933987966c48a2527d02c24494d83d988a",
            "72c9c6a47373d8d6ca7833ac09fff426b2a4e5d935a7f5114b129963e59edf5f",
            "71933934f74d32bc0862e31bf72bbe387ce85ecda33fc08b93f6cd0a302e5c5d",
            "bd3a4dd40590c49297f1305bcd5192ecd0a865d165509688613aaa6f082762a6",
            "03855a1af4596efe5c3416edd82e8705c9b30b78577bcb21d8de00a0a56d3a92",
            "e7e5f0520e83f1ff9db5c7ccbf454939bc145cedf36b7d395ce81bd124c38fb7",
            "1906bbf4392906ffb8470d9714f91446afa5ec73ae6789bfa5845cffc477c68c"
        };

        public static readonly HashSet<string> Keys12Months = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "50435e1cb0ccddcee3b3f796195fb63221982cb1cfa759fb889fb15fa09bbb17",
            "1c4fc9868f3d9405955100cd2390cbd7a7d6942cdacc7c711d561390803b4c37",
            "6e37854b7734dfa73238fd84f1732020de7b521492833fa3636ab323f488c2ca",
            "8a12b72e65ab9856c0c50b0c4548b3615c2d4af46dae1b77d95ccce82c16c8e1",
            "ddb01fcb34cf6f105195f1e47cf79a02b2eaa4cc30018db33a28c887d39f371a",
            "e0fb1a04e47bf7137654729c07b0df6c0b8b7758490ce048b32672bc854f1ce7",
            "7b598ae294f1d0402d2333c77410124eef8d4551626b90508340586219229061",
            "b48c8315f4104883fb0100c21c121060a6a7c9804c02f4be5b9d8607372dc951",
            "891e1b3c79d8878a34a5b73e4b7ac1b1b77998f5eeaef3a80b2bd9084d399a1d",
            "098c1a83adddc8cdaf21eaf435f1811a899671634031fd73363bf48d673eb144",
            "303e948942d2d3fda77a528eacf8ee81868a5d391a88c3ea20875de8df52eaeb",
            "30599e146fbafb9799b6e1d75f339809c59320d00eb4456f6f36e9210c334bd3",
            "feb082ae5559dc8f4dca956f2ac3cbf6dbe7b0581067b2962bd39f592082e1c9",
            "1f53e6a39f3e55ef9d682eca980b07a2f89c76b2279b85e8fde2c4461a162aec",
            "56a2d966f121dbbfe11bb46fd48a1e0b3d803a2311e61ef5bb9c23dc4e06e090",
            "026b37a6de33c51c8754d803e0d3f7e9c0787a193ec7501818e68d1979f33eb7",
            "fff382f84eb9952f5ce9958d5c3a17a84fb6ceeec2e172fc6954137b6a93a0f4",
            "b57386a340c322b11d95316ee46f2f2ee6da0cbf3692fd7a038df459c653575f",
            "033887b16196f6ab9d6e0300e0808df785ae51a5095871fba02af23f3688e780",
            "9e34532b4138025d6e668af88b3f8ce513f1f41dfe943b61cf8f97446f0b130b",
            "28b49126000f8f3735a741296730970d084edc89471c8d0032741b7cf61179bb",
            "6c803c06cc90df8d9d6ad18c67500dddf50f0d9497e21be6c46b3b0034c0959d",
            "adfe286ce0794a5ef1c280b6654c6435fbde89cfccd9f9a9dcd73d7e095a3c93",
            "b676080e31effd5af078dd3d6f8fe53a1efad9111123c8f038085500cd2857fa",
            "c90c32ddb80ad3a13dbe06826c38b05b5bc250d5e87143708e93bca2fe1c26a5",
            "c5b9eb4d66269113cdccc47145951dddac9cfde5c09391126db6a77f12a7d78a",
            "b3cab66401414feb5e44daaeeed0de6335d037e51790eaf295e388511b992514",
            "1a77442470a44fd9eb43cb921ba363fd5a004e78a6a9b67ae64e57882710cbc6",
            "c9fb665d995189fc8f97dfd06e59c100ee24f67b1ce0f86e94f2ee7bca4e1e93",
            "747099f951df1406691fafb45fe0db25cb68584a16eada152e6d964c7a8db891",
            "afff91654dc2922f58f450c187aad40d479f6d5088afabc497e1f9068ae85682",
            "e55a60f93e6bafda2aa173401cb9c7ee42f290bb7bb4404ef361da74d7445f5a",
            "adea33793ac35def5b223eaf0f200861dad084222c013826636e747cd565114c",
            "91c20d042d3e55bd8a8b3088148ce6d29579ebf54ba04a82c95a15d744464e8a",
            "f6314ea05c6f50b1d2de935831aa2bce6587bc72b9cebd82f2190401bb59769a",
            "0f45f1f472420395f49f08fff54df18d17316d0631daa2725fd821ce9127635f",
            "42cbf6e2e560d24ac9d6dc5092dc1fc87def8625748f83739411ea5a8de9fb39",
            "efd241e36dda243ac99b0ef42739fff1d6e8f4d0598c2d094660a11aa5c42e56",
            "e400f33115a1c584767a805cae7aadc9a9830fa1cc7abd86896fed870d0fea51",
            "5fa85e2db08286279ef37a14767ab9fc9ab4e63340edb7d02199cd916a90ca55",
            "6183b8501a4c95f6862dc6b96047062bbda6da98a6d471edbd2fa84bf7f976c7",
            "75a9e440140e1c3bd8dc6f8c32de3ac072aef65b68e1702ddd4713a17e90ad31",
            "5291e2c5d7871e63bf329c6b2dd5e6a5b042bc0e3d191929884224e3dc3e63ce",
            "9e66ae040fe1d1a39c631a28311a7370ed0cf9582c5fb7da9295d0bbaa8e2483",
            "a5ca7a59f322d5e84266e31756591a493d203415ce1d7ac743d482b558cd0ad4",
            "275ab64bc0b2928b5264b26a12915bed9f56641df8d1628c57a7e3ea7bb1bf0a",
            "03921852d0c391e3463efd5a522aee93cdea2e0110f51ac208f735bf7f9168e3",
            "432b76bed5da3b3a8913a149504b1fb5095313727334edd49c0e4b92e580bf4c",
            "cd52af10fba77d1f9ce209643d2ce93ab0b675510007b03bc21453f8d07d5e70",
            "edda8aeb755982f5cd12ac836bf13f899329ab1b1c8aa5a4ba0ec11be44858a7"
        };

        static LicenseManager()
        {
            UsedKeyHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadLicense();
        }

        private static string GetHashSha256(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input.Trim());
            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hashBytes = sha.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static void LoadLicense()
        {
            string encryptedData = "";

            // 1. Try reading from AppData file
            if (File.Exists(LicenseFilePath))
            {
                try
                {
                    encryptedData = File.ReadAllText(LicenseFilePath);
                }
                catch { }
            }

            // 2. If not in AppData, try Registry
            if (string.IsNullOrEmpty(encryptedData))
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                    {
                        if (key != null)
                        {
                            object val = key.GetValue(RegistryValueName);
                            if (val != null)
                            {
                                encryptedData = val.ToString();
                            }
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(encryptedData))
            {
                ResetState();
                return;
            }

            // Decrypt license string
            string plainText = LicenseEncryption.Decrypt(encryptedData);
            if (string.IsNullOrEmpty(plainText))
            {
                ResetState();
                return;
            }

            // Parse key-value structure
            try
            {
                string activeKey = null;
                DateTime? actDate = null;
                DateTime? expDate = null;
                DateTime? lastRun = null;
                HashSet<string> usedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string[] lines = plainText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    int idx = line.IndexOf('=');
                    if (idx < 0) continue;
                    string name = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();

                    if (name.Equals("ActiveKey", StringComparison.OrdinalIgnoreCase))
                    {
                        activeKey = val;
                    }
                    else if (name.Equals("ActivationDate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(val))
                        {
                            DateTime temp;
                            if (DateTime.TryParse(val, out temp)) actDate = temp;
                        }
                    }
                    else if (name.Equals("ExpirationDate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(val))
                        {
                            DateTime temp;
                            if (DateTime.TryParse(val, out temp)) expDate = temp;
                        }
                    }
                    else if (name.Equals("LastRunDate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(val))
                        {
                            DateTime temp;
                            if (DateTime.TryParse(val, out temp)) lastRun = temp;
                        }
                    }
                    else if (name.Equals("UsedKeys", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(val))
                        {
                            string[] hashes = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string h in hashes)
                            {
                                usedHashes.Add(h.Trim());
                            }
                        }
                    }
                }

                ActiveKey = activeKey;
                ActivationDate = actDate;
                ExpirationDate = expDate;
                LastRunDate = lastRun;
                UsedKeyHashes = usedHashes;
            }
            catch
            {
                ResetState();
            }
        }

        private static void ResetState()
        {
            ActiveKey = null;
            ActivationDate = null;
            ExpirationDate = null;
            LastRunDate = null;
            UsedKeyHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static void SaveLicense()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("ActiveKey={0}", ActiveKey ?? ""));
            sb.AppendLine(string.Format("ActivationDate={0}", ActivationDate.HasValue ? ActivationDate.Value.ToString("o") : ""));
            sb.AppendLine(string.Format("ExpirationDate={0}", ExpirationDate.HasValue ? ExpirationDate.Value.ToString("o") : ""));
            sb.AppendLine(string.Format("LastRunDate={0}", LastRunDate.HasValue ? LastRunDate.Value.ToString("o") : ""));
            sb.AppendLine(string.Format("UsedKeys={0}", string.Join(",", UsedKeyHashes)));

            string plainText = sb.ToString();
            string encryptedData = LicenseEncryption.Encrypt(plainText);

            // Save to AppData file
            try
            {
                if (!Directory.Exists(AppDataDir))
                {
                    Directory.CreateDirectory(AppDataDir);
                }
                File.WriteAllText(LicenseFilePath, encryptedData);
            }
            catch { }

            // Save to Windows Registry
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue(RegistryValueName, encryptedData, RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }

        public static bool IsLicenseValid(out string reason)
        {
            if (string.IsNullOrEmpty(ActiveKey))
            {
                reason = "Application is not activated. Please enter a product key.";
                return false;
            }

            if (!ExpirationDate.HasValue)
            {
                reason = "License is invalid. Please contact the developer.";
                return false;
            }

            // Check for system clock tampering (24-hour grace period)
            if (LastRunDate.HasValue && DateTime.Now < LastRunDate.Value.AddDays(-1))
            {
                reason = "System clock tampering detected. Please restore your correct system date and time.";
                return false;
            }

            if (DateTime.Now >= ExpirationDate.Value)
            {
                reason = string.Format("License expired on {0}. Please enter a new product key or contact developer for key.", ExpirationDate.Value.ToString("yyyy-MM-dd"));
                return false;
            }

            // Update LastRunDate to the current time since validation succeeded
            LastRunDate = DateTime.Now;
            SaveLicense();

            reason = "License is valid.";
            return true;
        }

        public static bool IsExpiringSoon(out int daysRemaining)
        {
            daysRemaining = 0;
            if (!ExpirationDate.HasValue) return false;

            TimeSpan diff = ExpirationDate.Value - DateTime.Now;
            daysRemaining = (int)Math.Ceiling(diff.TotalDays);

            return daysRemaining > 0 && daysRemaining <= 30;
        }

        public static bool ActivateKey(string rawKey, out string errorMessage)
        {
            errorMessage = "";
            string key = rawKey.Trim();

            if (string.IsNullOrEmpty(key))
            {
                errorMessage = "Product key cannot be empty.";
                return false;
            }

            string hash = GetHashSha256(key);

            // Check if already used
            if (UsedKeyHashes.Contains(hash))
            {
                errorMessage = "This product key has already been used and cannot be reused.";
                return false;
            }

            int months = 0;
            if (Keys6Months.Contains(hash))
            {
                months = 6;
            }
            else if (Keys12Months.Contains(hash))
            {
                months = 12;
            }
            else
            {
                errorMessage = "Invalid product key. Please contact developer for key.";
                return false;
            }

            // Success: Activate
            ActiveKey = key;
            ActivationDate = DateTime.Now;
            ExpirationDate = DateTime.Now.AddMonths(months);
            LastRunDate = DateTime.Now;
            UsedKeyHashes.Add(hash);

            SaveLicense();
            return true;
        }
    }

    public static class LicenseEncryption
    {
        // 16 bytes key and IV for AES-128
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("IrasSpotPosLic99");
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("IrasSpotPosIV123");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = Iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = Iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
