using FluentValidation;

namespace OrderProcessing.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Idempotency-Key header is required.");
        RuleFor(x => x.CustomerReference).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one order line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductCode).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.ProductCode.Trim().ToUpperInvariant()).Distinct().Count() == lines.Count)
            .WithMessage("Duplicate product codes are not allowed in a single order.")
            .When(x => x.Lines is { Count: > 0 });
    }
}
