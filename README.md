# DCScribe

## Introduction

DCSCribe is a program to get unit data and positions out of DCS and into
a database for use by other applications. It connects to a DCS-gRPC server
running inside of a DCS GameServer to get the data.

## Installation

### Database

This application requires access to a PostgreSQL database server with the
PostGIS spatial extensions added. This README assumes you are familiar
with PostgreSQL administration already. 

DCScribe can read from multiple DCS Servers at once and write to a separate
database for each server. Therefore for each DCS-gRPC server you wish to
connect to perform the following steps.

#### Configure the database(s)

Create, or reuse, a database on the PostgresQL server and make sure it is
spatially enabled by running:

```sql
CREATE EXTENSION postgis
```

Then create the `units` and `airbases` tables using the scripts provided in
`Documentation/DatabaseScripts`, making sure to update the owner to the owner
of the database in the script.

### DCScribe Configuration File

Copy the `Documentation\configuration.Sample.yaml` file to `configuration.yaml`
and edit the settings based on the explanatory comments in the file.

### Running the application

After completing the above setup you can run DCSscribe by running
`DCScribe.exe`

### Installing as a service

Note: Before installing as a service make sure DCScribe runs correctly when 
run from the command-line (Or by clicking `DCScribe.exe`)

DCScribe can be installed as a Windows Service. To do so run the following
command in a powershell window with administrative privileges. Make sure
you update the path to point to the location where the executable is.

```powershell
New-Service -Name DCScribe -BinaryPathName c:\YOUR\PATH\TO\DCScribe.exe -Description "Extract data from DigitalCombatSimulator to a PostgreSQL database" -DisplayName "DCScribe" -StartupType Automatic
```

You can then manage the created Windows Service as normal