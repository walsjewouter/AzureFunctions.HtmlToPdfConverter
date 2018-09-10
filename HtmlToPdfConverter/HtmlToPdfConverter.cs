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
                    string pageOrientationValue = req.Headers.GetValues("PageOrientation").First();
                    log.Info($"PageOrientation header found with value: {pageOrientationValue}");
                    pageOrientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), pageOrientationValue);

                }
                catch (Exception)
                {
                    log.Warning("Exception during PageOrientation parsing");
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid page orientation specified");
                }
            }
            else
            {
                log.Verbose($"PageOrientation header NOT found using default value");
            }

            PageSize pageSize = PageSize.Default;
            if (req.Headers.Contains("PageSize"))
            {
                try
                {
                    string pageSizeValue = req.Headers.GetValues("PageSize").First();
                    log.Info($"PageSize header found with value: {pageSizeValue}");
                    pageSize = (PageSize)Enum.Parse(typeof(PageSize), pageSizeValue);
                }
                catch (Exception)
                {
                    log.Warning("Exception during PageSize parsing");
                    return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid page size specified");
                }
            }
            else
            {
                log.Verbose($"PageSize header NOT found using default value");
            }

            int marginLeft = 15;
            int marginRight = 15;
            int marginTop = 15;
            int marginBottom = 15;
            if (req.Headers.Contains("Margin"))
            {
                int margin;
                string marginValue = req.Headers.GetValues("Margin").First();
                log.Info($"Margin header found with value: {marginValue}");
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
            else
            {
                log.Verbose($"Margin header NOT found using value of 15");
            }

            try
            {
                log.Verbose($"Instantiating converter");
                var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter();
                htmlToPdf.Orientation = pageOrientation;
                htmlToPdf.Size = pageSize;
                htmlToPdf.Margins = new PageMargins() { Left = marginLeft, Right = marginRight, Top = marginTop, Bottom = marginBottom };

                log.Info($"Generate Pdf");
                var pdfBytes = htmlToPdf.GeneratePdf(document);

                log.Verbose($"Creating response");
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "Matchreport.pdf" };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                log.Info("HtmlToPdfConverter process finished.");
                return response;
            }
            catch (Exception ex)
            {
                log.Warning("Exception during PDF and response creation");
                return req.CreateResponse(HttpStatusCode.BadRequest, $"Exception during PDF creation, message {ex.Message}");
            }
        }
    }
}
