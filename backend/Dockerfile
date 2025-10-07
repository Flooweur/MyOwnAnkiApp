FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY FlashcardApi.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "FlashcardApi.dll"]