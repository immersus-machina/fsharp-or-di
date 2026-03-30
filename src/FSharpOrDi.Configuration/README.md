# FSharpOrDi.Configuration

Binds `IConfigurationSection` to F# record types with error accumulation. Designed to integrate with [FSharpOrDi](../../README.md) — register configuration values directly into the function registry.

## Installation

ToDo
<!-- ```bash
dotnet add package FSharpOrDi.Configuration
``` -->

## Usage

Given a configuration record using idiomatic F# types:

```fsharp
type Port = Port of int
type ConnectionTimeout = ConnectionTimeout of int

type DatabaseConfiguration =
    {
        Host: string
        Port: Port
        ConnectionTimeout: ConnectionTimeout option
    }
```

### Bind to a Result

```fsharp
open FSharpOrDi.Configuration.ConfigurationBinding

let result: Result<DatabaseConfiguration, BindingError list> =
    configuration.GetSection("Database") |> bind<DatabaseConfiguration>
```

### Register directly into FSharpOrDi

```fsharp
open FSharpOrDi.Configuration.ConfigurationBinding

let graph =
    registerBind<DatabaseConfiguration> (configuration.GetSection("Database"))
    |> FunctionRegistry.build
```

`registerBind` binds the section and registers the result in one step. If binding fails, it throws with a formatted error message.

## Supported types

| Type | Example |
| --- | --- |
| Primitives | `string`, `int`, `int64`, `float`, `decimal`, `bool` |
| System types | `Guid`, `TimeSpan`, `DateTimeOffset` |
| Records | Nested records bind recursively |
| Single-case DUs | `Port of int` — unwraps and parses the inner value |
| Options | `ConnectionTimeout option` — `None` when the key is absent |
| Lists | `string list` — from indexed keys (`Tags:0`, `Tags:1`, ...) |
| Arrays | `int array` — same indexed key format as lists |

## Error handling

`bind` returns `Result<'T, BindingError list>`. Errors accumulate — all problems are reported at once, not just the first.

```fsharp
type BindingError =
    | MissingRequiredValue of fieldName: string
    | ValueConversionFailed of fieldName: string * rawValue: string * targetTypeName: string
    | UnsupportedTargetType of typeName: string
```

Format errors for display:

```fsharp
open FSharpOrDi.Configuration.BindingErrorFormatting

match result with
| Ok config -> // use config
| Error errors -> printfn "%s" (formatErrors errors)
// Configuration binding failed:
// - Missing required value: 'Host'
// - Cannot convert 'abc' to Int32 for field 'Port'
```
