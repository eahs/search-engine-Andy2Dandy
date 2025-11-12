namespace RoverSearch.Models;

public class Result
{
    public string Filename { get; set; } = string.Empty;
    public string Title { get; set; } = "N/A";
    public string Description { get; set; } = "error: cannot load preview.";
    public int Score { get; set; } = 0;
    public string Snippet { get; set; } = string.Empty;
}
