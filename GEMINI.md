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
*   **Npgsql**: PostgreSQL data provider for .NET (used for high-frequency telemetry).
*   **NLog**: Flexible logging platform.

## Key API Endpoints

### PlcStat Module
*   **`GET /api/plcstat/count`**: Returns the total record count in the database.
*   **`GET /api/plcstat/trend`**: Retrieves sensor trend data (last N days).
    *   Parameters: `DeviceId`, `SensorName`, `Days` (default 7, max 30), `Granularity` (`Hour`|`Day`).
*   **`GET /api/plcstat/trend-range`**: Retrieves sensor trend data for a specific date range.
    *   Parameters: `DeviceId`, `SensorName`, `StartTime`, `EndTime`, `Granularity` (`Hour`|`Day`).
    *   *Note: Query range is limited to 30 days.*

### Yunmou Module
*   **`GET /api/yunmou/video`**: Retrieves HLS live stream addresses.
    *   Parameters: `deviceSerial`, `channelNo` (default 1).
    *   *Note: Requires `YuMouKeyManage` content item configuration in the CMS.*

## Building and Running

### Prerequisites
*   .NET 8.0 SDK installed.
*   PostgreSQL database (for `PlcStat` telemetry data).

### Build and Run
```bash
dotnet build
dotnet run --project AmazData.Web
```

## Development Conventions

*   **Hybrid Storage Strategy**: 
    *   **Orchard Core (SQLite/PostgreSQL)**: Stores CMS content, metadata, and business configurations.
    *   **External PostgreSQL (TSDB)**: Stores high-frequency MQTT/PLC telemetry (e.g., `public.plcdata_hourly_rollup`).
*   **Timezone & Localization**:
    *   Use `DateTimeOffset` for time representation.
    *   API parameters must be converted to UTC before querying PostgreSQL (Npgsql driver requirement).
    *   SQL queries utilize `AT TIME ZONE 'Asia/Shanghai'` to align with China's natural day boundaries for statistical aggregation.
*   **Error Handling**:
    *   Internal modules should return detailed JSON error responses for API consumers.
    *   Upstream API failures (e.g., Yunmou) should return appropriate status codes (e.g., 424 Failed Dependency).
*   **Standard Practices**:
    *   Follows standard .NET/C# coding conventions.
    *   Orchard Core module development guidelines.
    *   Implicit usings and nullable reference types enabled.
