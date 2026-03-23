# fsharp-or-di

The name is OR - the concept is AND - functional DI for F# in ~500 lines of code. Now even hardcore F#-or-die people can get their DI.

## Installation

Requires .NET 10+.

```bash
dotnet add package FSharpOrDi
```

## Quick Start

```fsharp
let readTemperature: SensorId -> TemperatureReading = ...
let readPressure: SensorId -> PressureReading = ...
let combineSensors: (SensorId -> TemperatureReading) -> (SensorId -> PressureReading) -> SensorId -> CombinedReading = ...

let registry =
    FunctionRegistry.empty
    |> FunctionRegistry.register readTemperature
    |> FunctionRegistry.register readPressure
    |> FunctionRegistry.register combineSensors

let combined: SensorId -> CombinedReading = FunctionRegistry.resolve registry
```

`combineSensors` needs a `SensorId -> TemperatureReading` and a `SensorId -> PressureReading`. Well-engineered signatures make the dependency graph explicit — the library resolves the rest.

## Coming from C#?

The pattern maps directly. Here's the same thing in both languages:

```csharp
// C# — interface + class + constructor injection
interface IReadTemperature { TemperatureReading Read(SensorId id); }
interface IReadPressure { PressureReading Read(SensorId id); }

class CombineSensors(IReadTemperature temp, IReadPressure press)
{
    public CombinedReading Combine(SensorId id) => // use temp and press ...
}

services.AddSingleton<IReadTemperature, TemperatureService>();
services.AddSingleton<IReadPressure, PressureService>();
services.AddSingleton<CombineSensors>();
```

```fsharp
// F# — type alias + function + signature-based injection
type ReadTemperature = SensorId -> TemperatureReading
type ReadPressure = SensorId -> PressureReading

let readTemperature: ReadTemperature = fun id -> ...
let readPressure: ReadPressure = fun id -> ...
let combineSensors: ReadTemperature -> ReadPressure -> SensorId -> CombinedReading = fun temp press id -> ...

FunctionRegistry.empty
|> register readTemperature
|> register readPressure
|> register combineSensors
```

The type alias is the interface. The function is the implementation. The parameters are the constructor.

## Composition Chaining

Single-argument functions compose automatically with `resolveComposed`:

```fsharp
|> register QuantumSplitter.split           // RawQuantumEnergy -> SplitQuantumEnergy
|> register CoherentExtractor.extract       // SplitQuantumEnergy -> CoherentEnergy
|> register HarmonicTuner.tune              // CoherentEnergy -> HarmonizedEnergy
|> register ShieldCalibrator.calibrate      // HarmonizedEnergy -> CalibratedShieldEnergy

// The library chains: split >> extract >> tune >> calibrate
// Producing: RawQuantumEnergy -> CalibratedShieldEnergy
let shieldEnergy: RawQuantumEnergy -> CalibratedShieldEnergy = FunctionRegistry.resolveComposed registry
```

No manual `>>` composition needed. Register the building blocks, request the result type, the library figures out the chain. See the [KeplerStation](examples/KeplerStation/) example for a full-scale demonstration.

## Examples

- **[OpticalContract](examples/OpticalContract/)** — The simplest case. Three functions, one resolve. Each file includes C# interface comments showing the equivalent pattern. Start here if you're coming from C#.

- **[KeplerStation](examples/KeplerStation/)** — 66 registered functions, partial application and composition chaining working together. A realistic-scale example.

- **[ErrorMessages](examples/ErrorMessages/)** — What happens when things go wrong. Duplicate registrations, missing dependencies, ambiguity, and cycles — each with the error message the library produces.

## Performance

Benchmarks on the [KeplerStation](examples/KeplerStation) example (66 registered functions, including composition chaining). Results will vary by machine.

| Operation | Mean | Allocated |
| --- | --- | --- |
| Register 66 functions | 1.4 ms | 561 KB |
| Resolve (build graph) | 12.7 ms | 13 MB |
| Full pipeline (register + resolve + invoke) | 14.6 ms | 13.7 MB |

Resolution is a startup cost — it happens once. The returned function runs at full speed with no reflection on the call path. See [benchmark/](benchmark/) to reproduce.

## Motivation

