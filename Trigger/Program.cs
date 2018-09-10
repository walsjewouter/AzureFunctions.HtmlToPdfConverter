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

            var c = Console.ReadKey();
            while (c.Key != ConsoleKey.Q)
            {
                if (c.KeyChar == '1' || c.KeyChar == '2' || c.KeyChar == '3')
                {
                    Console.Write($" -> Posting request... ");

                    switch (c.KeyChar)
                    {
                        case '1':
                            DoPost();
                            break;

                        case '2':
                            DoPost("Landscape", "A4", 5);
                            break;

                        case '3':
                            DoPost("Portrait", "A3", 50);
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

        private static async void DoPost(string pageOrientation = null, string pageSize = null, int margin = 0)
        {
            try
            {
                var request = new HttpRequestMessage();
                request.Content = new StringContent($"<div>Hi, the time is now ${DateTime.Now.ToLongTimeString()}</div>", Encoding.UTF8, "text/html");
                request.RequestUri = new Uri("http://localhost:7071/api/HtmlToPdfConverter");
                request.Method = HttpMethod.Post;

                if (pageOrientation != null)
                {
                    request.Headers.Add("PageOrientation", pageOrientation);
                }

                if (pageSize != null)
                {
                    request.Headers.Add("PageSize", pageSize);
                }

                if (margin != 0)
                {
                    request.Headers.Add("Margin", margin.ToString());
                }

                var response = await client.SendAsync(request);

                Console.WriteLine($"Recieved response: {response.StatusCode}");

                File.WriteAllBytes("D:\\output.pdf", await response.Content.ReadAsByteArrayAsync());
            }
            catch (WebException wex)
            {
                Console.WriteLine($"Recieved response: {wex.HResult}");
            }
        }
    }
}
