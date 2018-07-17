using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace Woof.ServiceEx.Wcf {

    /// <summary>
    /// Special stand-alone, zero-configuration RESTful WebService host
    /// </summary>
    /// <typeparam name="T">Web service class type.</typeparam>
    public class WebServiceHost<T> : ServiceHost where T : new() {

        /// <summary>
        /// Gets or sets the maximum amount of memory, in bytes, that is allocated for use by the manager of the message buffers that receive messages from the channel.
        /// The default value is 524288 (0x80000) bytes.
        /// </summary>
        public int MaxBufferSize { get; set; } = 524288;

        /// <summary>
        /// Gets or sets the maximum amount of memory, in bytes, that is allocated for use by the manager of the message buffers that receive messages from the channel.
        /// The default value is 524288 (0x80000) bytes.
        /// </summary>
        public int MaxReceivedMessageSize { get; set; } = 524288;

        /// <summary>
        /// Gets one or more origins allowed with CORS support behavior defined in the host constructor.
        /// </summary>
        public string AllowOrigins { get; }

        /// <summary>
        /// Creates new <see cref="WebServiceHost"/> instance.
        /// </summary>
        /// <param name="endpointUri">Endpoint URI.</param>
        /// <param name="allowOrigins">One or more origins separated with a semicolon and optional whitespace.</param>
        public WebServiceHost(Uri endpointUri, string allowOrigins = "*") : base(typeof(T), new[] { endpointUri }) => AllowOrigins = allowOrigins;

        /// <summary>
        /// Configures service endpoint and adds special behaviors to it (WebHttp and CorsSupport).
        /// </summary>
        protected override void OnOpening() {
            var securityMode = BaseAddresses.FirstOrDefault().Scheme == "https" ? WebHttpSecurityMode.Transport : WebHttpSecurityMode.None;
            var receiveTimeout = new TimeSpan(1, 0, 0);
            var defaultWebHttpBehavior = new WebHttpBehavior() {
                AutomaticFormatSelectionEnabled = true,
                DefaultBodyStyle = WebMessageBodyStyle.Wrapped,
                DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                HelpEnabled = false
            };
            var endPoint = new ServiceEndpoint(
                ContractDescription.GetContract(Description.ServiceType),
                new WebHttpBinding(securityMode) {
                    UseDefaultWebProxy = false,
                    ReceiveTimeout = receiveTimeout,
                    MaxBufferSize = MaxBufferSize,
                    MaxReceivedMessageSize = MaxReceivedMessageSize
                },
                new EndpointAddress(BaseAddresses.FirstOrDefault())
            );
            endPoint.EndpointBehaviors.Add(defaultWebHttpBehavior);
            endPoint.EndpointBehaviors.Add(new CorsSupportBehavior(AllowOrigins));
            endPoint.EndpointBehaviors.Add(new BrowserCompatibilityBehavior());
            AddServiceEndpoint(endPoint);
            base.OnOpening();
        }

    }

}