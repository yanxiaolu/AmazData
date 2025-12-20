FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV TZ=Asia/Shanghai

RUN apk add --no-cache tzdata \
    && cp /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo "Asia/Shanghai" > /etc/timezone \
    && apk del tzdata

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /
COPY ["AmazData.Web/AmazData.Web.csproj", "AmazData.Web/"]
COPY ["AmazData.Module.Mqtt/AmazData.Module.Mqtt.csproj", "AmazData.Module.Mqtt/"]
RUN dotnet restore "AmazData.Web/AmazData.Web.csproj"
COPY . .
WORKDIR "/AmazData.Web"
RUN dotnet build "AmazData.Web.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "AmazData.Web.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AmazData.Web.dll"]
