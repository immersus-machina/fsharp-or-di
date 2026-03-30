module FSharpOrDi.Configuration.Test.RecordBindingTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.ConfigurationReader
open FSharpOrDi.Configuration.RecordBinding
open TestReaders

// ==========================================================================
// bindRecord
// ==========================================================================

[<Fact>]
let ``bindRecord calls bindField for each field and constructs record with makeRecord`` () =
    // Arrange
    let bindField (fieldName: string) (_fieldType: System.Type) (_reader: ConfigurationReader) =
        Ok(box $"value-for-{fieldName}")

    let makeRecord (_recordType: System.Type) (values: obj array) = box values

    let fields = [| ("Host", typeof<string>); ("Port", typeof<int>) |]

    // Act
    let result = bindRecord bindField makeRecord typeof<obj> fields emptyReader

    // Assert
    let expected = Ok(box [| box "value-for-Host"; box "value-for-Port" |])
    Assert.Equal(expected, result)

[<Fact>]
let ``bindRecord passes sub-section reader for each field`` () =
    // Arrange
    let reader =
        {
            GetValue = fun _ -> None
            GetSection = fun key ->
                {
                    GetValue = fun _ -> Some key
                    GetSection = fun _ -> failwith "unused"
                    GetChildren = fun () -> []
                }
            GetChildren = fun () -> []
        }

    let mutable capturedFieldReader = None

    let bindField (fieldName: string) (_fieldType: System.Type) (fieldReader: ConfigurationReader) =
        capturedFieldReader <- Some(fieldName, fieldReader)
        Ok(box "value")

    let makeRecord (_recordType: System.Type) (values: obj array) = box values
    let fields = [| ("Host", typeof<string>) |]

    // Act
    let _result = bindRecord bindField makeRecord typeof<obj> fields reader

    // Assert
    let (name, capturedReader) = capturedFieldReader.Value
    Assert.Equal("Host", name)
    Assert.Equal(Some "Host", capturedReader.GetValue "anything")

[<Fact>]
let ``bindRecord accumulates errors from all fields`` () =
    // Arrange
    let bindField (fieldName: string) (_fieldType: System.Type) (_reader: ConfigurationReader) =
        Error [ MissingRequiredValue fieldName ]

    let makeRecord (_recordType: System.Type) (_: obj array) = failwith "should not be called"
    let fields = [| ("Host", typeof<string>); ("Port", typeof<int>) |]

    // Act
    let result = bindRecord bindField makeRecord typeof<obj> fields emptyReader

    // Assert
    let expected = Error [ MissingRequiredValue "Host"; MissingRequiredValue "Port" ]
    Assert.Equal(expected, result)

[<Fact>]
let ``bindRecord returns Ok when all fields bind successfully`` () =
    // Arrange
    let bindField (_: string) (_: System.Type) (_: ConfigurationReader) = Ok(box "value")
    let makeRecord (_recordType: System.Type) (values: obj array) = box values
    let fields = [| ("Name", typeof<string>) |]

    // Act
    let result = bindRecord bindField makeRecord typeof<obj> fields emptyReader

    // Assert
    Assert.True(Result.isOk result)
