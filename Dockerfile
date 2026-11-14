# Pull source image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and get dependencies
COPY ["Aeoquotes.csproj", "./"]
RUN dotnet restore "Aeoquotes.csproj"

# Copy source and build
COPY . .
RUN dotnet publish "Aeoquotes.csproj" -c Release -o /app/publish /p:UseAppHost=false

# now the runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Make db dir
RUN mkdir -p /app/Database

# copy release build into here
COPY --from=build /app/publish .

ENTRYPOINT [ "dotnet", "Aeoquotes.dll"]
