module FSharpOrDi.Configuration.Test.OptionBindingTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.ConfigurationReader
open FSharpOrDi.Configuration.OptionBinding
open TestReaders

let private anyFieldName = "Timeout"

// ==========================================================================
// bindOptionValue
// ==========================================================================

[<Fact>]
let ``bindOptionValue returns None when hasValue returns false`` () =
    // Arrange
    let bindInnerValue (_: string) (_: System.Type) (_: ConfigurationReader) =
        failwith "should not be called"

    let makeNone (_innerType: System.Type) = box None
    let makeSome (_innerType: System.Type) (value: obj) = box (Some value)
    let hasValue (_: ConfigurationReader) = false

    // Act
    let result =
        bindOptionValue
            bindInnerValue
            makeNone
            makeSome
            hasValue
            typeof<int>
            anyFieldName
            emptyReader

    // Assert
    Assert.Equal(Ok(box None), result)

[<Fact>]
let ``bindOptionValue returns Some with bound inner value when hasValue returns true`` () =
    // Arrange
    let bindInnerValue (_: string) (_: System.Type) (_: ConfigurationReader) =
        Ok(box 30)

    let makeNone (_innerType: System.Type) = failwith "should not be called"
    let makeSome (_innerType: System.Type) (value: obj) = box ("Some", value)
    let hasValue (_: ConfigurationReader) = true

    // Act
    let result =
        bindOptionValue
            bindInnerValue
            makeNone
            makeSome
            hasValue
            typeof<int>
            anyFieldName
            emptyReader

    // Assert
    match result with
    | Ok value ->
        let (label, inner) = unbox<string * obj> value
        Assert.Equal("Some", label)
        Assert.Equal(box 30, inner)
    | Error errors -> Assert.Fail $"Expected Ok but got errors: %A{errors}"

[<Fact>]
let ``bindOptionValue passes reader to hasValue and bindInnerValue`` () =
    // Arrange
    let specificReader =
        { emptyReader with GetValue = fun _ -> Some "marker" }

    let mutable capturedHasValueReader = None
    let mutable capturedBindReader = None

    let bindInnerValue (_: string) (_: System.Type) (reader: ConfigurationReader) =
        capturedBindReader <- Some reader
        Ok(box "value")

    let makeNone (_innerType: System.Type) = failwith "should not be called"
    let makeSome (_innerType: System.Type) (value: obj) = box value

    let hasValue (reader: ConfigurationReader) =
        capturedHasValueReader <- Some reader
        true

    // Act
    let _result =
        bindOptionValue
            bindInnerValue
            makeNone
            makeSome
            hasValue
            typeof<int>
            anyFieldName
            specificReader

    // Assert
    Assert.Same(specificReader, capturedHasValueReader.Value)
    Assert.Same(specificReader, capturedBindReader.Value)

[<Fact>]
let ``bindOptionValue propagates errors from inner binding`` () =
    // Arrange
    let bindInnerValue (fieldName: string) (_: System.Type) (_: ConfigurationReader) =
        Error [ ValueConversionFailed(fieldName, "bad", "Int32") ]

    let makeNone (_innerType: System.Type) = failwith "should not be called"
    let makeSome (_innerType: System.Type) (_: obj) = failwith "should not be called"
    let hasValue (_: ConfigurationReader) = true

    // Act
    let result =
        bindOptionValue
            bindInnerValue
            makeNone
            makeSome
            hasValue
            typeof<int>
            anyFieldName
            emptyReader

    // Assert
    let expected = Error [ ValueConversionFailed(anyFieldName, "bad", "Int32") ]
    Assert.Equal(expected, result)
