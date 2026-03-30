module FSharpOrDi.Configuration.Test.ConfigurationBindingIntegrationTests

open Xunit
open Microsoft.Extensions.Configuration
open FSharpOrDi
open FSharpOrDi.Configuration.BindingError
open FSharpOrDi.Configuration.BindingErrorFormatting
open FSharpOrDi.Configuration.ConfigurationBinding

// ==========================================================================
// Domain types for integration tests
// ==========================================================================

type Port = Port of int
type ConnectionTimeout = ConnectionTimeout of int

type DatabaseConfiguration =
    {
        Host: string
        Port: Port
        ConnectionTimeout: ConnectionTimeout option
    }

type ApplicationConfiguration =
    {
        Database: DatabaseConfiguration
        AppName: string
    }

type TaggedConfiguration = { Name: string; Tags: string list }

type ArrayConfiguration = { Name: string; Scores: int array }

type AllPrimitivesConfiguration =
    {
        Name: string
        Count: int
        LargeNumber: int64
        Rate: float
        Price: decimal
        IsEnabled: bool
        Identifier: System.Guid
        Duration: System.TimeSpan
        Timestamp: System.DateTimeOffset
    }

// ==========================================================================
// Helper
// ==========================================================================

let private buildConfiguration (values: (string * string) list) =
    let builder = ConfigurationBuilder()

    let keyValuePairs =
        values
        |> List.map (fun (key, value) -> System.Collections.Generic.KeyValuePair(key, value))

    builder
        .AddInMemoryCollection(keyValuePairs)
        .Build()

// ==========================================================================
// All primitive types
// ==========================================================================

[<Fact>]
let ``bind returns record with all supported primitive types`` () =
    // Arrange
    let configuration =
        buildConfiguration
            [ ("S:Name", "test")
              ("S:Count", "42")
              ("S:LargeNumber", "9999999999")
              ("S:Rate", "3.14")
              ("S:Price", "99.99")
              ("S:IsEnabled", "true")
              ("S:Identifier", "d3b07384-d9a0-4c9b-8a3e-000000000001")
              ("S:Duration", "01:30:00")
              ("S:Timestamp", "2026-03-30T12:00:00+00:00") ]

    // Act
    let result: Result<AllPrimitivesConfiguration, BindingError list> =
        configuration.GetSection("S") |> bind<AllPrimitivesConfiguration>

    // Assert
    let expected =
        Ok
            { Name = "test"
              Count = 42
              LargeNumber = 9999999999L
              Rate = 3.14
              Price = 99.99m
              IsEnabled = true
              Identifier = System.Guid.Parse("d3b07384-d9a0-4c9b-8a3e-000000000001")
              Duration = System.TimeSpan(1, 30, 0)
              Timestamp = System.DateTimeOffset(2026, 3, 30, 12, 0, 0, System.TimeSpan.Zero) }

    Assert.Equal(expected, result)

// ==========================================================================
// Record with nested records
// ==========================================================================

[<Fact>]
let ``bind returns record with primitive fields from configuration section`` () =
    // Arrange
    let configuration =
        buildConfiguration
            [ ("MySection:AppName", "TestApp")
              ("MySection:Database:Host", "localhost")
              ("MySection:Database:Port", "5432") ]

    // Act
    let result: Result<ApplicationConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<ApplicationConfiguration>

    // Assert
    let expected =
        Ok
            { AppName = "TestApp"
              Database =
                { Host = "localhost"
                  Port = Port 5432
                  ConnectionTimeout = None } }

    Assert.Equal(expected, result)

// ==========================================================================
// Option fields
// ==========================================================================

[<Fact>]
let ``bind returns record with None for missing optional field`` () =
    // Arrange
    let configuration =
        buildConfiguration [ ("MySection:Host", "localhost"); ("MySection:Port", "5432") ]

    // Act
    let result: Result<DatabaseConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<DatabaseConfiguration>

    // Assert
    let expected =
        Ok
            { Host = "localhost"
              Port = Port 5432
              ConnectionTimeout = None }

    Assert.Equal(expected, result)

