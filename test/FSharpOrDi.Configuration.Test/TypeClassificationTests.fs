module FSharpOrDi.Configuration.Test.TypeClassificationTests

open Xunit
open FSharpOrDi.Configuration.TypeClassification

let private neverIsRecord (_: System.Type) = false
let private neverGetRecordFields (_: System.Type) = failwith "should not be called"
let private neverIsUnion (_: System.Type) = false
let private neverGetUnionCases (_: System.Type) = failwith "should not be called"

// ==========================================================================
// PrimitiveType classification
// ==========================================================================

[<Theory>]
[<InlineData(typeof<string>)>]
[<InlineData(typeof<int>)>]
[<InlineData(typeof<int64>)>]
[<InlineData(typeof<float>)>]
[<InlineData(typeof<decimal>)>]
[<InlineData(typeof<bool>)>]
let ``classifyType returns PrimitiveType for primitive types`` (primitiveType: System.Type) =
    // Act
    let result = classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases primitiveType

    // Assert
    Assert.Equal(PrimitiveType, result)

[<Fact>]
let ``classifyType returns PrimitiveType for Guid`` () =
    // Act
    let result = classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<System.Guid>

    // Assert
    Assert.Equal(PrimitiveType, result)

[<Fact>]
let ``classifyType returns PrimitiveType for TimeSpan`` () =
    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<System.TimeSpan>

    // Assert
    Assert.Equal(PrimitiveType, result)

[<Fact>]
let ``classifyType returns PrimitiveType for DateTimeOffset`` () =
    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<System.DateTimeOffset>

    // Assert
    Assert.Equal(PrimitiveType, result)

// ==========================================================================
// OptionType classification
// ==========================================================================

[<Fact>]
let ``classifyType returns OptionType with inner type for option of string`` () =
    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<string option>

    // Assert
    Assert.Equal(OptionType typeof<string>, result)

[<Fact>]
let ``classifyType returns OptionType with inner type for option of int`` () =
    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<int option>

    // Assert
    Assert.Equal(OptionType typeof<int>, result)

// ==========================================================================
// ListType classification
// ==========================================================================

[<Fact>]
let ``classifyType returns ListType with element type for string list`` () =
    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<string list>

    // Assert
    Assert.Equal(ListType typeof<string>, result)

// ==========================================================================
// ArrayType classification
// ==========================================================================

[<Fact>]
let ``classifyType returns ArrayType with element type for int array`` () =
    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<int array>

    // Assert
    Assert.Equal(ArrayType typeof<int>, result)

// ==========================================================================
// RecordType classification
// ==========================================================================

[<Fact>]
let ``classifyType returns RecordType with field names and types when isRecord returns true`` () =
    // Arrange
    let isRecord (_: System.Type) = true

    let getRecordFields (_: System.Type) =
        [| typeof<string>.GetProperty("Length") |]

    // Act
    let result = classifyType isRecord getRecordFields neverIsUnion neverGetUnionCases typeof<obj>

    // Assert
    match result with
    | RecordType fields ->
        Assert.Equal(1, fields.Length)
        Assert.Equal("Length", fst fields[0])
    | other -> Assert.Fail $"Expected RecordType but got %A{other}"

// ==========================================================================
// SingleCaseDiscriminatedUnion classification
// ==========================================================================

[<Fact>]
let ``classifyType returns SingleCaseDiscriminatedUnion for single-case union with one field`` () =
    // Arrange
    let isUnion (_: System.Type) = true

    let getUnionCases (_: System.Type) =
        Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(typeof<Result<int, string>>)
        |> Array.take 1

    let singleCase = (getUnionCases typeof<obj>)[0]
    let caseFields = singleCase.GetFields()
    let expectedInnerType = caseFields[0].PropertyType

    // Act
    let result = classifyType neverIsRecord neverGetRecordFields isUnion getUnionCases typeof<obj>

    // Assert
    match result with
    | SingleCaseDiscriminatedUnion(caseInfo, innerType) ->
        Assert.Equal(singleCase.Name, caseInfo.Name)
        Assert.Equal(expectedInnerType, innerType)
    | other -> Assert.Fail $"Expected SingleCaseDiscriminatedUnion but got %A{other}"

// ==========================================================================
// UnsupportedType classification
// ==========================================================================

[<Fact>]
let ``classifyType returns UnsupportedType for multi-case discriminated union`` () =
    // Arrange
    let isUnion (_: System.Type) = true

    let getUnionCases (_: System.Type) =
        Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(typeof<Result<int, string>>)

    // Act
    let result = classifyType neverIsRecord neverGetRecordFields isUnion getUnionCases typeof<obj>

    // Assert
    Assert.Equal(UnsupportedType, result)

[<Fact>]
let ``classifyType returns UnsupportedType for unknown non-primitive non-record non-union type`` () =
    // Act
    let result = classifyType neverIsRecord neverGetRecordFields neverIsUnion neverGetUnionCases typeof<obj>

    // Assert
    Assert.Equal(UnsupportedType, result)

// ==========================================================================
// Priority ordering
// ==========================================================================

[<Fact>]
let ``classifyType classifies option before checking union`` () =
    // Arrange
    let isUnion (_: System.Type) = true
    let getUnionCases (_: System.Type) = failwith "should not be called for option type"

    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields isUnion getUnionCases typeof<string option>

    // Assert
    Assert.Equal(OptionType typeof<string>, result)

[<Fact>]
let ``classifyType classifies list before checking union`` () =
    // Arrange
    let isUnion (_: System.Type) = true
    let getUnionCases (_: System.Type) = failwith "should not be called for list type"

    // Act
    let result =
        classifyType neverIsRecord neverGetRecordFields isUnion getUnionCases typeof<string list>

    // Assert
    Assert.Equal(ListType typeof<string>, result)
