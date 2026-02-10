using Inventory.Application.Commands;
using FluentValidation;

namespace Inventory.Application.Validators;

/// <summary>
/// Validates ReserveInventoryCommand before processing.
/// </summary>
public class ReserveInventoryCommandValidator : AbstractValidator<ReserveInventoryCommand>
{
    public ReserveInventoryCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items are required.")
            .Must(items => items != null && items.Count > 0).WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).SetValidator(new ReserveInventoryItemValidator());
    }
}
