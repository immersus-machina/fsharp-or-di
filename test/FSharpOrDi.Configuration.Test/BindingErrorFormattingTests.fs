module FSharpOrDi.Configuration.Test.BindingErrorFormattingTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.BindingErrorFormatting

// ==========================================================================
// formatError
// ==========================================================================

[<Fact>]
let ``formatError formats MissingRequiredValue with field name`` () =
    // Arrange
    let error = MissingRequiredValue "Host"

    // Act
    let result = formatError error

    // Assert
    Assert.Equal("Missing required value: 'Host'", result)

[<Fact>]
let ``formatError formats ValueConversionFailed with field name, raw value, and target type`` () =
    // Arrange
    let error = ValueConversionFailed("Port", "abc", "Int32")

    // Act
    let result = formatError error

    // Assert
    Assert.Equal("Cannot convert 'abc' to Int32 for field 'Port'", result)

[<Fact>]
let ``formatError formats UnsupportedTargetType with type name`` () =
    // Arrange
    let error = UnsupportedTargetType "MyUnion"

    // Act
    let result = formatError error

    // Assert
    Assert.Equal("Unsupported target type: 'MyUnion'", result)

// ==========================================================================
// formatErrors
// ==========================================================================

[<Fact>]
let ``formatErrors formats single error with header and bullet`` () =
    // Arrange
    let errors = [ MissingRequiredValue "Host" ]

    // Act
    let result = formatErrors errors

    // Assert
    let expected = "Configuration binding failed:\n- Missing required value: 'Host'"
    Assert.Equal(expected, result)

[<Fact>]
let ``formatErrors formats multiple errors with header and bullets`` () =
    // Arrange
    let errors =
        [ MissingRequiredValue "Host"
          ValueConversionFailed("Port", "abc", "Int32")
          UnsupportedTargetType "MyUnion" ]

    // Act
    let result = formatErrors errors

    // Assert
    let expected =
        "Configuration binding failed:\n- Missing required value: 'Host'\n- Cannot convert 'abc' to Int32 for field 'Port'\n- Unsupported target type: 'MyUnion'"

    Assert.Equal(expected, result)
