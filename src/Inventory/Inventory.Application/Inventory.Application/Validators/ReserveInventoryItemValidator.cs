using Inventory.Application.Commands;
using FluentValidation;

namespace Inventory.Application.Validators;

/// <summary>
/// Validates a single item in a reserve inventory request.
/// </summary>
public class ReserveInventoryItemValidator : AbstractValidator<ReserveInventoryItem>
{
    public ReserveInventoryItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.")
            .MaximumLength(128).WithMessage("ProductId must not exceed 128 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}
