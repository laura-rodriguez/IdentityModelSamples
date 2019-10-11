using IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WpfSample
{
    public class DefaultSystemBrowser : IBrowser
    {
        private BrowserOptions _options = null;
        public DefaultSystemBrowser()
        {
        }

        public static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return null;
            }

            using (var body = request.InputStream)
            {
                using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        public async Task<BrowserResult> InvokeAsync(BrowserOptions options)
        {
            _options = options;

            string redirectURI = options.EndUrl;
            Debug.WriteLine("redirect URI: " + redirectURI);

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            http.Start();
            Debug.WriteLine("Listening...");
            Debug.WriteLine($"Start URL: {options.StartUrl}");

            // Opens request in the browser.
            System.Diagnostics.Process.Start(options.StartUrl);

            // wait for the authorization response.
            var context = await http.GetContextAsync();

            var formData = GetRequestPostData(context.Request);

            // sends an HTTP response to the browser.
            var response = context.Response;
            
            // Do we need the authority here? 
            string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=http://www.okta.com'></head><body>Please return to the app.</body></html>");

            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Close();

            Debug.WriteLine($"Form Data: {formData}");

            return new BrowserResult()
            {
                ResultType = BrowserResultType.Success,
                Response = formData
            };
        }
    }
}
