using System.Net;
using System.Net.Http;

private static HttpResponseMessage GetResponse(Dictionary<string, string> payload, HttpStatusCode status)
{
    var response = new HttpResponseMessage();
    StringBuilder sb = new StringBuilder();
    sb.Append("<html><body>");
    foreach (var kv in payload)
    {
        sb.AppendFormat("<p>Adding '{0}' with value '{1}'</p>", kv.Key, kv.Value);
    }
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

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var response = new HttpResponseMessage();

    // https://disqus.com/api/oauth/2.0/authorize/?client_id=hDuMtiXLQn5TarhIlbB9Q8hpYYvDRS2QPa64U31QIi1DVu5pB4epANLFQeey4HIB&scope=read,write,email&response_type=code
    log.Info("C# HTTP trigger function processed a request.");

    // Parse query parameters
    var queryParams = req.GetQueryNameValuePairs();

    string error = GetParam("error");

    if (!String.IsNullOrEmpty(error))
    {
        // Figure out if user simply canceled
        switch (error)
        {
            case "access_denied":
                // User said "no thanks"
                return GetResponse(new Dictionary<string, string>(), HttpStatusCode.NoContent);
            default:
                // Some other error occurred, return it
                return GetResponse(new Dictionary<string, string>() { { "error", error } }, HttpStatusCode.BadRequest);
        }
    }

    string code = GetParam("code");

    if (String.IsNullOrEmpty(code))
    {
        return GetResponse(new Dictionary<string, string>()
            {
                { "error", "There was an error logging in" }
            },
            HttpStatusCode.BadRequest);
    }

    return GetResponse(new Dictionary<string, string>() { { "code", code } }, HttpStatusCode.OK);
}
