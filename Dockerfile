# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["AI-Agent-Document_Processing.csproj", "./"]
RUN dotnet restore "AI-Agent-Document_Processing.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "AI-Agent-Document_Processing.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "AI-Agent-Document_Processing.csproj" -c Release -o /app/publish

# Final stage - runtime image
FROM base AS final
WORKDIR /app

# Install required system dependencies for PDF processing
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Create directories for documents and output
RUN mkdir -p SampleTexts Analysis Logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AI-Agent-Document_Processing.dll"]