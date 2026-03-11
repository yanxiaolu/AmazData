# AmazData Project Overview

This repository contains the source code for the **AmazData** project, a .NET 8.0 C# solution. The core of the project is an **Orchard Core CMS** application (`AmazData.Web`) extended with custom modules for data integration and processing.

## Project Structure

*   **`AmazData.Web`**: The main Orchard Core CMS application. It serves as the entry point for the web application and hosts custom modules.
*   **`AmazData.Module.Mqtt`**: Custom Orchard Core module for handling MQTT communication using `MQTTnet`.
*   **`AmazData.Module.PlcStat`**: Provides RESTful APIs for querying PLC historical data and statistics from PostgreSQL.
*   **`AmazData.Module.Yunmou`**: Integration with Hikvision Yunmou (海康云眸) for video stream management.
*   **`ConsoleApp1`**: Utility console application.

## Key Technologies

*   **C# / .NET 8.0**: Primary language and framework.
*   **Orchard Core CMS**: Modular and extensible CMS framework.
*   **MQTTnet**: High-performance MQTT library.
*   **Npgsql**: PostgreSQL data provider for .NET.
*   **NLog**: Flexible logging platform.

## Key API Endpoints

### PlcStat Module
*   **`GET /api/plcstat/count`**: Returns the total record count in the database.
*   **`GET /api/plcstat/trend`**: Retrieves sensor trend data (last N days).
    *   Parameters: `DeviceId`, `SensorName`, `Days` (default 7), `Granularity` (`Hour`|`Day`).
*   **`GET /api/plcstat/trend-range`**: Retrieves sensor trend data for a specific date range.
    *   Parameters: `DeviceId`, `SensorName`, `StartTime`, `EndTime`, `Granularity` (`Hour`|`Day`).

### Yunmou Module
*   **`GET /api/yunmou/video`**: Retrieves HLS live stream addresses.
    *   Parameters: `deviceSerial`, `channelNo` (default 1).
    *   *Note: Requires `YuMouKeyManage` content item configuration in the CMS.*

## Building and Running

### Prerequisites
*   .NET 8.0 SDK installed.

### Build and Run
```bash
dotnet build
dotnet run --project AmazData.Web
```

## Development Conventions

*   Follows standard .NET/C# coding conventions.
*   Orchard Core module development guidelines.
*   Implicit usings and nullable reference types enabled.
