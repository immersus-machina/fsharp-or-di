namespace OpticalContract

type ViewDirection =
    | TopToBottom
    | MiddleOut
    | InsideOut

type RefractionIndex = RefractionIndex of float
type Aperture = Aperture of float

type Specimen =
    { Name: string
      Complexity: float
      ViewDirection: ViewDirection }

type RefractedSpecimen =
    { OriginalName: string
      RefractionIndex: RefractionIndex
      Clarity: float }

type IlluminatedSpecimen =
    { OriginalName: string
      Aperture: Aperture
      Brightness: float }

type InspectionReport =
    { Verdict: string
      DetailLevel: float }
