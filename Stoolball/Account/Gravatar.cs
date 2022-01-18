using System;
using System.Globalization;

namespace Stoolball.Account
{
    /// <summary>
    /// A Gravatar representing an email address
    /// </summary>
    public class Gravatar
    {
        private readonly int _size = 50;

        public Gravatar(string emailAddress)
        {
            Url = BuildGravatarUrl(emailAddress);
        }

        /// <summary>
        /// Gets the URL of the Gravatar
        /// </summary>
        public Uri Url { get; private set; }

        /// <summary>
        /// Size in pixels. Gravatars are square.
        /// </summary>
        public int Size { get { return _size; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "<Pending>")]
        private Uri BuildGravatarUrl(string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                throw new ArgumentException($"{nameof(emailAddress)} must not be null or an empty string");
            }

            //Get email to lower
            var emailToHash = emailAddress.ToLowerInvariant();

            // Create a new instance of the MD5CryptoServiceProvider object.
            using (var md5Hasher = System.Security.Cryptography.MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(emailToHash));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                var sBuilder = new System.Text.StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                var hashedEmail = sBuilder.ToString();  // Return the hexadecimal string.

                //Return the gravatar URL
                return new Uri($"https://s.gravatar.com/avatar/{hashedEmail}?s={Size}");
            }
        }
    }
}