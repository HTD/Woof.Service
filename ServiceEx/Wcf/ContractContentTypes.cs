using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace Woof.ServiceEx.Wcf {

    /// <summary>
    /// Determines response content type and format from request.
    /// </summary>
    public class ContractContentTypes {

        #region Content-Type constants

        /// <summary>
        /// JSON MIME type.
        /// </summary>
        public const string Json = "application/json";

        /// <summary>
        /// XML MIME type.
        /// </summary>
        public const string Xml = "application/xml";

        /// <summary>
        /// Plain text MIME type.
        /// </summary>
        public const string Text = "text/plain";

        /// <summary>
        /// HTML MIME type.
        /// </summary>
        public const string Html = "text/html";

        /// <summary>
        /// HTML form MIME type.
        /// </summary>
        public const string Form = "application/x-www-form-urlencoded";

        /// <summary>
        /// UTF-8 charset part.
        /// </summary>
        public const string Charset = "; charset=UTF-8";

        #endregion

        /// <summary>
        /// Creates contract content types module from web request.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <param name="fallback">Default content type to set if not matched with Accept header.</param>
        public ContractContentTypes(Message request, string fallback = null) {
            var httpRequest = request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
            if (fallback != null) {
                var p = fallback.IndexOf(';');
                Fallback = p > 0 ? fallback.Substring(0, p) : fallback;
            }
            Accepts = httpRequest.Headers["Accept"];
            RequestContentType = httpRequest.Headers["Content-Type"];
            AcceptsAnything = String.IsNullOrEmpty(Accepts) || Accepts.StartsWith("*");
            if (AcceptsAnything && RequestContentType == null) { // IE LT 10 case - we know nothing about the request
                if (RequestContentType == null) {
                    if (fallback != null) {
                        if (fallback.StartsWith(Json)) { AcceptsJson = true; AcceptsAnything = false; AcceptsText = false; } else if (fallback.StartsWith(Xml)) { AcceptsXml = true; AcceptsAnything = false; AcceptsText = false; }
                    }
                }
            } else
                if (!AcceptsAnything) {
                    if (Accepts.StartsWith(Json)) AcceptsJson = true;
                    else if (Accepts.StartsWith(Xml)) AcceptsXml = true;
                    else if (Accepts.StartsWith(Html)) AcceptsHtml = true;
                    else if (Accepts.StartsWith(Text)) AcceptsText = true;
                    else if (RequestContentType != null && RequestContentType.StartsWith(Form)) RequestIsForm = true;
                }
        }

        /// <summary>
        /// Gets Accepts header value.
        /// </summary>
        public string Accepts { get; }

        /// <summary>
        /// Gets a value indicating that the client has no preference about response content type.
        /// </summary>
        public bool AcceptsAnything { get; }

        /// <summary>
        /// Gets a value indicating whether the client accepts plain text response.
        /// </summary>
        public bool AcceptsText { get; }

        /// <summary>
        /// Gets a value indicating whether the client accepts HTML response.
        /// </summary>
        public bool AcceptsHtml { get; }

        /// <summary>
        /// Gets a value indicating whether the client accepts JSON response.
        /// </summary>
        public bool AcceptsJson { get; }

        /// <summary>
        /// Gets a value indicating whether the client accepts XML response.
        /// </summary>
        public bool AcceptsXml { get; }

        /// <summary>
        /// Gets a value indicating whether the request was sent from HTML form.
        /// </summary>
        public bool RequestIsForm { get; }

        /// <summary>
        /// Gets the request's ContentType header value.
        /// </summary>
        public string RequestContentType { get; }

        /// <summary>
        /// Gets the ContentType header value (with charset).
        /// </summary>
        public string ResponseContentType {
            get {
                if (RequestIsForm) return Html + Charset;
                if (AcceptsJson || (AcceptsHtml && Fallback.StartsWith(Json))) return Json + Charset;
                if (AcceptsXml || (AcceptsHtml && Fallback.StartsWith(Xml))) return Xml + Charset;
                if (Fallback != null) return Fallback + Charset;
                return Text + Charset;
            }
        }

        /// <summary>
        /// Gets response message format (JSON/XML if set, null for other)
        /// </summary>
        public WebMessageFormat? Format {
            get {
                if (AcceptsJson || (AcceptsHtml && Fallback.StartsWith(Json))) return WebMessageFormat.Json;
                if (AcceptsXml || (AcceptsHtml && Fallback.StartsWith(Xml))) return WebMessageFormat.Xml;
                return null;
            }
        }

        /// <summary>
        /// Fallback (default) response ContentType cache.
        /// </summary>
        private readonly string Fallback;


    }

}