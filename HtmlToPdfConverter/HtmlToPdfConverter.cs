using iText.Html2pdf;
using iText.Html2pdf.Attach.Impl.Layout;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace HtmlToPdfConverter;

public class HtmlToPdfConverter
{
    private readonly ILogger<HtmlToPdfConverter> _logger;

    public HtmlToPdfConverter(ILogger<HtmlToPdfConverter> logger)
    {
        _logger = logger;
    }

    [Function("HtmlToPdfConverter")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("HtmlToPdfConverter processing a request.");

        string html = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(html))
        {
            return new BadRequestResult();
        }

        var pageSize = GetPageSize(req.Headers, PageSize.Default);

        if (req.Headers.ContainsKey("PageOrientation"))
        {
            string pageOrientationValue = req.Headers["PageOrientation"].ToString().ToLowerInvariant();
            _logger.LogInformation($"PageOrientation header found with value: {pageOrientationValue}");
            pageSize = pageOrientationValue == "landscape" ? pageSize.Rotate() : pageSize;
        }
        else
        {
            _logger.LogTrace($"PageOrientation header NOT found using default value");
        }

        int leftMargin = GetHeaderValueAsInt(req.Headers, "LeftMargin", 36);
        int rightMargin = GetHeaderValueAsInt(req.Headers, "RightMargin", 36);
        int topMargin = GetHeaderValueAsInt(req.Headers, "TopMargin", 36);
        int bottomMargin = GetHeaderValueAsInt(req.Headers, "BottomMargin", 36);

        try
        {
            _logger.LogTrace($"Converting HTml into PDF");
            var pdfBytes = Convert(html, pageSize, leftMargin, rightMargin, topMargin, bottomMargin);

            _logger.LogTrace($"Creating response");
            var response = new FileContentResult(pdfBytes, "application/octet-stream");

            _logger.LogInformation("HtmlToPdfConverter process finished.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during PDF and response creation");
            return new BadRequestResult();
        }
    }

    private byte[] Convert(string html, PageSize pageSize, int leftMargin, int rightMargin, int topMargin, int bottomMargin)
    {
        byte[] pdfBytes;
        using (var stream = new MemoryStream())
        {
            _logger.LogTrace($"Creating PDF document, using pdf writer stream on memory stream");
            var pdf = new PdfDocument(new PdfWriter(stream));

            _logger.LogTrace($"Setting default page size");
            pdf.SetDefaultPageSize(pageSize);

            _logger.LogTrace($"Creating document from PDF");
            var document = new Document(pdf);

            _logger.LogTrace($"Setting margins");
            document.SetMargins(topMargin, rightMargin, bottomMargin, leftMargin);

            _logger.LogTrace($"Converting HTML into elements");
            var elements = HtmlConverter.ConvertToElements(html);

            _logger.LogTrace($"Adding elements to the document");
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

            _logger.LogTrace($"Closing document");
            document.Close();

            _logger.LogTrace($"Write memory stream to byte array");
            pdfBytes = stream.ToArray();

            pdf.Close();
        }

        return pdfBytes;
    }

    private PageSize GetPageSize(IHeaderDictionary headers, PageSize defaultSize)
    {
        var pageSize = defaultSize;
        if (headers.ContainsKey("PageSize"))
        {
            string pageSizeValue = headers["PageSize"].ToString().ToUpperInvariant();
            _logger.LogInformation($"PageSize header found with value: {pageSizeValue}");

            var field = typeof(PageSize).GetFields(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(f => f.Name.Equals(pageSizeValue));
            if (field != null)
            {
                pageSize = (PageSize)field.GetValue(null);
            }
            else
            {
                _logger.LogError($"Unsupported PageSize header value found, using default value");
            }
        }
        else
        {
            _logger.LogTrace($"PageSize header NOT found using default value");
        }

        return pageSize;
    }

    private int GetHeaderValueAsInt(IHeaderDictionary headers, string key, int defaultValue)
    {
        int value = defaultValue;
        if (headers.ContainsKey(key))
        {
            string marginValue = headers[key].ToString().ToUpperInvariant();
            _logger.LogInformation($"{key} header found with value: {marginValue}");
            if (!int.TryParse(marginValue, out value) || value < 0)
            {
                _logger.LogError($"Unable to parse header value to (positive) integer");
                value = defaultValue;
            }
        }
        else
        {
            _logger.LogTrace($"{key} header NOT found using default value of {defaultValue}");
        }

        return value;
    }
}