module internal FSharpOrDi.Configuration.ValueConversion

open BindingError

let convertValue (fieldName: string) (rawValue: string) (targetType: System.Type) : Result<obj, BindingError> =
    try
        if targetType = typeof<string> then
            Ok(box rawValue)
        elif targetType = typeof<int> then
            Ok(box (System.Int32.Parse(rawValue)))
        elif targetType = typeof<int64> then
            Ok(box (System.Int64.Parse(rawValue)))
        elif targetType = typeof<float> then
            Ok(box (System.Double.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture)))
        elif targetType = typeof<decimal> then
            Ok(box (System.Decimal.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture)))
        elif targetType = typeof<bool> then
            Ok(box (System.Boolean.Parse(rawValue)))
        elif targetType = typeof<System.Guid> then
            Ok(box (System.Guid.Parse(rawValue)))
        elif targetType = typeof<System.TimeSpan> then
            Ok(box (System.TimeSpan.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture)))
        elif targetType = typeof<System.DateTimeOffset> then
            Ok(
                box (
                    System.DateTimeOffset.Parse(rawValue, System.Globalization.CultureInfo.InvariantCulture)
                )
            )
        else
            Error(ValueConversionFailed(fieldName, rawValue, targetType.Name))
    with :? System.FormatException | :? System.OverflowException ->
        Error(ValueConversionFailed(fieldName, rawValue, targetType.Name))
