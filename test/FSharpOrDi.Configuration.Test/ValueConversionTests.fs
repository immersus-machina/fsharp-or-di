module FSharpOrDi.Configuration.Test.ValueConversionTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.ValueConversion

let private anyFieldName = "Field"

// ==========================================================================
// Successful conversions
// ==========================================================================

[<Fact>]
let ``convertValue returns Ok with string for string target type`` () =
    // Act
    let result = convertValue anyFieldName "hello" typeof<string>

    // Assert
    Assert.Equal(Ok(box "hello"), result)

[<Fact>]
let ``convertValue returns Ok with parsed int for int target type`` () =
    // Act
    let result = convertValue anyFieldName "42" typeof<int>

    // Assert
    Assert.Equal(Ok(box 42), result)

[<Fact>]
let ``convertValue returns Ok with parsed int64 for int64 target type`` () =
    // Act
    let result = convertValue anyFieldName "9999999999" typeof<int64>

    // Assert
    Assert.Equal(Ok(box 9999999999L), result)

[<Fact>]
let ``convertValue returns Ok with parsed float for float target type`` () =
    // Act
    let result = convertValue anyFieldName "3.14" typeof<float>

    // Assert
    Assert.Equal(Ok(box 3.14), result)

[<Fact>]
let ``convertValue returns Ok with parsed decimal for decimal target type`` () =
    // Act
    let result = convertValue anyFieldName "99.99" typeof<decimal>

    // Assert
    Assert.Equal(Ok(box 99.99m), result)

[<Fact>]
let ``convertValue returns Ok with parsed bool for bool target type`` () =
    // Act
    let result = convertValue anyFieldName "true" typeof<bool>

    // Assert
    Assert.Equal(Ok(box true), result)

[<Fact>]
let ``convertValue returns Ok with parsed Guid for Guid target type`` () =
    // Arrange
    let guidString = "d3b07384-d9a0-4c9b-8a3e-000000000001"

    // Act
    let result = convertValue anyFieldName guidString typeof<System.Guid>

    // Assert
    Assert.Equal(Ok(box (System.Guid.Parse(guidString))), result)

[<Fact>]
let ``convertValue returns Ok with parsed TimeSpan for TimeSpan target type`` () =
    // Act
    let result = convertValue anyFieldName "01:30:00" typeof<System.TimeSpan>

    // Assert
    Assert.Equal(Ok(box (System.TimeSpan(1, 30, 0))), result)

[<Fact>]
let ``convertValue returns Ok with parsed DateTimeOffset for DateTimeOffset target type`` () =
    // Arrange
    let dateString = "2026-03-30T12:00:00+00:00"

    // Act
    let result = convertValue anyFieldName dateString typeof<System.DateTimeOffset>

    // Assert
    let expected =
        Ok(box (System.DateTimeOffset.Parse(dateString, System.Globalization.CultureInfo.InvariantCulture)))

    Assert.Equal(expected, result)

// ==========================================================================
// Failed conversions
// ==========================================================================

[<Fact>]
let ``convertValue returns Error ValueConversionFailed for unparseable int`` () =
    // Act
    let result = convertValue anyFieldName "not-a-number" typeof<int>

    // Assert
    Assert.Equal(Error(ValueConversionFailed(anyFieldName, "not-a-number", "Int32")), result)

[<Fact>]
let ``convertValue returns Error ValueConversionFailed for overflowing int`` () =
    // Act
    let result = convertValue anyFieldName "99999999999999999999" typeof<int>

    // Assert
    Assert.Equal(Error(ValueConversionFailed(anyFieldName, "99999999999999999999", "Int32")), result)

[<Fact>]
let ``convertValue returns Error ValueConversionFailed for unparseable bool`` () =
    // Act
    let result = convertValue anyFieldName "maybe" typeof<bool>

    // Assert
    Assert.Equal(Error(ValueConversionFailed(anyFieldName, "maybe", "Boolean")), result)

[<Fact>]
let ``convertValue returns Error ValueConversionFailed for unsupported target type`` () =
    // Act
    let result = convertValue anyFieldName "value" typeof<obj>

    // Assert
    Assert.Equal(Error(ValueConversionFailed(anyFieldName, "value", "Object")), result)
