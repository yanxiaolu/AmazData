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

The project follows standard OrchardCore module development conventions. Key patterns and practices are outlined below.

## MQTTnet Library Version

Due to persistent compatibility issues and compilation errors encountered with `MQTTnet` version 5.x within this OrchardCore module, the project currently uses **`MQTTnet` version 4.3.7** and **`MQTTnet.Extensions.ManagedClient` version 4.3.7.1207**. Attempts to upgrade to version 5.x were unsuccessful, indicating a deeper integration challenge that requires further investigation beyond simple code refactoring.

## Service Registration

Services are registered in the `ConfigureServices` method of the module's `Startup.cs` file.

-   **Singleton Services**: Services that manage a shared state across the application, like `IMqttConnectionManager`, are registered as singletons.
    ```csharp
    services.AddSingleton<IMqttConnectionManager, MqttConnectionManager>();
    ```
-   **Scoped Services**: Services that are created once per request, like `IMqttOptionsBuilderService` and `IMqttSubscriptionManager`, are registered as scoped. This is crucial to avoid dependency injection scope validation errors when consuming scoped services (e.g., `IContentManager`) from these managers.
    ```csharp
    services.AddScoped<IMqttOptionsBuilderService, MqttOptionsBuilderService>();
    services.AddScoped<IMqttSubscriptionManager, MqttSubscriptionManager>();
    ```

## Display Drivers and Shapes

Display Drivers are used to create and place UI components (Shapes) in specific locations.

-   **Creating Shapes**: To add a shape to a view (e.g., the "SummaryAdmin" view of a content item), create a class that inherits from `ContentDisplayDriver`.
-   **Passing Data to Shapes**: The recommended pattern is to use a strongly-typed ViewModel.
    1.  Create a simple ViewModel class to hold the data for the shape.
    2.  In the driver, use the `Initialize<TViewModel>(shapeName, model => { ... })` method to create the shape. This method creates a new instance of your ViewModel and allows you to populate it.
    3.  In the shape's Razor view (`.cshtml`), use the `@model` directive to declare it as a strongly-typed view.

    **Example (`MqttBrokerButtonsDisplayDriver.cs`):**
    ```csharp
    // 1. ViewModel exists (MqttBrokerButtonsViewModel)
    // 2. Driver uses Initialize<TViewModel>
    return Initialize<MqttBrokerButtonsViewModel>("MqttBrokerButtons_Start", model => model.ContentItem = contentItem)
        .Location("SummaryAdmin", "Actions:10");
    ```
    **Example (`MqttBrokerButtons.Start.cshtml`):**
    ```csharp
    @model AmazData.Module.Mqtt.Models.MqttBrokerButtonsViewModel
    @{
        if (Model.ContentItem == null) { return; }
    }
    <a href="..." class="btn">...</a>
    ```

## Controllers and Actions

-   **Dependency Injection**: Controllers use constructor injection to get instances of necessary services (e.g., `IContentManager`, `IMqttConnectionManager`).
-   **Custom Routes**: Custom routes are mapped in the `Configure` method of `Startup.cs` to handle module-specific URLs.
-   **Interacting with Content**: Controllers use `IContentManager` to query, create, and update content items.

## General

*   The main web application is configured in `AmazData.Web/Program.cs`.
*   Logging is configured using NLog in `AmazData.Web/NLog.config`.
*   Custom OrchardCore content types (`Broker`, `Topic`) are used to manage MQTT subscription parameters. These are defined in `Migrations.cs`.