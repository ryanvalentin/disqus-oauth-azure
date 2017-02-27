using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string code = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "code", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    code = code ?? data?.code;

    return code == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "There was an error logging in")
        : req.CreateResponse(HttpStatusCode.OK, "Will exchange this code for token: " + code);
}
