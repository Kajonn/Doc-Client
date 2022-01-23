using Doc_Client;
using System.Text;
using System.Text.Json;

HttpClient client = new HttpClient();

String server = "https://localhost:7000";
int numUploads = 100;


var postUpload = async (String path) =>
{
    UploadRequest uploadRequest = new();
    uploadRequest.Path = "FILE";
    uploadRequest.User = "user";
    var json = JsonSerializer.Serialize(uploadRequest);
    var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

    HttpResponseMessage postResponse = await client.PostAsync(server + "/upload", data);
    postResponse.EnsureSuccessStatusCode();
    string postResponseBody = await postResponse.Content.ReadAsStringAsync();
    var jsondoc = JsonDocument.Parse(postResponseBody);
    //TODO json parsing direct to objects fails
    
    String identifier = jsondoc.RootElement.GetProperty("identifier").GetString();

    return identifier;
};

var getUploadStatus = async (String identifier) =>
{
    HttpResponseMessage response = await client.GetAsync(server + "/upload/" + identifier);
    response.EnsureSuccessStatusCode();
    string responseBody = await response.Content.ReadAsStringAsync();
    var status = JsonDocument.Parse(responseBody);
    
    UploadStatus uploadStatus = new();
    uploadStatus.Status = (UploadStatus.StatusType)status.RootElement.GetProperty("status").GetInt32();
    uploadStatus.StatusString = status.RootElement.GetProperty("statusString").GetString();
    return uploadStatus;
};

try
{
    List<String> uploadIds = new();
    for (int i = 0; i < numUploads; i++)
    {
        Console.WriteLine("Posting upload " + i);
        uploadIds.Add(await postUpload.Invoke("filenr_" + i));
    }

    Dictionary<String, UploadStatus> uploadStatuses = new();
    while (uploadIds.Count > 0)
    {
        foreach (String uploadId in uploadIds)
        {
            UploadStatus uploadStatus = await getUploadStatus(uploadId);
            if(uploadStatus.Status != UploadStatus.StatusType.Pending)
            {
                Console.WriteLine("Upload finished for " + uploadId);
                Console.WriteLine("Status: " + uploadStatus.Status);
                Console.WriteLine("Message: " + uploadStatus.StatusString);
                uploadStatuses.Add(uploadId, uploadStatus);
            }
        }

        if(uploadIds.RemoveAll(s => uploadStatuses.ContainsKey(s)) > 0)
        {
            Console.WriteLine("Uploads remaining: " + uploadIds.Count);
        }

        Thread.Sleep(100);
    }

}
catch (Exception e)
{
    Console.WriteLine("\nException Caught!");
    Console.WriteLine("Message :{0} ", e.Message);
}