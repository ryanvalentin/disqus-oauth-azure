using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

private static HttpResponseMessage GetResponse(string jsonString)
{
    var response = new HttpResponseMessage();

    StringBuilder sb = new StringBuilder();
    sb.Append("<html><body>");
    sb.Append(jsonString);
    sb.Append("</body></html>");

    response.Content = new StringContent(sb.ToString());
    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

    return response;
}

private static string GetParam(IEnumerable<KeyValuePair<string, string>> queryParams, string key)
{
    return queryParams
        .FirstOrDefault(q => String.Compare(q.Key, key, true) == 0)
        .Value;
}

// Make a request starting here:
// https://disqus.com/api/oauth/2.0/authorize/?client_id=YOUR_API_KEY&scope=read,write,email&response_type=code
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    string disqusApiKey = ConfigurationManager.AppSettings["DISQUS_API_KEY"];
    string disqusApiSecret = ConfigurationManager.AppSettings["DISQUS_API_SECRET"];

    log.Info("Processing OAuth callback...");
    log.Info("Request URI: " + req.RequestUri.OriginalString);
    log.Info("API Key: " + disqusApiKey);
    log.Info("Secret Key: " + disqusApiSecret);

    // Parse query parameters
    var queryParams = req.GetQueryNameValuePairs();

    string error = GetParam(queryParams, "error");

    if (!String.IsNullOrEmpty(error))
    {
        // Figure out if user simply canceled
        switch (error)
        {
            case "access_denied":
                // User said "no thanks"
                return GetResponse("{}");
            default:
                // Some other error occurred, return it
                return GetResponse("{\"error\": \"" + error + "\"}");
        }
    }

    string code = GetParam(queryParams, "code");

    if (String.IsNullOrEmpty(code))
        return GetResponse("{}");

    using (var client = new HttpClient())
    {
        var content = new FormUrlEncodedContent(new []
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", disqusApiKey),
            new KeyValuePair<string, string>("client_secret", disqusApiSecret),
            new KeyValuePair<string, string>("redirect_uri", "https://dsqoauthexample.azurewebsites.net/api/OAuthCallbackHttpTrigger/"),
            new KeyValuePair<string, string>("code", code)
        });
        var response = await client.PostAsync("https://disqus.com/api/oauth/2.0/access_token/", content);

        string jsonContent = await response.Content.ReadAsStringAsync();

        return GetResponse(jsonContent);
    }
}
