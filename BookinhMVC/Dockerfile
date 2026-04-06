FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BookinhMVC.csproj", "./"]
RUN dotnet restore "./BookinhMVC.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "BookinhMVC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookinhMVC.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookinhMVC.dll"]
