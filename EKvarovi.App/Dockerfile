FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["EKvarovi.App/EKvarovi.App.csproj", "EKvarovi.App/"]
COPY ["EKvarovi.Shared/EKvarovi.Shared.csproj", "EKvarovi.Shared/"]
RUN dotnet restore "EKvarovi.App/EKvarovi.App.csproj"

COPY . .
WORKDIR /src/EKvarovi.App
RUN dotnet publish -c Release -o /app/publish
RUN cp wwwroot/appsettings.docker.json /app/publish/wwwroot/appsettings.json

FROM nginx:alpine AS final
COPY EKvarovi.App/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
