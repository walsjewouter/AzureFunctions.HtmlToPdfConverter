using iText.Html2pdf;
using iText.Html2pdf.Attach.Impl.Layout;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace HtmlToPdfConverter
{
    public static class HtmlToPdfConverter
    {
        [FunctionName("HtmlToPdfConverter")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("HtmlToPdfConverter processing a request.");

            string html = await req.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(html))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a document content on the request data");
            }

            var pageSize = GetPageSize(req.Headers, PageSize.Default, log);

            if (req.Headers.Contains("PageOrientation"))
            {
                string pageOrientationValue = req.Headers.GetValues("PageOrientation").First().ToLowerInvariant();
                log.Info($"PageOrientation header found with value: {pageOrientationValue}");
                pageSize = pageOrientationValue == "landscape" ? pageSize.Rotate() : pageSize;
            }
            else
            {
                log.Verbose($"PageOrientation header NOT found using default value");
            }

            int leftMargin = GetHeaderValueAsInt(req.Headers, "LeftMargin", 36, log);
            int rightMargin = GetHeaderValueAsInt(req.Headers, "RightMargin", 36, log);
            int topMargin = GetHeaderValueAsInt(req.Headers, "TopMargin", 36, log);
            int bottomMargin = GetHeaderValueAsInt(req.Headers, "BottomMargin", 36, log);

            try
            {
                log.Verbose($"Converting HTml into PDF");
                var pdfBytes = Convert(html, pageSize, leftMargin, rightMargin, topMargin, bottomMargin, log);

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

        private static byte[] Convert(string html, PageSize pageSize, int leftMargin, int rightMargin, int topMargin, int bottomMargin, TraceWriter log)
        {
            byte[] pdfBytes;
            using (var stream = new MemoryStream())
            {
                log.Verbose($"Creating PDF document, using pdf writer stream on memory stream");
                var pdf = new PdfDocument(new PdfWriter(stream));

                log.Verbose($"Setting default page size");
                pdf.SetDefaultPageSize(pageSize);

                log.Verbose($"Creating document from PDF");
                var document = new Document(pdf);

                log.Verbose($"Setting margins");
                document.SetMargins(topMargin, rightMargin, bottomMargin, leftMargin);

                log.Verbose($"Converting HTML into elements");
                var elements = HtmlConverter.ConvertToElements(html);

                log.Verbose($"Adding elements to the document");
                foreach (IElement element in elements)
                {
                    if (element is HtmlPageBreak)
                    {
                        document.Add((HtmlPageBreak)element);
                    }
                    else
                    {
                        document.Add((IBlockElement)element);
                    }
                }

                log.Verbose($"Closing document");
                document.Close();

                log.Verbose($"Write memory stream to byte array");
                pdfBytes = stream.ToArray();

                pdf.Close();
            }

            return pdfBytes;
        }

        private static PageSize GetPageSize(HttpRequestHeaders headers, PageSize defaultSize, TraceWriter log)
        {
            var pageSize = defaultSize;
            if (headers.Contains("PageSize"))
            {
                string pageSizeValue = headers.GetValues("PageSize").First().ToUpperInvariant();
                log.Info($"PageSize header found with value: {pageSizeValue}");

                var field = typeof(PageSize).GetFields(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(f => f.Name.Equals(pageSizeValue));
                if (field != null)
                {
                    pageSize = (PageSize)field.GetValue(null);
                }
                else
                {
                    log.Error($"Unsupported PageSize header value found, using default value");
                }
            }
            else
            {
                log.Verbose($"PageSize header NOT found using default value");
            }

            return pageSize;
        }

        private static int GetHeaderValueAsInt(HttpRequestHeaders headers, string key, int defaultValue, TraceWriter log)
        {
            int value = defaultValue;
            if (headers.Contains(key))
            {
                string marginValue = headers.GetValues(key).First();
                log.Info($"{key} header found with value: {marginValue}");
                if (!int.TryParse(marginValue, out value) || value < 0)
                {
                    log.Error($"Unable to parse header value to (positive) integer");
                    value = defaultValue;
                }
            }
            else
            {
                log.Verbose($"{key} header NOT found using default value of {defaultValue}");
            }

            return value;
        }
    }
}
