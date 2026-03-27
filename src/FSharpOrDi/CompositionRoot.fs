module internal FSharpOrDi.CompositionRoot

open Microsoft.FSharp.Reflection

let decomposeType systemType =
    TypeDecomposition.decomposeType FSharpType.IsFunction FSharpType.GetFunctionElements systemType

let reconstructType signature =
    TypeDecomposition.reconstructType
        (fun inputType outputType -> FSharpType.MakeFunctionType(inputType, outputType))
        signature

let composeFunctions firstImplementation secondImplementation composedSystemType =
    RuntimeInvocation.composeFunctions
        (fun systemType converter -> FSharpValue.MakeFunction(systemType, converter))
        firstImplementation
        secondImplementation
        composedSystemType

let formatOriginShallow =
    Formatting.formatOriginShallow (ResolutionGraph.signatureOf >> Formatting.formatSignature)

let failIfAlreadyRegistered =
    AmbiguityDetection.failIfAlreadyRegistered Formatting.formatSignature

let filterCandidateAgainstExistingStage =
    AmbiguityDetection.filterCandidateAgainstExistingStage Formatting.formatSignature formatOriginShallow

let deduplicateBatch =
    AmbiguityDetection.deduplicateBatch Formatting.formatSignature formatOriginShallow

let describeMissingDependencies =
    Diagnostics.describeMissingDependencies Formatting.formatSignature

let cycleDetector = CycleDetection.detectCycles Formatting.formatSignature
