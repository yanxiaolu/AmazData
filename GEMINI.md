# Project Overview

This project is a data acquisition system built on the OrchardCore CMS framework. It consists of a main web application (`AmazData.Web`) and a custom module (`AmazData.Module.Mqtt`) for handling MQTT data subscriptions. The application uses .NET 8.

- **AmazData.Web:** The main web application, which is an OrchardCore CMS site. It's configured to use NLog for logging.
- **AmazData.Module.Mqtt:** This module is responsible for subscribing to MQTT topics. It uses a custom OrchardCore content type to create and manage the parameters required for multiple MQTT subscriptions.

# Building and Running

To build and run this project, you will need the .NET 8 SDK.

1. **Restore Dependencies:**
   ```bash
   dotnet restore AmazData.slnx
   ```

2. **Build the Solution:**
   ```bash
   dotnet build AmazData.slnx
   ```

3. **Run the Web Application:**
   ```bash
   dotnet run --project AmazData.Web/AmazData.Web.csproj
   ```

The application will be accessible at the URL specified in the console output (usually `https://localhost:5001`).

# Development Conventions

* The project follows the standard OrchardCore module development conventions.
* The `AmazData.Module.Mqtt` module has its own controllers, views, and startup configuration.
* The main web application is configured in `AmazData.Web/Program.cs`.
* Logging is configured using NLog in `AmazData.Web/NLog.config`.
* A custom OrchardCore content type is used to manage MQTT subscription parameters.