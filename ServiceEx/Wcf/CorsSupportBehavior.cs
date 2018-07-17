using System;
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

        private class CorsMessageInspector : IDispatchMessageInspector {

            /// <summary>
            /// If set, the requests will be allowed only for origins specified, otherwise for all
            /// </summary>
            public string[] AccessControlAllowOrigin { get; set; }

            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) {
                HttpRequestMessageProperty httpRequest = request.Properties["httpRequest"] as HttpRequestMessageProperty;
                // Check if the client sent an "OPTIONS" request
                if (httpRequest != null && httpRequest.Method == "OPTIONS") // Store the requested headers
                    OperationContext.Current.Extensions.Add(new PreflightDetected(httpRequest.Headers["Access-Control-Request-Headers"]));
                return httpRequest;
            }

            public void BeforeSendReply(ref Message reply, object correlationState) {
                HttpRequestMessageProperty httpRequest = correlationState as HttpRequestMessageProperty;
                HttpResponseMessageProperty httpResponse = null;
                if (reply == null) {
                    // This will usually be for a preflight response
                    reply = Message.CreateMessage(MessageVersion.None, null);
                    httpResponse = new HttpResponseMessageProperty();
                    reply.Properties[HttpResponseMessageProperty.Name] = httpResponse;
                    httpResponse.StatusCode = HttpStatusCode.OK;
                } else if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                    httpResponse = reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;

                PreflightDetected preflightRequest = OperationContext.Current.Extensions.Find<PreflightDetected>();
                if (preflightRequest != null) {
                    // Add allow HTTP headers to respond to the preflight request
                    if (preflightRequest.RequestedHeaders == string.Empty)
                        httpResponse.Headers.Add("Access-Control-Allow-Headers", "Accept");
                    else
                        httpResponse.Headers.Add("Access-Control-Allow-Headers", preflightRequest.RequestedHeaders + ", Accept");

                    httpResponse.Headers.Add("Access-Control-Allow-Methods", "*");
                }

                // Add allow-origin header to each response message, because client expects it
                // If AccessControlAllowOrigin is set to allowed origins array the request origin is tested against it
                string originHeader = httpRequest.Headers["Origin"];
                
                string originString = null;
                if (httpResponse != null) {
                    if (AccessControlAllowOrigin == null || String.IsNullOrEmpty(originHeader) || originHeader == "null") {
                        httpResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                    } else {
                        foreach (var i in AccessControlAllowOrigin) if (i == "*" || originHeader == i) { originString = i; break; }
                        if (originString == null) {
                            httpResponse.StatusCode = HttpStatusCode.Forbidden;
                            httpResponse.StatusDescription = "Origin not allowed";
                        }
                        else if (httpResponse != null) {
                            httpResponse.Headers.Add("Access-Control-Allow-Origin", originString);
                            httpResponse.Headers.Add("Access-Control-Allow-Credentials", "true");
                        }
                    }
                }
            }
        }

        private CorsMessageInspector MessageInspector { get; } = new CorsMessageInspector();

        /// <summary>
        /// Creates CORS support behavior for specified one or more origins (separated with semicolon).
        /// </summary>
        /// <param name="origins">One or more origins separated with semicolon and optional white space.</param>
        public CorsSupportBehavior(string origins = "*") => AccessControlAllowOrigin = RxOriginSeparator.Split(origins);

        /// <summary>
        /// Empty.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="bindingParameters"></param>
        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters) { }

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
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CorsMessageInspector() { AccessControlAllowOrigin = AccessControlAllowOrigin });
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