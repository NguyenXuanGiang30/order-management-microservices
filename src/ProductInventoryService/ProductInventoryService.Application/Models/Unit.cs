namespace ProductInventoryService.Application.Models;

/// <summary>
/// Đơn vị tính sản phẩm - VD: Cái, Hộp, Kg, Lít.
/// Tách thành bảng riêng để tái sử dụng và nhất quán.
/// </summary>
public class Unit : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Abbreviation { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
