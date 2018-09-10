using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HtmlToPdfConverter
{
    public static class HtmlToPdfConverter
    {
        [FunctionName("HtmlToPdfConverter")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("HtmlToPdfConverter processing a request.");

            // parse query parameter
            string document = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "document", true) == 0)
                .Value;

            if (string.IsNullOrWhiteSpace(document))
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Please pass a document on the request body");
            }

            var dummy = string.Empty;

            //return new FileContentResult(null, "application/pdf");
            return req.CreateResponse(HttpStatusCode.OK, dummy, "application/pdf");
        }
    }
}
