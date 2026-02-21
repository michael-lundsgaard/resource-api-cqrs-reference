---
applyTo: '**/*.cs'
---

# .NET Coding Standards

- Use `var` only when the type is obvious from the right-hand side
- Prefer `async/await` over `.Result` or `.Wait()`
- Guard clauses at the top of methods, early return pattern
- Prefer one top-level type per file. In feature slices, it is acceptable to co-locate request/handler/validator types in one file.
- Throw `ArgumentNullException.ThrowIfNull()` for null guards
- Use `ILogger<T>` injected via primary constructor
- Never catch `Exception` — catch specific exception types only
