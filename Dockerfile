FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EventTicketSystem.Web/EventTicketSystem.Web.csproj", "src/EventTicketSystem.Web/"]
RUN dotnet restore "src/EventTicketSystem.Web/EventTicketSystem.Web.csproj"
COPY . .
RUN dotnet publish "src/EventTicketSystem.Web/EventTicketSystem.Web.csproj" \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventTicketSystem.Web.dll"]
