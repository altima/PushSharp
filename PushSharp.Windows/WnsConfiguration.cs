using System;

namespace PushSharp.Windows
{
    public class WnsConfiguration
    {
        const string WNS_AUTH_URL = "https://login.live.com/accesstoken.srf";

        public WnsConfiguration (string packageName, string packageSecurityIdentifier, string clientSecret)
        {
            PackageName = packageName;
            PackageSecurityIdentifier = packageSecurityIdentifier;
            ClientSecret = clientSecret;

            WnsAuthUrl = WNS_AUTH_URL;
        }

        public string PackageName { get; private set; }
        public string PackageSecurityIdentifier { get; private set; }
        public string ClientSecret { get; private set; }

        public string WnsAuthUrl { get; private set; }

        public void OverwriteAuthUrl(string url)
        {
            WnsAuthUrl = url;
        }
    }
}

