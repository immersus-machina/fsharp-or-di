module FSharpOrDi.Configuration.Test.SingleCaseUnionBindingTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.ConfigurationReader
open FSharpOrDi.Configuration.SingleCaseUnionBinding
open TestReaders

let private anyFieldName = "Port"
let private dummyCaseInfo = Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(typeof<int option>)[0]

// ==========================================================================
// bindSingleCaseUnion
// ==========================================================================

[<Fact>]
let ``bindSingleCaseUnion binds inner value and wraps with makeUnionCase`` () =
    // Arrange
    let bindInnerValue (_fieldName: string) (_innerType: System.Type) (_reader: ConfigurationReader) =
        Ok(box 8080)

    let makeUnionCase (_caseInfo: Microsoft.FSharp.Reflection.UnionCaseInfo) (values: obj array) =
        box ("Wrapped", values[0])

    // Act
    let result =
        bindSingleCaseUnion
            bindInnerValue
            makeUnionCase
            dummyCaseInfo
            typeof<int>
            anyFieldName
            emptyReader

    // Assert
    match result with
    | Ok value ->
        let (label, inner) = unbox<string * obj> value
        Assert.Equal("Wrapped", label)
        Assert.Equal(box 8080, inner)
    | Error errors -> Assert.Fail $"Expected Ok but got errors: %A{errors}"

[<Fact>]
let ``bindSingleCaseUnion propagates errors from inner binding`` () =
    // Arrange
    let bindInnerValue (fieldName: string) (_innerType: System.Type) (_reader: ConfigurationReader) =
        Error [ MissingRequiredValue fieldName ]

    let makeUnionCase (_caseInfo: Microsoft.FSharp.Reflection.UnionCaseInfo) (_: obj array) =
        failwith "should not be called"

    // Act
    let result =
        bindSingleCaseUnion
            bindInnerValue
            makeUnionCase
            dummyCaseInfo
            typeof<int>
            anyFieldName
            emptyReader

    // Assert
    Assert.Equal(Error [ MissingRequiredValue anyFieldName ], result)
