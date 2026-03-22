module FSharpOrDi.Test.TypeDecompositionTests

open Xunit
open FSharpOrDi.TypeSignature
open FSharpOrDi.TypeDecomposition

// ==========================================================================
// decomposeType
// ==========================================================================

[<Fact>]
let ``decomposeType returns ValueType when isFunction returns false`` () =
    // Arrange
    let isFunction (_: System.Type) = false
    let getFunctionElements (_: System.Type) = failwith "should not be called"

    // Act
    let result = decomposeType isFunction getFunctionElements typeof<int>

    // Assert
    Assert.Equal(ValueType typeof<int>, result)

[<Fact>]
let ``decomposeType returns FunctionType with decomposed input and output for single-arg function`` () =
    // Arrange
    let isFunction (systemType: System.Type) = systemType = typeof<int -> float>
    let getFunctionElements (systemType: System.Type) =
        if systemType = typeof<int -> float> then (typeof<int>, typeof<float>)
        else failwith "unexpected type"

    // Act
    let result = decomposeType isFunction getFunctionElements typeof<int -> float>

    // Assert
    let expected = FunctionType(ValueType typeof<int>, ValueType typeof<float>)
    Assert.Equal(expected, result)

[<Fact>]
let ``decomposeType recursively decomposes multi-arg function types`` () =
    // Arrange
    let isFunction (systemType: System.Type) =
        systemType = typeof<int -> bool -> char> || systemType = typeof<bool -> char>

    let getFunctionElements (systemType: System.Type) =
        if systemType = typeof<int -> bool -> char> then (typeof<int>, typeof<bool -> char>)
        elif systemType = typeof<bool -> char> then (typeof<bool>, typeof<char>)
        else failwith "unexpected type"

    // Act
    let result = decomposeType isFunction getFunctionElements typeof<int -> bool -> char>

    // Assert
    let expected =
        FunctionType(ValueType typeof<int>, FunctionType(ValueType typeof<bool>, ValueType typeof<char>))

    Assert.Equal(expected, result)

[<Fact>]
let ``decomposeType recursively decomposes function parameter on input side`` () =
    // Arrange
    let isFunction (systemType: System.Type) =
        systemType = typeof<(int -> string) -> float> || systemType = typeof<int -> string>

    let getFunctionElements (systemType: System.Type) =
        if systemType = typeof<(int -> string) -> float> then (typeof<int -> string>, typeof<float>)
        elif systemType = typeof<int -> string> then (typeof<int>, typeof<string>)
        else failwith "unexpected type"

    // Act
    let result = decomposeType isFunction getFunctionElements typeof<(int -> string) -> float>

    // Assert
    let expected =
        FunctionType(FunctionType(ValueType typeof<int>, ValueType typeof<string>), ValueType typeof<float>)

    Assert.Equal(expected, result)

// ==========================================================================
// reconstructType
// ==========================================================================

[<Fact>]
let ``reconstructType returns the system type directly for ValueType`` () =
    // Arrange
    let makeFunctionType (_: System.Type) (_: System.Type) = failwith "should not be called"
    let signature = ValueType typeof<int>

    // Act
    let result = reconstructType makeFunctionType signature

    // Assert
    Assert.Equal(typeof<int>, result)

[<Fact>]
let ``reconstructType calls makeFunctionType for a simple FunctionType`` () =
    // Arrange
    let makeFunctionType (inputType: System.Type) (outputType: System.Type) =
        if inputType = typeof<int> && outputType = typeof<string> then typeof<int -> string>
        else failwith "unexpected types"

    let signature = FunctionType(ValueType typeof<int>, ValueType typeof<string>)

    // Act
    let result = reconstructType makeFunctionType signature

    // Assert
    Assert.Equal(typeof<int -> string>, result)

[<Fact>]
let ``reconstructType recursively reconstructs multi-arg FunctionType`` () =
    // Arrange
    let makeFunctionType (inputType: System.Type) (outputType: System.Type) =
        if inputType = typeof<bool> && outputType = typeof<char> then typeof<bool -> char>
        elif inputType = typeof<int> && outputType = typeof<bool -> char> then typeof<int -> bool -> char>
        else failwith "unexpected types"

    let signature =
        FunctionType(ValueType typeof<int>, FunctionType(ValueType typeof<bool>, ValueType typeof<char>))

    // Act
    let result = reconstructType makeFunctionType signature

    // Assert
    Assert.Equal(typeof<int -> bool -> char>, result)

[<Fact>]
let ``reconstructType recursively reconstructs function parameter on input side`` () =
    // Arrange
    let makeFunctionType (inputType: System.Type) (outputType: System.Type) =
        if inputType = typeof<int> && outputType = typeof<string> then typeof<int -> string>
        elif inputType = typeof<int -> string> && outputType = typeof<float> then typeof<(int -> string) -> float>
        else failwith "unexpected types"

    let signature =
        FunctionType(FunctionType(ValueType typeof<int>, ValueType typeof<string>), ValueType typeof<float>)

    // Act
    let result = reconstructType makeFunctionType signature

    // Assert
    Assert.Equal(typeof<(int -> string) -> float>, result)
