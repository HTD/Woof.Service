using System;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text.RegularExpressions;

namespace Woof.ServiceEx.Wcf {

    /// <summary>
    /// Makes the endpoint support Cross Origin Resource Sharing
    /// </summary>
    public class CorsSupportBehavior : IEndpointBehavior {

        /// <summary>
        /// Gets or sets allowed origins, default * meaning all origins allowed.
        /// </summary>
        public string[] AccessControlAllowOrigin { get; }

        /// <summary>
        /// Gets or sets value indicating whether passing cookies to the endpoint via CORS is allowed.
        /// </summary>
        public bool AccessControlAllowCredentials { get; }

        /// <summary>
        /// Custom extension used to indicate preflights.
        /// </summary>
        private class PreflightDetected : IExtension<OperationContext> {

            private string _requestedHeaders = null;

            public PreflightDetected(string requestedHeaders) => RequestedHeaders = requestedHeaders;

            public string RequestedHeaders {
                get => _requestedHeaders ?? string.Empty;
                set => _requestedHeaders = value;
            }

            public void Attach(OperationContext owner) {
            }

            public void Detach(OperationContext owner) {
            }

        }

        private class CustomOperationInvoker : IOperationInvoker {

            private IOperationInvoker InnerInvoker = null;

            public bool IsSynchronous => InnerInvoker.IsSynchronous;

            public CustomOperationInvoker(IOperationInvoker innerInvoker) => InnerInvoker = innerInvoker;

            public object[] AllocateInputs() => InnerInvoker.AllocateInputs();

            public object Invoke(object instance, object[] inputs, out object[] outputs) {
                // Check if the unhandled request is due to preflight checks (OPTIONS header)
                if (OperationContext.Current.Extensions.Find<PreflightDetected>() != null) // Override the standard error handling, so the request won't contain an error
                    return outputs = null;
                else // No preflight - probably a missed call (wrong URI or method) 
                    return InnerInvoker.Invoke(instance, inputs, out outputs);
            }

            public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state) =>
                // Not supported - an exception will be thrown
                InnerInvoker.InvokeBegin(instance, inputs, callback, state);

            public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result) =>
                // Not supported - an exception will be thrown
                InnerInvoker.InvokeEnd(instance, out outputs, result);

        }

        /// <summary>
        /// Actual message inspector class.
        /// </summary>
        private class CorsMessageInspector : IDispatchMessageInspector {

            /// <summary>
            /// Gets the assigned <see cref="CorsSupportBehavior"/>.
            /// </summary>
            private CorsSupportBehavior Behavior { get; }

            /// <summary>
            /// Assigns message inspector to base behavior.
            /// </summary>
            /// <param name="behavior">Base <see cref="CorsSupportBehavior"/>.</param>
            public CorsMessageInspector(CorsSupportBehavior behavior) => Behavior = behavior;

            /// <summary>
            /// Adds <see cref="PreflightDetected"/> extensions if HTTP request method is OPTIONS, invoked on each request.
            /// </summary>
            /// <param name="request">HTTP request.</param>
            /// <param name="channel">Not used.</param>
            /// <param name="instanceContext">Not used.</param>
            /// <returns></returns>
            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) {
                HttpRequestMessageProperty httpRequest = request.Properties["httpRequest"] as HttpRequestMessageProperty;
                // Check if the client sent an "OPTIONS" request
                if (httpRequest != null && httpRequest.Method == "OPTIONS") // Store the requested headers
                    OperationContext.Current.Extensions.Add(new PreflightDetected(httpRequest.Headers["Access-Control-Request-Headers"]));
                return httpRequest;
            }

            /// <summary>
            /// Detects preflight responses, sends CORS headers.
            /// </summary>
            /// <param name="reply">Reply message.</param>
            /// <param name="correlationState">Response message.</param>
            public void BeforeSendReply(ref Message reply, object correlationState) {
                HttpRequestMessageProperty httpRequest = correlationState as HttpRequestMessageProperty;
                HttpResponseMessageProperty httpResponse = null;
                if (reply == null) {
                    // This will usually be for a preflight response
                    reply = Message.CreateMessage(MessageVersion.None, null);
                    httpResponse = new HttpResponseMessageProperty();
                    reply.Properties[HttpResponseMessageProperty.Name] = httpResponse;
                    httpResponse.StatusCode = HttpStatusCode.OK;
                }
                else if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                    httpResponse = reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                else return;
                var preflightRequest = OperationContext.Current.Extensions.Find<PreflightDetected>();
                if (preflightRequest != null) { // Add allow HTTP headers to respond to the preflight request
                    if (preflightRequest.RequestedHeaders == string.Empty)
                        httpResponse.Headers.Add("Access-Control-Allow-Headers", "Accept");
                    else
                        httpResponse.Headers.Add("Access-Control-Allow-Headers", preflightRequest.RequestedHeaders + ", Accept");
                    httpResponse.Headers.Add("Access-Control-Allow-Methods", "*");
                }
                // Add allow-origin header to each response message, because client expects it
                // If AccessControlAllowOrigin is set to allowed origins array the request origin is tested against it
                var originHeader = httpRequest.Headers["Origin"];
                if (Behavior.AccessControlAllowOrigin == null || String.IsNullOrEmpty(originHeader) || originHeader == "null") return;
                var matchedOrigin = Behavior.AccessControlAllowOrigin.FirstOrDefault(i => i.Equals(originHeader, StringComparison.OrdinalIgnoreCase) || i == "*");
                if (matchedOrigin == null) {
                    httpResponse.StatusCode = HttpStatusCode.Forbidden;
                    httpResponse.StatusDescription = "Origin not allowed";
                }
                else if (httpResponse != null) {
                    httpResponse.Headers.Add("Access-Control-Allow-Origin", matchedOrigin);
                    httpResponse.Headers.Add("Access-Control-Allow-Credentials", Behavior.AccessControlAllowCredentials.ToString().ToLower());
                }
            }

        }

        /// <summary>
        /// Creates CORS support behavior for specified one or more origins (separated with semicolon).
        /// </summary>
        /// <param name="origins">One or more origins separated with semicolon and optional white space.</param>
        /// <param name="allowCredentials">If set true, authentication cookie can be passed to the service.</param>
        public CorsSupportBehavior(string origins = null, bool allowCredentials = false) {
            AccessControlAllowOrigin = RxOriginSeparator.Split(origins);
            AccessControlAllowCredentials = allowCredentials;
        }

        /// <summary>
        /// Empty.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="bindingParameters"></param>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        /// <summary>
        /// Empty.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="clientRuntime"></param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

        /// <summary>
        /// Registers CORS message inspector, and an operation invoker for undhandled operations.
        /// </summary>
        /// <param name="endpoint">End point.</param>
        /// <param name="endpointDispatcher">Dispatcher.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CorsMessageInspector(this));
            IOperationInvoker invoker = endpointDispatcher.DispatchRuntime.UnhandledDispatchOperation.Invoker;
            endpointDispatcher.DispatchRuntime.UnhandledDispatchOperation.Invoker = new CustomOperationInvoker(invoker);
        }

        /// <summary>
        /// Makes sure that the behavior is applied to an end point with WebHttp binding
        /// </summary>
        /// <param name="endpoint"></param>
        public void Validate(ServiceEndpoint endpoint) {
            if (!(endpoint.Binding is WebHttpBinding))
                throw new InvalidOperationException("The CorsSupportBehavior can only be used in WebHttpBinding endpoints");
        }

        private readonly Regex RxOriginSeparator = new Regex(@"\s*;\s*", RegexOptions.Compiled);

    }

}