open OpticalContract
open FSharpOrDi.FunctionRegistry

let registry =
    empty
    |> register Refraction.refract
    |> register Illumination.illuminate
    |> register Inspection.inspect

let inspect: Specimen -> InspectionReport = resolve registry

let report =
    inspect
        { Name = "Crystal"
          Complexity = 3.5
          ViewDirection = MiddleOut }

printfn "%A" report
