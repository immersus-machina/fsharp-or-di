module OpticalContract.Refraction

// C# equivalent:
//
// interface IRefract
// {
//     RefractedSpecimen Refract(Specimen specimen);
// }
//
// class RefractionService : IRefract
// {
//     public RefractedSpecimen Refract(Specimen specimen) =>
//         // ...
// }

type Refract = Specimen -> RefractedSpecimen

let refract: Refract =
    fun specimen ->
        let index = specimen.Complexity * 1.5

        { OriginalName = specimen.Name
          RefractionIndex = RefractionIndex index
          Clarity = 100.0 - specimen.Complexity * 10.0 }
