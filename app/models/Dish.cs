namespace CateringLab.Models;
public class Dish
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public long PriceKopecks { get; set; }
    public List<OrderItem> OrderItems { get; set; } = [];
}
