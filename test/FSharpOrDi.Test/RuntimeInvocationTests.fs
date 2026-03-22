module FSharpOrDi.Test.RuntimeInvocationTests

open Xunit
open Microsoft.FSharp.Reflection
open FSharpOrDi.RuntimeInvocation

let private makeFSharpFunction (systemType: System.Type) (converter: obj -> obj) : obj =
    FSharpValue.MakeFunction(systemType, converter)

[<Fact>]
let ``applyFunction applies boxed int function to boxed int argument`` () =
    // Arrange
    let boxedIncrementFunction = box (fun (x: int) -> x + 1)
    let boxedArgument = box 5

    // Act
    let result = applyFunction boxedIncrementFunction boxedArgument

    // Assert
    Assert.Equal(box 6, result)

[<Fact>]
let ``applyFunction supports partial application of multi-argument function`` () =
    // Arrange
    let boxedAddFunction = box (fun (x: int) (y: int) -> x + y)
    let boxedFirstArgument = box 3

    // Act
    let partiallyAppliedResult = applyFunction boxedAddFunction boxedFirstArgument
    let fullyAppliedResult = applyFunction partiallyAppliedResult (box 4)

    // Assert
    Assert.Equal(box 7, fullyAppliedResult)

[<Fact>]
let ``applyFunction applies boxed string function to boxed string argument`` () =
    // Arrange
    let boxedUppercaseFunction = box (fun (s: string) -> s.ToUpper())
    let boxedArgument = box "hello"

    // Act
    let result = applyFunction boxedUppercaseFunction boxedArgument

    // Assert
    Assert.Equal(box "HELLO", result)

[<Fact>]
let ``composeFunctions composes two int functions into a single function`` () =
    // Arrange
    let boxedIncrementFunction = box (fun (x: int) -> x + 1)
    let boxedDoubleFunction = box (fun (x: int) -> x * 2)
    let composedSystemType = typeof<int -> int>

    // Act
    let composedFunction =
        composeFunctions makeFSharpFunction boxedIncrementFunction boxedDoubleFunction composedSystemType

    // Assert
    let result = applyFunction composedFunction (box 5)
    Assert.Equal(box 12, result)

[<Fact>]
let ``composeFunctions composes two string functions into a single function`` () =
    // Arrange
    let boxedTrimFunction = box (fun (s: string) -> s.Trim())
    let boxedUppercaseFunction = box (fun (s: string) -> s.ToUpper())
    let composedSystemType = typeof<string -> string>

    // Act
    let composedFunction =
        composeFunctions makeFSharpFunction boxedTrimFunction boxedUppercaseFunction composedSystemType

    // Assert
    let result = applyFunction composedFunction (box "  hello  ")
    Assert.Equal(box "HELLO", result)

[<Fact>]
let ``composeFunctions applies first function then second function in order`` () =
    // Arrange
    let boxedAddTenFunction = box (fun (x: int) -> x + 10)
    let boxedMultiplyByThreeFunction = box (fun (x: int) -> x * 3)
    let composedSystemType = typeof<int -> int>

    // Act
    let composedFunction =
        composeFunctions makeFSharpFunction boxedAddTenFunction boxedMultiplyByThreeFunction composedSystemType

    // Assert
    // (2 + 10) * 3 = 36, not (2 * 3) + 10 = 16
    let result = applyFunction composedFunction (box 2)
    Assert.Equal(box 36, result)

[<Fact>]
let ``composeFunctions composes functions with different input and output types`` () =
    // Arrange
    let boxedStringLengthFunction = box (fun (s: string) -> s.Length)
    let boxedIsEvenFunction = box (fun (n: int) -> n % 2 = 0)
    let composedSystemType = typeof<string -> bool>

    // Act
    let composedFunction =
        composeFunctions makeFSharpFunction boxedStringLengthFunction boxedIsEvenFunction composedSystemType

    // Assert
    let resultForEvenLength = applyFunction composedFunction (box "test")
    Assert.Equal(box true, resultForEvenLength)
    let resultForOddLength = applyFunction composedFunction (box "odd")
    Assert.Equal(box false, resultForOddLength)