Coming from C#, dependency injection is a given — you register services, the container wires them, and you don't think twice about it. Moving to F#, the story was surprisingly different:

1. **Use C#'s DI** (`Microsoft.Extensions.DependencyInjection`) — it works, but you're back to interfaces and classes. It doesn't feel like F#.
2. **Pass functions manually** — the standard approach. But is it standard because it's the best option, or because there hasn't been a better one?

Neither option felt satisfying. A functional language should have a functional answer to dependency injection.

### The observation

In F#, when functions use **distinct types**, their signatures become unambiguous identifiers. A well-engineered `Addend -> Addend -> Sum` can only mean one thing. A registry can use this to resolve dependencies automatically — no names, no interfaces, just signatures.

Scala has had this at the language level since 2004 (`implicit`, later refined to `given`/`using` in Scala 3). The compiler searches scope for values matching required types and injects them. It's compile-time, zero-cost, and central to how Scala code is written.

F# has no equivalent. There's been an [open language suggestion](https://github.com/fsharp/fslang-suggestions/issues/243) for type classes / implicits since 2014 with 300+ upvotes — but it's never been implemented.

### The signature is the contract

Signature-based resolution has a useful side effect: it incentivizes good type design.

Generic signatures like `int -> int` are an anti-pattern — they're ambiguous and would collide. The model pushes you toward domain-specific types: not `int -> int` but `Celsius -> Fahrenheit`. Each function's signature becomes self-documenting.

Visibility follows naturally too. If a library defines an internal type `MyInternalResult`, consumers can't request `Event -> MyInternalResult` from the registry — they can't even name the type. F#'s existing access modifiers already control what's resolvable.

This is the same discipline the Scala community learned: registering `given (Int => Int)` as an implicit causes chaos — which `Int => Int` is it? The convention became: use distinct types like `Temperature => Fahrenheit` so every signature is unambiguous.

## Engineered Graph

A dependency graph isn't arbitrary — it's a deliberate expression of your domain architecture. The type signatures you choose define which functions connect, which subsystems depend on each other, and how data flows through the system. That's engineering, not convention.

The library takes this seriously. When you call `resolve` or `resolveComposed`, the graph is built and validated before anything runs:

- **Duplicate registrations** are rejected immediately — one signature, one provider
- **Ambiguous derivations** throw — if two paths produce the same type, the design is unclear
- **Cycles** are detected and rejected — circular dependencies are a design error
- **Missing dependencies** produce diagnostic traces — which types are missing, which registered functions could produce them, and what *those* functions need

The graph either resolves cleanly or tells you exactly what's wrong. No silent failures, no runtime surprises.

### Design principles

- **Distinct types prevent ambiguity.** The registry doesn't guess. If two derivation paths produce the same type, that's a configuration error — fix the types or remove a registration.
- **The user's type design determines what gets wired.** There's no library configuration, no "scopes," no "lifetimes." The types are the configuration.
- **Errors are configuration errors.** A failed resolve means the registrations are wrong, not that something went wrong at runtime. The library throws — these aren't conditions to recover from, they're bugs to fix.
- **The library enables synthesis but doesn't force it.** If your registered function signatures are all distinct, no synthesis happens — just direct resolution. If you engineer signatures where outputs feed into inputs, the library composes them automatically. The choice is in the type design, not in a library setting.

## Limitations

- **Runtime resolution**, not compile-time. Unlike Scala's `given`/`using`, errors are caught at startup, not at build time.
- **Reflection-based** startup cost. The graph is built using .NET reflection. Once resolved, the returned function runs at full speed — no reflection on the hot path.
- **IDE support matches the usage pattern.** Direct registrations have full "go to definition" support — same as C# interfaces. Synthesized functions (derived via partial application or composition chaining) don't have a declaration site to navigate to. Scala doesn't synthesize functions either — its `given`/`using` only resolves explicitly declared instances.
- **Type aliases are documentation, not enforcement.** `type Refract = Specimen -> RefractedSpecimen` is a naming convenience — at the .NET level, it's the same type as any other `Specimen -> RefractedSpecimen`. The compiler won't catch a mislabeled alias.
- **No generic type support.** The library resolves by exact type match. You can't register a generic function and have it resolve for specific type instantiations.
