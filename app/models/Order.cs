namespace CateringLab.Models;
public enum OrderStatus { New, Preparing, Completed, Cancelled }
public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
