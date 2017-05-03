﻿using System;
using System.Threading.Tasks;
using System.Net.Http;
using PushSharp.Core;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PushSharp.Windows
{
    public class WnsAccessTokenManager
    {
        Task renewAccessTokenTask = null;
        string accessToken = null;
        PushyHttpClient http;
        private DateTime expire;

        public WnsAccessTokenManager(WnsConfiguration configuration)
        {
            http = new PushyHttpClient();
            Configuration = configuration;
        }

        public WnsConfiguration Configuration { get; private set; }

        public async Task<string> GetAccessToken()
        {
            if (expire <= DateTime.UtcNow || accessToken == null)
            {
                if (renewAccessTokenTask == null)
                {
                    Log.Info("Renewing Access Token");
                    renewAccessTokenTask = RenewAccessToken();
                    await renewAccessTokenTask;
                }
                else
                {
                    Log.Info("Waiting for access token");
                    await renewAccessTokenTask;
                }
            }

            return accessToken;
        }

        public void InvalidateAccessToken(string currentAccessToken)
        {
            if (accessToken == currentAccessToken)
                accessToken = null;
        }

        async Task RenewAccessToken()
        {
            var p = new Dictionary<string, string> {
                { "grant_type", "client_credentials" },
                { "client_id", Configuration.PackageSecurityIdentifier },
                { "client_secret", Configuration.ClientSecret },
                { "scope", "notify.windows.com" }
            };

            var result = await http.PostAsync(Configuration.WnsAuthUrl, new FormUrlEncodedContent(p));

            var data = await result.Content.ReadAsStringAsync();

            var token = string.Empty;
            var tokenType = string.Empty;
            var expireTime = 3600;

            try
            {
                var json = JObject.Parse(data);
                token = json.Value<string>("access_token");
                tokenType = json.Value<string>("token_type");
                expireTime = json.Value<int>("expires_in");

            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tokenType))
            {
                accessToken = token;
                expire = DateTime.UtcNow.AddSeconds(expireTime - 60);
            }
            else
            {
                accessToken = null;
                expire = DateTime.UtcNow.AddYears(-1);
                throw new UnauthorizedAccessException("Could not retrieve access token for the supplied Package Security Identifier (SID) and client secret");
            }
        }
    }
}

