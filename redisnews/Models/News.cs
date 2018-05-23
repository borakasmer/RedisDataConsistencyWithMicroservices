public class News
{
    public int ID {get; set;}
    public string Title { get; set; }
    public string Detail { get; set; }
    public System.DateTime CreatedDate { get; set; }
    public System.DateTime? UpdatedDate { get; set; }
    public string Image { get; set; }
    public bool IsError { get; set; }
}