[<Fact>]
let ``bind returns record with Some for present optional field`` () =
    // Arrange
    let configuration =
        buildConfiguration
            [ ("MySection:Host", "localhost")
              ("MySection:Port", "5432")
              ("MySection:ConnectionTimeout", "30") ]

    // Act
    let result: Result<DatabaseConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<DatabaseConfiguration>

    // Assert
    let expected =
        Ok
            { Host = "localhost"
              Port = Port 5432
              ConnectionTimeout = Some(ConnectionTimeout 30) }

    Assert.Equal(expected, result)

// ==========================================================================
// List fields
// ==========================================================================

[<Fact>]
let ``bind returns record with list field from configuration section`` () =
    // Arrange
    let configuration =
        buildConfiguration
            [ ("MySection:Name", "Tagged")
              ("MySection:Tags:0", "alpha")
              ("MySection:Tags:1", "beta")
              ("MySection:Tags:2", "gamma") ]

    // Act
    let result: Result<TaggedConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<TaggedConfiguration>

    // Assert
    let expected =
        Ok
            { Name = "Tagged"
              Tags = [ "alpha"; "beta"; "gamma" ] }

    Assert.Equal(expected, result)

// ==========================================================================
// Array fields
// ==========================================================================

[<Fact>]
let ``bind returns record with array field from configuration section`` () =
    // Arrange
    let configuration =
        buildConfiguration
            [ ("MySection:Name", "Scored")
              ("MySection:Scores:0", "10")
              ("MySection:Scores:1", "20")
              ("MySection:Scores:2", "30") ]

    // Act
    let result: Result<ArrayConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<ArrayConfiguration>

    // Assert
    let expected =
        Ok
            { Name = "Scored"
              Scores = [| 10; 20; 30 |] }

    Assert.Equal(expected, result)

// ==========================================================================
// Error cases
// ==========================================================================

[<Fact>]
let ``bind returns error when required field is missing`` () =
    // Arrange
    let configuration =
        buildConfiguration [ ("MySection:Host", "localhost") ]

    // Act
    let result: Result<DatabaseConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<DatabaseConfiguration>

    // Assert
    Assert.Equal(Error [ MissingRequiredValue "Port" ], result)

[<Fact>]
let ``bind accumulates multiple errors for multiple missing fields`` () =
    // Arrange
    let configuration = buildConfiguration []

    // Act
    let result: Result<DatabaseConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<DatabaseConfiguration>

    // Assert
    let expected = Error [ MissingRequiredValue "Host"; MissingRequiredValue "Port" ]
    Assert.Equal(expected, result)

[<Fact>]
let ``bind returns error when value cannot be parsed`` () =
    // Arrange
    let configuration =
        buildConfiguration [ ("MySection:Host", "localhost"); ("MySection:Port", "not-a-number") ]

    // Act
    let result: Result<DatabaseConfiguration, BindingError list> =
        configuration.GetSection("MySection") |> bind<DatabaseConfiguration>

    // Assert
    match result with
    | Error errors ->
        let hasConversionError =
            errors
            |> List.exists (fun error ->
                match error with
                | ValueConversionFailed(_, "not-a-number", _) -> true
                | _ -> false)

        Assert.True(hasConversionError)
    | Ok _ -> Assert.Fail "Expected Error but got Ok"

// ==========================================================================
// registerBind
// ==========================================================================

[<Fact>]
let ``registerBind registers bound configuration into function registry`` () =
    // Arrange
    let configuration =
        buildConfiguration
            [ ("MySection:Host", "localhost")
              ("MySection:Port", "5432") ]

    // Act
    let graph =
        configuration.GetSection("MySection") |> registerBind<DatabaseConfiguration>
        |> FunctionRegistry.build

    // Assert
    let resolved: DatabaseConfiguration = FunctionGraph.resolve graph

    let expected =
        { Host = "localhost"
          Port = Port 5432
          ConnectionTimeout = None }

    Assert.Equal(expected, resolved)

[<Fact>]
let ``registerBind throws with formatted error message when binding fails`` () =
    // Arrange
    let configuration = buildConfiguration []

    // Act & Assert
    let exn =
        Assert.Throws<System.Exception>(fun () ->
            configuration.GetSection("MySection") |> registerBind<DatabaseConfiguration>
            |> FunctionRegistry.build
            |> ignore)

    Assert.Contains("Configuration binding failed:", exn.Message)
    Assert.Contains("Missing required value: 'Host'", exn.Message)
    Assert.Contains("Missing required value: 'Port'", exn.Message)
