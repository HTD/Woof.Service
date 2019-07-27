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
        /// Creates new <see cref="WebServiceHost"/> instance.
        /// </summary>
        /// <param name="baseAddresses">Base addresses.</param>
        /// <param name="allowOrigins">One or more origins separated with a semicolon and optional whitespace.</param>
        /// <param name="allowCredentials">If set true, authentication cookie can be passed to the service.</param>
        public WebServiceHost(Uri[] baseAddresses, string allowOrigins = null, bool allowCredentials = false) : base(typeof(T), baseAddresses) {
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
            if (allowOrigins != null) endPoint.EndpointBehaviors.Add(new CorsSupportBehavior(allowOrigins, allowCredentials));
            endPoint.EndpointBehaviors.Add(new BrowserCompatibilityBehavior());
            AddServiceEndpoint(endPoint);
        }

        /// <summary>
        /// Creates new <see cref="WebServiceHost"/> instance.
        /// </summary>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="allowOrigins">One or more origins separated with a semicolon and optional whitespace.</param>
        /// <param name="allowCredentials">If set true, authentication cookie can be passed to the service.</param>
        public WebServiceHost(Uri baseAddress, string allowOrigins = null, bool allowCredentials = false) : this(new[] { baseAddress }, allowOrigins, allowCredentials) { }

    }

}