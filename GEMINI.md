# AmazData Project Overview

This repository contains the source code for the **AmazData** project, a .NET 8.0 C# solution. The core of the project is an **Orchard Core CMS** application (`AmazData.Web`) extended with a custom module (`AmazData.Module.Mqtt`) for MQTT (Message Queuing Telemetry Transport) data integration.

## Project Structure

*   **`AmazData.Web`**: The main Orchard Core CMS application. It serves as the entry point for the web application and hosts the custom MQTT module.
*   **`AmazData.Module.Mqtt`**: A custom Orchard Core module responsible for handling MQTT communication. It utilizes the `MQTTnet` library for MQTT client functionalities.
*   **`ConsoleApp1`**: A simple console application, likely used for testing or utility purposes.

## Key Technologies

*   **C#**: The primary programming language.
*   **.NET 8.0**: The target framework for all projects.
*   **ASP.NET Core**: The web framework used by `AmazData.Web`.
*   **Orchard Core CMS**: A free, open-source, modular, and extensible CMS framework built on ASP.NET Core.
*   **MQTTnet**: A high-performance .NET library for MQTT client and server communication, used within `AmazData.Module.Mqtt`.
*   **NLog**: A flexible and free .NET logging platform, used for application logging.

## Building and Running

### Prerequisites

*   .NET 8.0 SDK installed.

### Build the Project

To build the entire solution, navigate to the root directory of the repository and execute:

```bash
dotnet build
```

### Run the Web Application

To run the `AmazData.Web` application, execute the following command from the root directory:

```bash
dotnet run --project AmazData.Web
```

For development with hot-reloading, you can use:

```bash
dotnet watch run --project AmazData.Web
```

Upon the first run, the Orchard Core CMS setup screen will be displayed in your browser. Follow the instructions to configure your site.

## Development Conventions

*   Follows standard .NET/C# coding conventions and best practices.
*   Orchard Core module development guidelines are applied for the `AmazData.Module.Mqtt` project.
*   Implicit usings and nullable reference types are enabled.
