using FluentValidation;
using ProductInventoryService.Application.Features.Products.Commands.CreateProduct;

namespace ProductInventoryService.Application.Validators;

/// <summary>
/// FluentValidation rule cho CreateProductCommand.
/// Tự động được kích hoạt bởi ValidationBehavior trong MediatR pipeline.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã sản phẩm không được để trống.")
            .MaximumLength(50).WithMessage("Mã sản phẩm tối đa 50 ký tự.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(300).WithMessage("Tên sản phẩm tối đa 300 ký tự.");

        RuleFor(x => x.ImportPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá nhập phải >= 0.");

        RuleFor(x => x.SellPrice)
            .GreaterThan(0).WithMessage("Giá bán phải > 0.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Phải chọn danh mục sản phẩm.");

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Phải chọn đơn vị tính.");
    }
}
