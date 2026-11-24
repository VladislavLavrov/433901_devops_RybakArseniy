FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5016
ENV ASPNETCORE_URLS=http://+:5016

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Calculator16.csproj .
RUN dotnet restore "Calculator16.csproj"

COPY . .
RUN dotnet publish "Calculator16.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Calculator16.dll"]
