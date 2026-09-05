namespace api.Models;

public class CraftsmanCraft
{
    public int CraftsmanId { get; set; }
    public int CraftId { get; set; }

    public Craftsman Craftsman { get; set; } = null!;
    public Craft Craft { get; set; } = null!;
}