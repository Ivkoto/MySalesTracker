---
description: "This file provides guidelines for coding-guidelines instructions & coding-style instructions - writing clean, maintainable, and idiomatic C# code with a focus on functional patterns and proper abstraction."
---

# Coding-Guidelines Instructions

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

---

# Coding-Style Instructions

## Type Definitions:

- Prefer records for data types where appropriate:

  ```csharp
  // Good: Immutable data type with value semantics
  public sealed record CustomerDto(string Name, Email Email);

  // Avoid: Class with mutable properties
  public class Customer
  {
      public string Name { get; set; }
      public string Email { get; set; }
  }
  ```

## Variable Declarations:

- Use var where possible:

  ```csharp
  // Good: Using var for type inference
  var fruit = "Apple";
  var number = 42;
  var order = new Order(fruit, number);
  ```

- Use pattern matching effectively:

  ```csharp
  // Good: Clear pattern matching
  public decimal CalculateDiscount(Customer customer) =>
      customer switch
      {
          { Tier: CustomerTier.Premium } => 0.2m,
          { OrderCount: > 10 } => 0.1m,
          _ => 0m
      };

  // Avoid: Nested if statements
  public decimal CalculateDiscount(Customer customer)
  {
      if (customer.Tier == CustomerTier.Premium)
          return 0.2m;
      if (customer.OrderCount > 10)
          return 0.1m;
      return 0m;
  }
  ```

## Nullability:

- Mark nullable fields explicitly:

  ```csharp
  // Good: Explicit nullability
  public class OrderProcessor
  {
      private readonly ILogger<OrderProcessor>? _logger;
      private string? _lastError;

      public OrderProcessor(ILogger<OrderProcessor>? logger = null)
      {
          _logger = logger;
      }
  }

  // Avoid: Implicit nullability
  public class OrderProcessor
  {
      private readonly ILogger<OrderProcessor> _logger; // Warning: Could be null
      private string _lastError; // Warning: Could be null
  }
  ```

- Use null checks only when necessary for reference types and public methods:

  ```csharp
  // Good: Proper null checking
  public void ProcessOrder(Order order)
  {
      ArgumentNullException.ThrowIfNull(order); // Appropriate for reference types

      _logger?.LogInformation("Processing order {Id}", order.Id);
  }

  // Good: Using pattern matching for null checks
  public decimal CalculateTotal(Order? order) =>
      order switch
      {
          null => throw new ArgumentNullException(nameof(order)),
          { Lines: null } => throw new ArgumentException("Order lines cannot be null", nameof(order)),
          _ => order.Lines.Sum(l => l.Total)
      };
  ```

- Use init-only properties with non-null validation:

  ```csharp
  // Good: Non-null validation in constructor
  public sealed record Order
  {
      public required OrderId Id { get; init; }
      public required ImmutableList<OrderLine> Lines { get; init; }

      public Order()
      {
          Id = null!; // Will be set by required property
          Lines = null!; // Will be set by required property
      }

      private Order(OrderId id, ImmutableList<OrderLine> lines)
      {
          Id = id;
          Lines = lines;
      }

      public static Order Create(OrderId id, IEnumerable<OrderLine> lines) =>
          new(id, lines.ToImmutableList());
  }
  ```

- Document nullability in interfaces:

  ```csharp
  public interface IOrderRepository
  {
      // Explicitly shows that null is a valid return value
      Task<Order?> FindByIdAsync(OrderId id, CancellationToken ct = default);

      // Method will never return null
      [return: NotNull]
      Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);

      // Parameter cannot be null
      Task SaveAsync([NotNull] Order order, CancellationToken ct = default);
  }
  ```

## Safe Operations:

- Use Try methods for safer operations:

  ```csharp
  // Good: Using TryGetValue for dictionary access
  if (dictionary.TryGetValue(key, out var value))
  {
      // Use value safely here
  }
  else
  {
      // Handle missing key case
  }

  // Avoid: Direct indexing which can throw
  var value = dictionary[key];  // Throws if key doesn't exist
  ```

  ```csharp
  // Good: Using Uri.TryCreate for URL parsing
  if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
  {
      // Use uri safely here
  }
  else
  {
      // Handle invalid URL case
  }

  // Avoid: Direct Uri creation which can throw
    var uri = new Uri(urlString);  // Throws on invalid URL
  ```

  ```csharp
  // Good: Using int.TryParse for number parsing
    if (int.TryParse(input, out var number))
    {
        // Use number safely here
    }
    else
    {
        // Handle invalid number case
    }
  ```

  ```csharp
    // Good: Combining Try methods with null coalescing
    var value = dictionary.TryGetValue(key, out var result)
        ? result
        : defaultValue;

    // Good: Using Try methods in LINQ with pattern matching
    var validNumbers = strings
        .Select(s => (Success: int.TryParse(s, out var num), Value: num))
        .Where(x => x.Success)
        .Select(x => x.Value);
  ```

## Asynchronous Programming:

- Avoid use of Task.Result, Task.Wait, Task.FromResult.

- Prefer async/await over direct Task return:
  ```csharp
  // Good: Using async/await
  public async Task<Order> ProcessOrderAsync(OrderRequest request)
  {
      await _validator.ValidateAsync(request);
      var order = await _factory.CreateAsync(request);
      return order;
  }
  ```
