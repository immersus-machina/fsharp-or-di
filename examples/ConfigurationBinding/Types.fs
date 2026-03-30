module ConfigurationBinding.Types

type Port = Port of int
type ConnectionTimeout = ConnectionTimeout of int
type MaxRetries = MaxRetries of int

type AppConfiguration =
    { Name: string
      MaxRetries: MaxRetries
      VerboseLogging: bool }

type DatabaseConfiguration =
    { Host: string
      Port: Port
      ConnectionTimeout: ConnectionTimeout option }

type SensorConfiguration =
    { Tags: string list
      Thresholds: float array }

type DatabaseConfigurationValidation = DatabaseConfigurationValidation of unit

type PrintExecuted = PrintExecuted of unit
