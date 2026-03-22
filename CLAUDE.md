# Project Principles

## Code Design

- Decompose into small, independently testable functions. Always. "It's small" is never an excuse.
- Functions take their dependencies as parameters — no direct module references that could be mocked.
- Every function should be testable in isolation with mocked inputs.

## Naming

- Verbose, self-documenting names. No abbreviations, no single-letter names (except in tests for mathematical examples).
- Consistent naming style across files. Module names describe the domain concept they represent.

## Testing

- Requirements are defined as tests first. Implementation follows.
- Tests may fail — that's fine. They define what the system should do, not what it currently does.
- Integration tests are valuable as requirement documentation.
- Unit tests test decomposed building blocks in isolation.
- Use // Arrange // Act // Assert comments in every test for readability. Always separate — never combine as "// Act & Assert".

## Discussion Before Implementation

- When stuck or when a fix requires workarounds, stop and discuss options instead of iterating blindly.
- Do not change tests to match implementation. Tests define requirements.
- Do not optimize for "making tests pass" — optimize for correct design.

## Comments

- Minimum comments. Only add comments when the business logic genuinely requires explanation.
- Do not comment what the function name, signature, or logic already make obvious.
- Self-documenting code (good names, clear types) is preferred over commented code.

## F# Style

- Idiomatic F#: discriminated unions for statuses/classifications, wrapper types for domain values crossing boundaries.
- No `process'` naming — rename to proper verbs instead.
- Functions should not directly depend on concrete modules when that dependency can be injected.

## Architecture

- No "it's small enough" exceptions. Apply the same rigor regardless of library size.
- Error formatting, type inspection, reflection, resolution logic — all separate concerns, all separately testable.
