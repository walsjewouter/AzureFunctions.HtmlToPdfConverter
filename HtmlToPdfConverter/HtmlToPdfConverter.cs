using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using NReco.PdfGenerator;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HtmlToPdfConverter
{
    public static class HtmlToPdfConverter
    {
        [FunctionName("HtmlToPdfConverter")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("HtmlToPdfConverter processing a request.");

            string document = await req.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(document))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a document content on the request data");
            }

            PageOrientation pageOrientation = PageOrientation.Default;
            if (req.Headers.Contains("PageOrientation"))
            {
                try
                {
                    pageOrientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), req.Headers.GetValues("PageOrientation").First());
                }
                catch (Exception)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid page orientation specified");
                }
            }

            PageSize pageSize = PageSize.Default;
            if (req.Headers.Contains("PageSize"))
            {
                try
                {
                    pageSize = (PageSize)Enum.Parse(typeof(PageSize), req.Headers.GetValues("PageSize").First());
                }
                catch (Exception)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid page size specified");
                }
            }

            int marginLeft = 15;
            int marginRight = 15;
            int marginTop = 15;
            int marginBottom = 15;
            if (req.Headers.Contains("Margin"))
            {
                int margin;
                if (int.TryParse(req.Headers.GetValues("Margin").First(), out margin) && margin >= 0)
                {
                    marginLeft = margin;
                    marginRight = margin;
                    marginTop = margin;
                    marginBottom = margin;
                }
                else
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid margin specified");
                }
            }

            try
            {
                var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter();
                htmlToPdf.Orientation = pageOrientation;
                htmlToPdf.Size = pageSize;
                htmlToPdf.Margins = new PageMargins() { Left = marginLeft, Right = marginRight, Top = marginTop, Bottom = marginBottom };

                var pdfBytes = htmlToPdf.GeneratePdf(document);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "Matchreport.pdf" };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return response;
            }
            catch (Exception ex)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Exception during PDF creation, message {ex.Message}");
            }
        }
    }
}
