# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Bulky.sln .
COPY BulkyWeb/BulkyBookWeb.csproj BulkyWeb/
COPY Bulky.DataAccess/BulkyBook.DataAccess.csproj Bulky.DataAccess/
COPY Bulky.Models/BulkyBook.Models.csproj Bulky.Models/
COPY Bulky.Utility/BulkyBook.Utility.csproj Bulky.Utility/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/BulkyWeb
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE $PORT

ENTRYPOINT ["dotnet", "BulkyBookWeb.dll"]
