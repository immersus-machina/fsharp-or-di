module FSharpOrDi.Test.DiagnosticsTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph
open FSharpOrDi.Diagnostics

let private signatureA = ValueType typeof<int>
let private signatureB = ValueType typeof<string>
let private signatureC = ValueType typeof<float>

let private functionAToB = FunctionType(signatureA, signatureB)
let private functionBToC = FunctionType(signatureB, signatureC)

let private makeNode signature origin =
    { Signature = signature; Implementation = obj (); Origin = origin }

[<Fact>]
let ``describeMissingDependencies reports no producer when target has none`` () =
    // Arrange
    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = signatureA -> "MOCK(A)"
        | _ -> failwith "unexpected type"

    // Act
    let result = describeMissingDependencies formatSignature signatureA emptyStage

    // Assert
    Assert.Contains("Cannot resolve: MOCK(A)", result)
    Assert.Contains("No registered function produces MOCK(A)", result)

[<Fact>]
let ``describeMissingDependencies traces one level when producer exists but its input is missing`` () =
    // Arrange
    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = functionAToB -> "MOCK(A -> B)"
        | s when s = signatureA -> "MOCK(A)"
        | s when s = signatureB -> "MOCK(B)"
        | _ -> failwith "unexpected type"

    let stage =
        { Nodes =
            Map.ofList
                [ (functionAToB, makeNode functionAToB (Registered functionAToB)) ] }

    // Act
    let result = describeMissingDependencies formatSignature signatureB stage

    // Assert
    Assert.Contains("Cannot resolve: MOCK(B)", result)
    Assert.Contains("Could be produced by: MOCK(A -> B)", result)
    Assert.Contains("Missing: MOCK(A)", result)
    Assert.Contains("No registered function produces MOCK(A)", result)

[<Fact>]
let ``describeMissingDependencies traces multiple levels deep`` () =
    // Arrange
    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = functionAToB -> "MOCK(A -> B)"
        | s when s = functionBToC -> "MOCK(B -> C)"
        | s when s = signatureA -> "MOCK(A)"
        | s when s = signatureB -> "MOCK(B)"
        | s when s = signatureC -> "MOCK(C)"
        | _ -> failwith "unexpected type"

    let stage =
        { Nodes =
            Map.ofList
                [ (functionAToB, makeNode functionAToB (Registered functionAToB))
                  (functionBToC, makeNode functionBToC (Registered functionBToC)) ] }

    // Act
    let result = describeMissingDependencies formatSignature signatureC stage

    // Assert
    Assert.Contains("Cannot resolve: MOCK(C)", result)
    Assert.Contains("Could be produced by: MOCK(B -> C)", result)
    Assert.Contains("Missing: MOCK(B)", result)
    Assert.Contains("Could be produced by: MOCK(A -> B)", result)
    Assert.Contains("Missing: MOCK(A)", result)
    Assert.Contains("No registered function produces MOCK(A)", result)

[<Fact>]
let ``describeMissingDependencies handles cyclic dependency without infinite loop`` () =
    // Arrange
    let functionBToA = FunctionType(signatureB, signatureA)

    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = functionAToB -> "MOCK(A -> B)"
        | s when s = functionBToA -> "MOCK(B -> A)"
        | s when s = signatureA -> "MOCK(A)"
        | s when s = signatureB -> "MOCK(B)"
        | _ -> failwith "unexpected type"

    let stage =
        { Nodes =
            Map.ofList
                [ (functionAToB, makeNode functionAToB (Registered functionAToB))
                  (functionBToA, makeNode functionBToA (Registered functionBToA)) ] }

    // Act
    let result = describeMissingDependencies formatSignature signatureB stage

    // Assert
    Assert.Contains("Cannot resolve: MOCK(B)", result)
    Assert.Contains("Cyclic dependency on", result)

[<Fact>]
let ``describeMissingDependencies reports unexpected when producer input is available but output was not resolved`` () =
    // Arrange
    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = functionAToB -> "MOCK(A -> B)"
        | s when s = signatureA -> "MOCK(A)"
        | s when s = signatureB -> "MOCK(B)"
        | _ -> failwith "unexpected type"

    let stage =
        { Nodes =
            Map.ofList
                [ (functionAToB, makeNode functionAToB (Registered functionAToB))
                  (signatureA, makeNode signatureA (Registered signatureA)) ] }

    // Act
    let result = describeMissingDependencies formatSignature signatureB stage

    // Assert
    Assert.Contains("Could be produced by: MOCK(A -> B)", result)
    Assert.Contains("But input MOCK(A) is available (unexpected — possible library bug)", result)

[<Fact>]
let ``describeMissingDependencies finds multi-arg function as eventual producer`` () =
    // Arrange
    let functionAToFunctionBToC = FunctionType(signatureA, FunctionType(signatureB, signatureC))

    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = functionAToFunctionBToC -> "MOCK(A -> B -> C)"
        | s when s = signatureA -> "MOCK(A)"
        | s when s = signatureC -> "MOCK(C)"
        | _ -> failwith "unexpected type"

    let stage =
        { Nodes =
            Map.ofList
                [ (functionAToFunctionBToC, makeNode functionAToFunctionBToC (Registered functionAToFunctionBToC)) ] }

    // Act
    let result = describeMissingDependencies formatSignature signatureC stage

    // Assert
    Assert.Contains("Cannot resolve: MOCK(C)", result)
    Assert.Contains("Could eventually be produced by (after partial application of): MOCK(A -> B -> C)", result)

[<Fact>]
let ``describeMissingDependencies lists multiple producers when both could produce the target`` () =
    // Arrange
    let functionAToC = FunctionType(signatureA, signatureC)

    let formatSignature (signature: TypeSignature) =
        match signature with
        | s when s = functionAToC -> "MOCK(A -> C)"
        | s when s = functionBToC -> "MOCK(B -> C)"
        | s when s = signatureA -> "MOCK(A)"
        | s when s = signatureB -> "MOCK(B)"
        | s when s = signatureC -> "MOCK(C)"
        | _ -> failwith "unexpected type"

    let stage =
        { Nodes =
            Map.ofList
                [ (functionAToC, makeNode functionAToC (Registered functionAToC))
                  (functionBToC, makeNode functionBToC (Registered functionBToC)) ] }

    // Act
    let result = describeMissingDependencies formatSignature signatureC stage

    // Assert
    Assert.Contains("Cannot resolve: MOCK(C)", result)
    Assert.Contains("Could be produced by: MOCK(A -> C)", result)
    Assert.Contains("Could be produced by: MOCK(B -> C)", result)
    Assert.Contains("Missing: MOCK(A)", result)
    Assert.Contains("Missing: MOCK(B)", result)
