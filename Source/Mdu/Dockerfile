FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Mdu/Mdu.csproj", "Mdu/"]
RUN dotnet restore "Mdu/Mdu.csproj"
COPY . .
WORKDIR "/src/Mdu"
RUN dotnet build "Mdu.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Mdu.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mdu.dll"]
