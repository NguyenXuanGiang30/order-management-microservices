namespace OrderSalesService.Application.Models;

/// <summary>
/// Nhóm khách hàng - Phân loại khách hàng (VIP, Sỉ, Lẻ)
/// để tự động áp dụng DefaultDiscountPercent khi tạo đơn.
/// </summary>
public class CustomerGroup : BaseEntity
{
    public string Name { get; set; } = null!;
    public decimal DefaultDiscountPercent { get; set; }
    public string? Note { get; set; }

    // Navigation
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
