FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["EKvarovi.Api/EKvarovi.Api.csproj", "EKvarovi.Api/"]
COPY ["EKvarovi.Shared/EKvarovi.Shared.csproj", "EKvarovi.Shared/"]
RUN dotnet restore "EKvarovi.Api/EKvarovi.Api.csproj"

COPY . .
WORKDIR /src/EKvarovi.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EKvarovi.Api.dll"]
