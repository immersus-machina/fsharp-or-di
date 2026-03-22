module FSharpOrDi.Test.FormattingTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph
open FSharpOrDi.Formatting

[<Fact>]
let ``formatSignature returns type name for ValueType`` () =
    // Arrange
    let signature = ValueType typeof<int>

    // Act
    let result = formatSignature signature

    // Assert
    Assert.Equal("Int32", result)

[<Fact>]
let ``formatSignature returns parenthesized arrow for FunctionType`` () =
    // Arrange
    let signature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)

    // Act
    let result = formatSignature signature

    // Assert
    Assert.Equal("(Int32 -> String)", result)

[<Fact>]
let ``formatSignature formats nested function types recursively`` () =
    // Arrange
    let innerFunction = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let signature = FunctionType(innerFunction, ValueType typeof<bool>)

    // Act
    let result = formatSignature signature

    // Assert
    Assert.Equal("((Int32 -> String) -> Boolean)", result)

[<Fact>]
let ``formatOriginShallow returns direct registration with formatted signature`` () =
    // Arrange
    let mockFormatOriginSignature (_: NodeOrigin) : string = "MOCK_TYPE"
    let origin = Registered(ValueType typeof<int>)

    // Act
    let result = formatOriginShallow mockFormatOriginSignature origin

    // Assert
    Assert.Equal("direct registration of MOCK_TYPE", result)

[<Fact>]
let ``formatOriginShallow formats partial application with child origin signatures`` () =
    // Arrange
    let mockFormatOriginSignature (origin: NodeOrigin) : string =
        match origin with
        | Registered(FunctionType _) -> "(Int -> String)"
        | Registered(ValueType _) -> "Int"
        | _ -> failwith "Unexpected origin"

    let functionOrigin = Registered(FunctionType(ValueType typeof<int>, ValueType typeof<string>))
    let argumentOrigin = Registered(ValueType typeof<int>)
    let origin = DerivedByPartialApplication(functionOrigin, argumentOrigin)

    // Act
    let result = formatOriginShallow mockFormatOriginSignature origin

    // Assert
    Assert.Equal("partial application of (Int -> String) with Int", result)

[<Fact>]
let ``formatOriginShallow formats composition with child origin signatures`` () =
    // Arrange
    let mockFormatOriginSignature (origin: NodeOrigin) : string =
        match origin with
        | Registered(FunctionType(ValueType i, ValueType s)) when i = typeof<int> && s = typeof<string> -> "(Int -> String)"
        | Registered(FunctionType(ValueType s, ValueType f)) when s = typeof<string> && f = typeof<float> -> "(String -> Float)"
        | _ -> failwith "Unexpected origin"

    let firstOrigin = Registered(FunctionType(ValueType typeof<int>, ValueType typeof<string>))
    let secondOrigin = Registered(FunctionType(ValueType typeof<string>, ValueType typeof<float>))
    let origin = DerivedByComposition(firstOrigin, secondOrigin)

    // Act
    let result = formatOriginShallow mockFormatOriginSignature origin

    // Assert
    Assert.Equal("composition of (Int -> String) >> (String -> Float)", result)

[<Fact>]
let ``formatOriginDeep returns direct registration for Registered origin`` () =
    // Arrange
    let mockFormatSignature (_: TypeSignature) : string = "MOCK_TYPE"
    let origin = Registered(ValueType typeof<int>)

    // Act
    let result = formatOriginDeep mockFormatSignature origin

    // Assert
    Assert.Equal("direct registration of MOCK_TYPE", result)

[<Fact>]
let ``formatOriginDeep recursively formats nested partial application`` () =
    // Arrange
    let mockFormatSignature (signature: TypeSignature) : string =
        match signature with
        | ValueType systemType when systemType = typeof<int> -> "Int"
        | ValueType systemType when systemType = typeof<string> -> "String"
        | _ -> "Other"

    let functionOrigin = Registered(ValueType typeof<int>)
    let argumentOrigin = Registered(ValueType typeof<string>)
    let origin = DerivedByPartialApplication(functionOrigin, argumentOrigin)

    // Act
    let result = formatOriginDeep mockFormatSignature origin

    // Assert
    Assert.Equal("partial application of [direct registration of Int] with [direct registration of String]", result)

[<Fact>]
let ``formatOriginDeep recursively formats nested composition`` () =
    // Arrange
    let mockFormatSignature (signature: TypeSignature) : string =
        match signature with
        | ValueType systemType when systemType = typeof<int> -> "Int"
        | ValueType systemType when systemType = typeof<string> -> "String"
        | _ -> "Other"

    let firstOrigin = Registered(ValueType typeof<int>)
    let secondOrigin = Registered(ValueType typeof<string>)
    let origin = DerivedByComposition(firstOrigin, secondOrigin)

    // Act
    let result = formatOriginDeep mockFormatSignature origin

    // Assert
    Assert.Equal("composition of [direct registration of Int] >> [direct registration of String]", result)
