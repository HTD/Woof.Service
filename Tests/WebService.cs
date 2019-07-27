using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Woof.Service.Tests {

    [ServiceContract]
    class WebService {

        [OperationContract, WebGet]
        public TestType Hello() => new TestType { DateTest = DateTime.Now };

        [OperationContract, WebGet(UriTemplate = "add/{a}/{b}")]
        public int Add(string a, string b) => int.Parse(a) + int.Parse(b);

        [DataContract]
        public struct TestType {

            [DataMember]
            public DateTime? DateTest;

        }


    }

}
