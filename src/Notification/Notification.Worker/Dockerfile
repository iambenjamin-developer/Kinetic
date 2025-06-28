FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Notification/Notification.Worker/Notification.Worker.csproj", "src/Notification/Notification.Worker/"]
COPY ["src/Notification/Notification.Application/Notification.Application.csproj", "src/Notification/Notification.Application/"]
COPY ["src/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj", "src/Notification/Notification.Infrastructure/"]
COPY ["src/Notification/Notification.Domain/Notification.Domain.csproj", "src/Notification/Notification.Domain/"]
COPY ["shared/SharedKernel/SharedKernel.csproj", "shared/SharedKernel/"]
RUN dotnet restore "src/Notification/Notification.Worker/Notification.Worker.csproj"
COPY . .
WORKDIR "/src/src/Notification/Notification.Worker"
RUN dotnet build "Notification.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notification.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.Worker.dll"] 