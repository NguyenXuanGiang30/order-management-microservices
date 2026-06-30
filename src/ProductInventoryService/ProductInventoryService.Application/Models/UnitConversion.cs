using System;

namespace ProductInventoryService.Application.Models;

/// <summary>
/// Quy đổi đơn vị tính cho một sản phẩm cụ thể.
/// Ví dụ: 1 Thùng (FromUnit) = 24 Lon (ToUnit)
/// </summary>
public class UnitConversion : AuditableEntity
{
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public Guid FromUnitId { get; set; }
    public virtual Unit FromUnit { get; set; } = null!;

    public Guid ToUnitId { get; set; }
    public virtual Unit ToUnit { get; set; } = null!;

    public decimal Factor { get; set; } // Số lượng quy đổi: e.g. 24
}
