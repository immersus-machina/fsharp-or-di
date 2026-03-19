# fsharp-or-di

Exploring functional dependency injection for F#.

## Background

Coming from C#, dependency injection is a given — you register services, the container wires them, and you don't think twice about it. Moving to F#, the story is surprisingly different:

1. **Use C#'s DI** (`Microsoft.Extensions.DependencyInjection`) — it works, but you're back to interfaces and classes. It doesn't feel like F#.
2. **Pass functions manually** — the idiomatic answer, and it's fine for small projects. But it doesn't scale the way a DI container does.

Neither option feels right. It seems like there should be a native functional equivalent.

## The observation

In F#, when functions return **distinct types**, their signatures alone identify them. A function `float -> float -> AdditionResult` can only be one thing. A container could use this to resolve and wire dependencies automatically — no names, no interfaces, just types.

Scala has had this at the language level since 2004 (`implicit`, later refined to `given`/`using` in Scala 3). The compiler searches scope for values matching required types and injects them. It's compile-time, zero-cost, and central to how Scala code is written.

F# has no equivalent. There's been an [open language suggestion](https://github.com/fsharp/fslang-suggestions/issues/243) for type classes / implicits since 2014 with 300+ upvotes — but it's never been implemented.

## The signature is the contract

This approach has a useful side effect: it incentivizes good type design.

Generic signatures like `int -> int` are an anti-pattern here — they're ambiguous and would collide in the container. The model pushes you toward domain-specific types: not `int -> int` but `Celsius -> Fahrenheit`. Each function's signature becomes self-documenting.

Visibility follows naturally too. If a library defines an internal type `MyInternalResult`, consumers can't request `Event -> MyInternalResult` from the container — they can't even name the type. No need for an "internal" concept in the container itself. F#'s existing access modifiers already control what's resolvable.

This is the same discipline the Scala community learned: registering `given Int` or `given String` as implicits causes chaos. The convention became: always use distinct wrapper types.

## This project

A humble attempt to explore what functional DI could look like in F# as a library. It won't be compile-time safe like Scala's approach, and it won't replace a proper language feature. But it might help show that the gap exists and is worth closing.

> Do or DI? #F# DI
