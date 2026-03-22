module FSharpOrDi.Test.ResolutionGraphTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.ResolutionGraph

let private originA = Registered(ValueType typeof<int>)
let private originB = Registered(ValueType typeof<string>)
let private originC = Registered(ValueType typeof<float>)

[<Fact>]
let ``flattenOrigin returns single-element list for Registered origin`` () =
    // Act
    let result = flattenOrigin originA

    // Assert
    Assert.Equal<NodeOrigin list>([ originA ], result)

[<Fact>]
let ``flattenOrigin flattens partial application to argument then function`` () =
    // Arrange
    let origin = DerivedByPartialApplication(originA, originB)

    // Act
    let result = flattenOrigin origin

    // Assert
    Assert.Equal<NodeOrigin list>([ originB; originA ], result)

[<Fact>]
let ``flattenOrigin flattens composition to first then second`` () =
    // Arrange
    let origin = DerivedByComposition(originA, originB)

    // Act
    let result = flattenOrigin origin

    // Assert
    Assert.Equal<NodeOrigin list>([ originA; originB ], result)

[<Fact>]
let ``flattenOrigin produces same list for left and right associated compositions`` () =
    // Arrange
    let leftAssociated = DerivedByComposition(DerivedByComposition(originA, originB), originC)
    let rightAssociated = DerivedByComposition(originA, DerivedByComposition(originB, originC))

    // Act
    let leftResult = flattenOrigin leftAssociated
    let rightResult = flattenOrigin rightAssociated

    // Assert
    Assert.Equal<NodeOrigin list>(leftResult, rightResult)
    Assert.Equal<NodeOrigin list>([ originA; originB; originC ], leftResult)

[<Fact>]
let ``doNodesConflict returns false when signatures differ`` () =
    // Arrange
    let nodeA = { Signature = ValueType typeof<int>; Implementation = obj (); Origin = originA }
    let nodeB = { Signature = ValueType typeof<string>; Implementation = obj (); Origin = originB }

    // Act
    let result = doNodesConflict nodeA nodeB

    // Assert
    Assert.False(result)

[<Fact>]
let ``doNodesConflict returns false when same signature and same flattened origin`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let nodeA = { Signature = signature; Implementation = obj (); Origin = originA }
    let nodeB = { Signature = signature; Implementation = obj (); Origin = originA }

    // Act
    let result = doNodesConflict nodeA nodeB

    // Assert
    Assert.False(result)

[<Fact>]
let ``doNodesConflict returns true when same signature but different flattened origins`` () =
    // Arrange
    let signature = ValueType typeof<int>
    let nodeA = { Signature = signature; Implementation = obj (); Origin = originA }
    let nodeB = { Signature = signature; Implementation = obj (); Origin = originB }

    // Act
    let result = doNodesConflict nodeA nodeB

    // Assert
    Assert.True(result)

[<Fact>]
let ``doNodesConflict returns false for composition vs partial application with same building blocks`` () =
    // Arrange
    // h |> (g >> f) and (h |> g) |> f flatten to the same sequence
    let signature = ValueType typeof<int>
    let compositionOrigin = DerivedByComposition(originA, DerivedByComposition(originB, originC))
    let partialApplicationOrigin = DerivedByPartialApplication(DerivedByPartialApplication(originC, originB), originA)

    // flattenOrigin compositionOrigin = [A, B, C]
    // flattenOrigin partialApplicationOrigin: partialApp(partialApp(C, B), A)
    //   flatten(A) @ flatten(partialApp(C, B)) = [A] @ (flatten(B) @ flatten(C)) = [A, B, C]
    // Wait — partial application flattens as argument then function
    // flatten(partialApp(C, B)) = flatten(B) @ flatten(C) = [B, C]
    // flatten(partialApp(partialApp(C, B), A)) = flatten(A) @ flatten(partialApp(C, B)) = [A, B, C]

    let nodeA = { Signature = signature; Implementation = obj (); Origin = compositionOrigin }
    let nodeB = { Signature = signature; Implementation = obj (); Origin = partialApplicationOrigin }

    // Act
    let result = doNodesConflict nodeA nodeB

    // Assert
    Assert.False(result)

[<Fact>]
let ``signatureOf returns type signature for Registered origin`` () =
    // Arrange
    let origin = Registered(ValueType typeof<int>)

    // Act
    let result = signatureOf origin

    // Assert
    Assert.Equal(ValueType typeof<int>, result)

[<Fact>]
let ``signatureOf returns output type for partial application origin`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let functionOrigin = Registered intToStringSignature
    let argumentOrigin = Registered(ValueType typeof<int>)
    let origin = DerivedByPartialApplication(functionOrigin, argumentOrigin)

    // Act
    let result = signatureOf origin

    // Assert
    Assert.Equal(ValueType typeof<string>, result)

[<Fact>]
let ``signatureOf returns composed signature for composition origin`` () =
    // Arrange
    let intToStringSignature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)
    let stringToFloatSignature = FunctionType(ValueType typeof<string>, ValueType typeof<float>)
    let firstOrigin = Registered intToStringSignature
    let secondOrigin = Registered stringToFloatSignature
    let origin = DerivedByComposition(firstOrigin, secondOrigin)

    // Act
    let result = signatureOf origin

    // Assert
    let expected = FunctionType(ValueType typeof<int>, ValueType typeof<float>)
    Assert.Equal(expected, result)
