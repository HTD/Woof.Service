﻿using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace Woof.ServiceEx.Wcf {

    /// <summary>
    /// Endpoint behaviour compatible with lame browsers (and other devices) which don't set "Content-Type" and "Accept" header properly.
    /// </summary>
    public class BrowserCompatibilityBehavior : IEndpointBehavior, IDispatchMessageInspector {

        /// <summary>
        /// Default content type set in service contract description
        /// </summary>
        public string EndpointDefaultContentType { get; set; } = "application/json; charset=UTF-8";

        /// <summary>
        /// Content type mapper for incompatible requests from IE LT 10 (if no "Content-Type" or "Accept" headers are set in the request)
        /// </summary>
        public class AutoContentTypeMapper : WebContentTypeMapper {

            /// <summary>
            /// Default content type to match 
            /// </summary>
            public string DefaultContentType { get; set; }

            /// <summary>
            /// If let's say old IE doesn't provide Content-Type header via CORS, we have to guess
            /// </summary>
            /// <param name="contentType"></param>
            /// <returns></returns>
            public override WebContentFormat GetMessageFormatForContentType(string contentType) {
                if (contentType.Contains("octet") && DefaultContentType != null) {
                    if (DefaultContentType.StartsWith(ContractContentTypes.Json)) return WebContentFormat.Json;
                    if (DefaultContentType.StartsWith(ContractContentTypes.Xml)) return WebContentFormat.Xml;
                }
                return WebContentFormat.Default;
            }

        }

        /// <summary>
        /// Sets response format and content type from request (IMessageInspector implementation)
        /// </summary>
        /// <param name="request"></param>
        /// <param name="channel"></param>
        /// <param name="instanceContext"></param>
        /// <returns></returns>
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) {
            if (request != null) {
                var serviceType = instanceContext.Host.Description.ServiceType;
                var uriTemplateMatch = WebOperationContext.Current.IncomingRequest.UriTemplateMatch;
                if (uriTemplateMatch == null) return null;
                var methodName = (string)uriTemplateMatch.Data;
                var method = serviceType.GetMethods().Where(m => m.Name == methodName && m.IsPublic).SingleOrDefault();
                var rcta = method.GetCustomAttribute<ReturnContentType>(true);
                var returnContentType = rcta?.ContentType;
                var contractContentTypes = new ContractContentTypes(request, returnContentType ?? EndpointDefaultContentType);
                var response = WebOperationContext.Current.OutgoingResponse;
                response.Format = contractContentTypes.Format;
                response.ContentType = contractContentTypes.ResponseContentType;
            }
            return null;
        }

        /// <summary>
        /// Empty code to satisfy interface.
        /// </summary>
        /// <param name="reply"></param>
        /// <param name="correlationState"></param>
        public void BeforeSendReply(ref Message reply, object correlationState) { }

        /// <summary>
        /// Binds AutoContentType mapper if default content type is specified in service contract description (IMessageInspector implementation)
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="bindingParameters"></param>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            => (endpoint.Binding as WebHttpBinding).ContentTypeMapper = new AutoContentTypeMapper { DefaultContentType = EndpointDefaultContentType };
        
        /// <summary>
        /// Empty code to satisfy interface.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="clientRuntime"></param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

        /// <summary>
        /// Adds a message inspector used to determine correct data formats and content types for incompatible browsers
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="endpointDispatcher"></param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            => endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        
        /// <summary>
        /// Empty code to satisfy interface.
        /// </summary>
        /// <param name="endpoint"></param>
        public void Validate(ServiceEndpoint endpoint) { }

    }

    /// <summary>
    /// Sets default method output Content-Type if raw stream is returned by the method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReturnContentType : Attribute {

        /// <summary>
        /// Content-type attribute content.
        /// </summary>
        public readonly string ContentType = null;

        /// <summary>
        /// Creates <see cref="ReturnContentType"/> attribute with specified content.
        /// </summary>
        /// <param name="contentType">Content-type attribute value.</param>
        public ReturnContentType(string contentType = null) => ContentType = contentType;

    }

}