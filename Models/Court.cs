namespace PadelTournamentManager.Models;

public class Court
{
    public Court(string name, int order)
    {
        Name = name;
        Order = order;
    }

    public string Name { get; set; }

    public int Order { get; set; }

    public bool IsEditing { get; set; }

    public string? EditName { get; set; }
}