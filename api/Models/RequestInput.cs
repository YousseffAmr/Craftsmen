namespace api.Models;

public class RequestInput
{
    public int CraftId { get; set; }
    public DateTime NeededOn { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
}