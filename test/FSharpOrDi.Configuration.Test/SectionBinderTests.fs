module FSharpOrDi.Configuration.Test.SectionBinderTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.ConfigurationReader
open FSharpOrDi.Configuration.TypeClassification
open FSharpOrDi.Configuration.SectionBinder
open TestReaders

let private anyFieldName = "Field"

let private neverHandlePrimitive (_: string) (_: System.Type) (_: ConfigurationReader) : Result<obj, BindingError list> = failwith "should not call handlePrimitive"
let private neverHandleRecord (_: System.Type) (_: (string * System.Type) array) (_: ConfigurationReader) : Result<obj, BindingError list> = failwith "should not call handleRecord"
let private neverHandleUnion (_: Microsoft.FSharp.Reflection.UnionCaseInfo) (_: System.Type) (_: string) (_: ConfigurationReader) : Result<obj, BindingError list> = failwith "should not call handleUnion"
let private neverHandleOption (_: System.Type) (_: string) (_: ConfigurationReader) : Result<obj, BindingError list> = failwith "should not call handleOption"
let private neverHandleList (_: string) (_: System.Type) (_: ConfigurationReader) : Result<obj, BindingError list> = failwith "should not call handleList"
let private neverHandleArray (_: string) (_: System.Type) (_: ConfigurationReader) : Result<obj, BindingError list> = failwith "should not call handleArray"

// ==========================================================================
// PrimitiveType binding
// ==========================================================================

[<Fact>]
let ``bindValue delegates to handlePrimitive when type classifies as PrimitiveType`` () =
    // Arrange
    let classifyType (_: System.Type) = PrimitiveType

    let handlePrimitive (_fieldName: string) (_targetType: System.Type) (reader: ConfigurationReader) =
        match reader.GetValue "" with
        | Some rawValue -> Ok(box rawValue)
        | None -> Error [ MissingRequiredValue "missing" ]

    let readerWithValue =
        { emptyReader with GetValue = fun key -> if key = "" then Some "hello" else None }

    // Act
    let result =
        bindValue
            classifyType
            handlePrimitive
            neverHandleRecord
            neverHandleUnion
            neverHandleOption
            neverHandleList
            neverHandleArray
            anyFieldName
            typeof<string>
            readerWithValue

    // Assert
    Assert.Equal(Ok(box "hello"), result)

[<Fact>]
let ``bindValue returns error from handlePrimitive when primitive value is missing`` () =
    // Arrange
    let classifyType (_: System.Type) = PrimitiveType

    let handlePrimitive (fieldName: string) (_targetType: System.Type) (_reader: ConfigurationReader) =
        Error [ MissingRequiredValue fieldName ]

    // Act
    let result =
        bindValue
            classifyType
            handlePrimitive
            neverHandleRecord
            neverHandleUnion
            neverHandleOption
            neverHandleList
            neverHandleArray
            anyFieldName
            typeof<string>
            emptyReader

    // Assert
    Assert.Equal(Error [ MissingRequiredValue anyFieldName ], result)

// ==========================================================================
// UnsupportedType
// ==========================================================================

[<Fact>]
let ``bindValue returns UnsupportedTargetType for unsupported type classification`` () =
    // Arrange
    let classifyType (_: System.Type) = UnsupportedType

    // Act
    let result =
        bindValue
            classifyType
            neverHandlePrimitive
            neverHandleRecord
            neverHandleUnion
            neverHandleOption
            neverHandleList
            neverHandleArray
            anyFieldName
            typeof<obj>
            emptyReader

    // Assert
    Assert.Equal(Error [ UnsupportedTargetType "Object" ], result)

// ==========================================================================
// RecordType delegation
// ==========================================================================

[<Fact>]
let ``bindValue delegates to handleRecord when type classifies as RecordType`` () =
    // Arrange
    let fields = [| ("Name", typeof<string>) |]
    let classifyType (_: System.Type) = RecordType fields

    let mutable handleRecordCalled = false

    let handleRecord (_recordType: System.Type) (_fields: (string * System.Type) array) (_reader: ConfigurationReader) =
        handleRecordCalled <- true
        Ok(box "record-result")

    // Act
    let result =
        bindValue
            classifyType
            neverHandlePrimitive
            handleRecord
            neverHandleUnion
            neverHandleOption
            neverHandleList
            neverHandleArray
            anyFieldName
            typeof<obj>
            emptyReader

    // Assert
    Assert.True(handleRecordCalled)
    Assert.Equal(Ok(box "record-result"), result)
