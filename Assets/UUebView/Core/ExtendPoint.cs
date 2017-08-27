using System;
using System.Collections.Generic;

namespace UUebViewCore {
    public enum HttpMethod {
        Get
    }

    public struct AutoyaStatus {
        
    }

    public class Autoya {
        public delegate Dictionary<string, string> HttpRequestHeaderDelegate (HttpMethod method, string url, Dictionary<string, string> requestHeader, string data);
		public delegate void HttpResponseHandlingDelegate (string connectionId, Dictionary<string, string> responseHeader, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string, AutoyaStatus> failed);
    }
}