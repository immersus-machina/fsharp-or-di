module OpticalContract.Illumination

// C# equivalent:
//
// interface IIlluminate
// {
//     IlluminatedSpecimen Illuminate(Specimen specimen);
// }
//
// class IlluminationService : IIlluminate
// {
//     public IlluminatedSpecimen Illuminate(Specimen specimen) =>
//         // ...
// }

type Illuminate = Specimen -> IlluminatedSpecimen

let illuminate: Illuminate =
    fun specimen ->
        let aperture = specimen.Complexity * 2.0

        { OriginalName = specimen.Name
          Aperture = Aperture aperture
          Brightness = 100.0 / (specimen.Complexity + 1.0) }
