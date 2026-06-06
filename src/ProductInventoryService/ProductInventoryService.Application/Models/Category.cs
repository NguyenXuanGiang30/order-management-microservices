namespace ProductInventoryService.Application.Models;

/// <summary>
/// Danh mục sản phẩm - Hỗ trợ cấu trúc cây cha-con (self-referencing).
/// VD: Điện tử → Laptop → Laptop Gaming.
/// </summary>
public class Category : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    // Self-referencing relationship (danh mục cha)
    public Guid? ParentId { get; set; }
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

    // Navigation
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
