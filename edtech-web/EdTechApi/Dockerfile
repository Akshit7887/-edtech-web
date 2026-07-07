FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine3.24 AS build
WORKDIR /src
COPY . .
RUN if [ -f edtech-web/EdTechApi/EdTechApi.csproj ]; then \
      cd edtech-web/EdTechApi && dotnet restore && dotnet publish -c Release -o /app; \
    else \
      dotnet restore && dotnet publish -c Release -o /app; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.24 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "EdTechApi.dll"]