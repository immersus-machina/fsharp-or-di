module FSharpOrDi.Configuration.Test.CollectionBindingTests

open Xunit
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.ConfigurationReader
open FSharpOrDi.Configuration.CollectionBinding
open TestReaders

let private makeChildReader (value: string) =
    {
        GetValue = fun _ -> Some value
        GetSection = fun _ -> failwith "unused"
        GetChildren = fun () -> []
    }

let private anyFieldName = "Tags"

// ==========================================================================
// bindCollection
// ==========================================================================

[<Fact>]
let ``bindCollection returns empty collection when no children exist`` () =
    // Arrange
    let bindElement (_: string) (_: System.Type) (_: ConfigurationReader) =
        failwith "should not be called"

    let buildCollection (_elementType: System.Type) (values: obj list) = box values

    // Act
    let result = bindCollection bindElement buildCollection anyFieldName typeof<string> emptyReader

    // Assert
    Assert.Equal(Ok(box []), result)

[<Fact>]
let ``bindCollection binds each child and returns populated collection`` () =
    // Arrange
    let bindElement (_: string) (_: System.Type) (reader: ConfigurationReader) =
        match reader.GetValue "" with
        | Some value -> Ok(box value)
        | None -> Error [ MissingRequiredValue "element" ]

    let buildCollection (_elementType: System.Type) (values: obj list) = box values

    let readerWithChildren =
        { emptyReader with
            GetChildren = fun () -> [ makeChildReader "alpha"; makeChildReader "beta"; makeChildReader "gamma" ]
        }

    // Act
    let result = bindCollection bindElement buildCollection anyFieldName typeof<string> readerWithChildren

    // Assert
    let expected = Ok(box [ box "alpha"; box "beta"; box "gamma" ])
    Assert.Equal(expected, result)

[<Fact>]
let ``bindCollection accumulates errors from all elements`` () =
    // Arrange
    let bindElement (fieldName: string) (_: System.Type) (_: ConfigurationReader) =
        Error [ MissingRequiredValue fieldName ]

    let buildCollection (_elementType: System.Type) (_: obj list) = failwith "should not be called"

    let readerWithChildren =
        { emptyReader with
            GetChildren = fun () -> [ emptyReader; emptyReader ]
        }

    // Act
    let result = bindCollection bindElement buildCollection anyFieldName typeof<string> readerWithChildren

    // Assert
    let expected = Error [ MissingRequiredValue $"{anyFieldName}[0]"; MissingRequiredValue $"{anyFieldName}[1]" ]
    Assert.Equal(expected, result)
