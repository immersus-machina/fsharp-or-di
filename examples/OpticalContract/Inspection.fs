module OpticalContract.Inspection

// C# equivalent:
//
// interface IInspect
// {
//     InspectionReport Inspect(Specimen specimen);
// }
//
// class InspectionService(IRefract refract, IIlluminate illuminate) : IInspect
// {
//     public InspectionReport Inspect(Specimen specimen) =>
//         // use refract and illuminate ...
// }

type Inspect = Refraction.Refract -> Illumination.Illuminate -> Specimen -> InspectionReport

let inspect: Inspect =
    fun refract illuminate specimen ->
        let refracted = refract specimen
        let illuminated = illuminate specimen
        let (RefractionIndex ri) = refracted.RefractionIndex
        let (Aperture ap) = illuminated.Aperture

        { Verdict =
            sprintf
                "Inspected %s via %A: refraction %.1f, aperture %.1f"
                specimen.Name
                specimen.ViewDirection
                ri
                ap
          DetailLevel = refracted.Clarity * illuminated.Brightness / 100.0 }
