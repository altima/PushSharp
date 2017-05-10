using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PushSharp.Core
{
    public class PushyHttpClient : HttpClient
    {
        static HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }
            return handler;
        }

        public PushyHttpClient() : base(GetHandler())
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}