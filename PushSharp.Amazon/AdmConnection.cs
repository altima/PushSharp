using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using PushSharp.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace PushSharp.Amazon
{
    public class AdmServiceConnectionFactory : IServiceConnectionFactory<AdmNotification>
    {
        private AdmAccessTokenManager admAccessTokenManager;

        public AdmServiceConnectionFactory(AdmConfiguration configuration)
        {
            admAccessTokenManager = new AdmAccessTokenManager(configuration);
            Configuration = configuration;
        }

        public AdmConfiguration Configuration { get; private set; }

        public IServiceConnection<AdmNotification> Create()
        {
            return new AdmServiceConnection(Configuration, admAccessTokenManager);
        }
    }

    public class AdmServiceBroker : ServiceBroker<AdmNotification>
    {
        public AdmServiceBroker(AdmConfiguration configuration) : base(new AdmServiceConnectionFactory(configuration))
        {
        }
    }

    public class AdmServiceConnection : IServiceConnection<AdmNotification>
    {
        public AdmServiceConnection(AdmConfiguration configuration, AdmAccessTokenManager accessTokenManager)
        {
            AccessTokenManager = accessTokenManager;
            Configuration = configuration;

            
        }

        public AdmConfiguration Configuration { get; private set; }
        public AdmAccessTokenManager AccessTokenManager { get; private set; }

        public async Task Send(AdmNotification notification)
        {
            var accessToken = await AccessTokenManager.GetAccessToken();

            var http = new PushyHttpClient();

            http.DefaultRequestHeaders.ExpectContinue = false;

            http.DefaultRequestHeaders.Add("X-Amzn-Type-Version", "com.amazon.device.messaging.ADMMessage@1.0");
            http.DefaultRequestHeaders.Add("X-Amzn-Accept-Type", "com.amazon.device.messaging.ADMSendResult@1.0");
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            //http.DefaultRequestHeaders.ConnectionClose = true;
            //http.DefaultRequestHeaders.Remove("connection");

            if (!http.DefaultRequestHeaders.Contains("Authorization")) //prevent double values
                http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);

            var sc = new StringContent(notification.ToJson());
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await http.PostAsync(string.Format(Configuration.AdmSendUrl, notification.RegistrationId), sc);

            // We're done here if it was a success
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var data = await response.Content.ReadAsStringAsync();

            var json = JObject.Parse(data);

            var reason = json["reason"].ToString();

            var regId = notification.RegistrationId;

            if (json["registrationID"] != null)
                regId = json["registrationID"].ToString();

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadGateway: //400
                case HttpStatusCode.BadRequest: //
                    if ("InvalidRegistrationId".Equals(reason, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new DeviceSubscriptionExpiredException(notification)
                        {
                            OldSubscriptionId = regId,
                            ExpiredAt = DateTime.UtcNow
                        };
                    }
                    throw new NotificationException("Notification Failed: " + reason, notification);
                case HttpStatusCode.Unauthorized: //401
                                                  //Access token expired
                    AccessTokenManager.InvalidateAccessToken(accessToken);
                    throw new UnauthorizedAccessException("Access token failed authorization");
                case HttpStatusCode.Forbidden: //403
                    throw new AdmRateLimitExceededException(reason, notification);
                case HttpStatusCode.RequestEntityTooLarge: //413
                    throw new AdmMessageTooLargeException(notification);
                default:
                    throw new NotificationException("Unknown ADM Failure", notification);
            }
        }
    }
}
