using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Trigger
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Press Q to Quit or:");
            Console.WriteLine("1 to post a request for a PDF with: default values");
            Console.WriteLine("2 to post a request for a PDF with: A4 Landscape, margin 5");
            Console.WriteLine("3 to post a request for a PDF with: A3 Portrait, margin 50");
            Console.WriteLine("4 to post a request for a PDF with: Letter Portrait, default margin");

            var c = Console.ReadKey();
            while (c.Key != ConsoleKey.Q)
            {
                if (c.KeyChar == '1' || c.KeyChar == '2' || c.KeyChar == '3' || c.KeyChar == '4')
                {
                    Console.Write($" -> Posting request... ");

                    switch (c.KeyChar)
                    {
                        case '1':
                            DoPost();
                            break;

                        case '2':
                            DoPost("A4", "Landscape", 5);
                            break;

                        case '3':
                            DoPost("A3", "Portrait", 50);
                            break;

                        case '4':
                            DoPost("Letter", "Portrait");
                            break;
                    }

                    Console.WriteLine(" Request posted, awaiting result");
                }
                else
                {
                    Console.WriteLine(" -> Invalid key pressed");
                }

                c = Console.ReadKey();
            }

            Console.WriteLine("\r\n\r\nQuitting\r\n");
        }

        private static async void DoPost(string pageSize = null, string pageOrientation = null, int? margin = null)
        {
            try
            {
                string body = $"<div>Hi, the time is now {DateTime.Now.ToLongTimeString()}</div>";
                var request = new HttpRequestMessage();
                request.Content = new StringContent(body, Encoding.UTF8, "text/html");
                request.RequestUri = new Uri("http://localhost:7071/api/HtmlToPdfConverter");
                request.Method = HttpMethod.Post;

                if (pageSize != null)
                {
                    request.Headers.Add("PageSize", pageSize);
                }

                if (pageOrientation != null)
                {
                    request.Headers.Add("PageOrientation", pageOrientation);
                }

                if (margin.HasValue)
                {
                    request.Headers.Add("LeftMargin", margin.Value.ToString());
                    request.Headers.Add("rightMargin", margin.Value.ToString());
                    request.Headers.Add("TopMargin", margin.Value.ToString());
                    request.Headers.Add("BottomMargin", margin.Value.ToString());
                }

                var response = await client.SendAsync(request);

                string content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Received response: {response.StatusCode}");

                File.WriteAllBytes("output.pdf", await response.Content.ReadAsByteArrayAsync());
            }
            catch (WebException wex)
            {
                Console.WriteLine($"Recieved response: {wex.HResult}");
            }
        }
    }
}
