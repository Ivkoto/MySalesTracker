---
applyTo: "**"
description: "This file provides guidelines for coding-guidelines instructions - writing clean, maintainable, and idiomatic C# code with a focus on functional patterns and proper abstraction."
---

## General:

**Description:**
.Net code should be written to maximize readability, maintainability, and correctness while minimizing complexity and coupling. Prefer immutable data where appropriate, and keep abstractions simple and focused.

**Requirements:**

- Write clear, self-documenting code
- Keep abstractions simple and focused
- Minimize dependencies and coupling
- Use modern C# features appropriately

## Code Organization:

- Use meaningful names for classes, methods, and variables
- Organize code into logical namespaces based on the project and folder, where the current class is located
- Use file-scoped namespaces

- Separate state from behavior:

  ```csharp
  // Good: Behavior separate from state
  public sealed record Order(OrderId Id, List<OrderLine> Lines);

  public static class OrderOperations
  {
      public static decimal CalculateTotal(Order order) =>
          order.Lines.Sum(line => line.Price * line.Quantity);
  }
  ```

- Prefer pure methods:

  ```csharp
  // Good: Pure function
  public static decimal CalculateTotalPrice(
      IEnumerable<OrderLine> lines,
      decimal taxRate) =>
      lines.Sum(line => line.Price * line.Quantity) * (1 + taxRate);

  // Avoid: Method with side effects
  public void CalculateAndUpdateTotalPrice()
  {
      this.Total = this.Lines.Sum(l => l.Price * l.Quantity);
      this.UpdateDatabase();
  }
  ```

- Use extension methods appropriately:

  ```csharp
  // Good: Extension method for domain-specific operations
  public static class OrderExtensions
  {
      public static bool CanBeFulfilled(this Order order, Inventory inventory) =>
          order.Lines.All(line => inventory.HasStock(line.ProductId, line.Quantity));
  }
  ```

- Design for testability:
  ```csharp
  // Good: Easy to test pure functions
  public static class PriceCalculator
  {
      public static decimal CalculateDiscount(
          decimal price,
          int quantity,
          CustomerTier tier) =>
          // Pure calculation
  }

  // Avoid: Hard to test due to hidden dependencies
  public decimal CalculateDiscount()
  {
      var user = _userService.GetCurrentUser();  // Hidden dependency
      var settings = _configService.GetSettings(); // Hidden dependency
      // Calculation
  }
  ```

## Dependency Management:

- Minimize constructor injection:

  ```csharp
  // Good: Minimal dependencies
  public sealed class OrderProcessor(IOrderRepository repository)
  {
      // Implementation
  }

  // Avoid: Too many dependencies where is possible
  // Too many dependencies indicates possible design issues
  public class OrderProcessor(
      IOrderRepository repository,
      ILogger logger,
      IEmailService emailService,
      IMetrics metrics,
      IValidator validator)
  {
      // Implementation
  }
  ```

- Prefer composition with interfaces:
  ```csharp
  // Good: Composition with interfaces
  public sealed class EnhancedLogger(ILogger baseLogger, IMetrics metrics) : ILogger
  {
  }
  ```

## Smart Comments

- Don't comment on what the code does - make the code self-documenting
- Use comments to explain why something is done a certain way
- Document APIs, complex algorithms, and non-obvious side effects

## Single Responsibility

- Each function or method should do exactly one thing
- It should be small and focused
- If it needs a comment to explain what it does, it should be split

## DRY (Don't Repeat Yourself)

- Extract repeated code into reusable functions
- Share common logic through proper abstraction
- Maintain single sources of truth

## Verify Information

Always verify information before presenting it. Do not make assumptions or speculate without clear evidence.
