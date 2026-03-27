open OpticalContract
open FSharpOrDi.FunctionRegistry
open FSharpOrDi.FunctionGraph

let graph =
    register Refraction.refract
    >> register Illumination.illuminate
    >> register Inspection.inspect
    |> build

let inspect: Specimen -> InspectionReport = resolve graph

let report =
    inspect
        { Name = "Crystal"
          Complexity = 3.5
          ViewDirection = MiddleOut }

printfn "%A" report
