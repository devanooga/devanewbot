namespace SlackDotNet.Responses;

public class FileResponse
{
    public bool Ok { get; set; }
    public FileInfo File { get; set; }

    public class FileInfo
    {
        public string Id { get; set; }
    }
}
