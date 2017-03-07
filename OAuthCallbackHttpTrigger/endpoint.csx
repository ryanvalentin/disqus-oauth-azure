#r Newtonsoft.Json

using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

private static HttpResponseMessage GetResponse(string jsonString, string state = "")
{
    var response = new HttpResponseMessage();
    dynamic authData = JObject.Parse(jsonString);

    StringBuilder tmpl = new StringBuilder();
    tmpl.Append("<html>");
    tmpl.Append("<head>");
    tmpl.Append("<title>You're logged in</title>");
    tmpl.Append("</head>");
    tmpl.Append("<body>");
    tmpl.Append("<div>");

    try
    {
        // Currently logged-in user
        tmpl.Append("<p>");
        tmpl.AppendFormat("<img width=\"92\" height=\"92\" src=\"https://disqus.com/api/users/avatars/{0}.jpg\">", authData.username);
        tmpl.Append("<br />");
        tmpl.AppendFormat("You're logged in as <a href=\"https://disqus.com/by/{0}/\" target=\"_blank\">{1}</a>.", authData.username, authData.username)
        tmpl.Append("</p>");
    }
    catch (System.Exception)
    {
        // Username wasn't in response and was likely an error.
        tmpl.Append("<p>");
        tmpl.AppendFormat("There was an error logging you in.");
        tmpl.Append("</p>");
    }

    // Return to app button
    tmpl.Append("<p>");
    tmpl.AppendFormat("<a href=\"disqusoauthexample://authorization?payload={0}&state={1}\">Back to app</a>", Uri.EscapeDataString(jsonString), state);
    tmpl.Append("</p>");

    tmpl.Append("</div>");
    tmpl.Append("</body>");
    tmpl.Append("</html>");

    response.StatusCode = HttpStatusCode.OK;
    response.Content = new StringContent(tmpl.ToString());
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
// https://disqus.com/api/oauth/2.0/authorize/?client_id=YOUR_API_KEY&scope=read,write,email&response_type=code&state=ANYTHING
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    string disqusApiKey = ConfigurationManager.AppSettings["DISQUS_API_KEY"];
    string disqusApiSecret = ConfigurationManager.AppSettings["DISQUS_API_SECRET"];

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
                return GetResponse("{\"response\": {}}");
            default:
                // Some other error occurred, return it
                return GetResponse("{\"error\": \"" + error + "\"}");
        }
    }

    string code = GetParam(queryParams, "code");
    string state = GetParam(queryParams, "state");
    if (String.IsNullOrEmpty(code) || String.IsNullOrEmpty(state))
        return GetResponse("{}");

    using (var client = new HttpClient())
    {
        var reqUri = req.RequestUri;
        string redirectUri = String.Format("{0}://{1}{2}", reqUri.Scheme, reqUri.Host, reqUri.AbsolutePath);
        var content = new FormUrlEncodedContent(new []
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", disqusApiKey),
            new KeyValuePair<string, string>("client_secret", disqusApiSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("state", state)
        });
        var response = await client.PostAsync("https://disqus.com/api/oauth/2.0/access_token/", content);

        string jsonContent = await response.Content.ReadAsStringAsync();

        return GetResponse(jsonContent, state);
    }
}
