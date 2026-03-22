module ErrorMessages.Functions

let readTemperature: SensorId -> TemperatureReading =
    fun (SensorId id) -> { Temperature = float id * 10.0 }

let readTemperatureAlternate: SensorId -> TemperatureReading =
    fun (SensorId id) -> { Temperature = float id * 20.0 }

let readPressure: SensorId -> PressureReading =
    fun (SensorId id) -> { Pressure = float id * 100.0 }

let readHumidity: SensorId -> HumidityReading =
    fun (SensorId id) -> { Humidity = float id * 0.5 }

let combineWeather: (SensorId -> HumidityReading) -> (SensorId -> WindReading) -> SensorId -> CombinedReading =
    fun readHumid readWind (SensorId id) ->
        let humid = readHumid (SensorId id)
        let wind = readWind (SensorId id)
        { Temperature = humid.Humidity; Pressure = wind.WindSpeed }

let combineSensors: (SensorId -> TemperatureReading) -> (SensorId -> PressureReading) -> SensorId -> CombinedReading =
    fun readTemp readPress (SensorId id) ->
        let temp = readTemp (SensorId id)
        let press = readPress (SensorId id)
        { Temperature = temp.Temperature; Pressure = press.Pressure }

let viaRouteA: (SensorId -> RouteAResult) -> SensorId -> FinalResult =
    fun getA id -> { Value = (getA id).ViaA }

let viaRouteB: (SensorId -> RouteBResult) -> SensorId -> FinalResult =
    fun getB id -> { Value = (getB id).ViaB }

let makeRouteA: SensorId -> RouteAResult =
    fun (SensorId id) -> { ViaA = float id }

let makeRouteB: SensorId -> RouteBResult =
    fun (SensorId id) -> { ViaB = float id * 2.0 }

let produceA: (SensorId -> RouteBResult) -> SensorId -> RouteAResult =
    fun _getB (SensorId id) -> { ViaA = float id }

let produceB: (SensorId -> RouteAResult) -> SensorId -> RouteBResult =
    fun _getA (SensorId id) -> { ViaB = float id }

let stepOne: SensorId -> StepOneResult =
    fun (SensorId id) -> { StepOne = float id }

let chainViaA: StepOneResult -> RouteAResult =
    fun s -> { ViaA = s.StepOne }

let chainViaB: StepOneResult -> RouteBResult =
    fun s -> { ViaB = s.StepOne * 2.0 }

let finishFromA: RouteAResult -> FinalResult =
    fun a -> { Value = a.ViaA }

let finishFromB: RouteBResult -> FinalResult =
    fun b -> { Value = b.ViaB }
