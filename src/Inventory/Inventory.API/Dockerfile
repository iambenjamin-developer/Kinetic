FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Inventory/Inventory.API/Inventory.API.csproj", "src/Inventory/Inventory.API/"]
COPY ["src/Inventory/Inventory.Application/Inventory.Application.csproj", "src/Inventory/Inventory.Application/"]
COPY ["src/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "src/Inventory/Inventory.Infrastructure/"]
COPY ["src/Inventory/Inventory.Domain/Inventory.Domain.csproj", "src/Inventory/Inventory.Domain/"]
COPY ["shared/SharedKernel/SharedKernel.csproj", "shared/SharedKernel/"]
RUN dotnet restore "src/Inventory/Inventory.API/Inventory.API.csproj"
COPY . .
WORKDIR "/src/src/Inventory/Inventory.API"
RUN dotnet build "Inventory.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Inventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inventory.API.dll"] 