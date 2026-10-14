FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["BookLibrary.sln", "./"]
COPY ["src/BookLibrary.Domain/BookLibrary.Domain.csproj", "src/BookLibrary.Domain/"]
COPY ["src/BookLibrary.Application/BookLibrary.Application.csproj", "src/BookLibrary.Application/"]
COPY ["src/BookLibrary.Infrastructure/BookLibrary.Infrastructure.csproj", "src/BookLibrary.Infrastructure/"]
COPY ["src/BookLibrary.Api/BookLibrary.Api.csproj", "src/BookLibrary.Api/"]
COPY ["tests/BookLibrary.Tests.Unit/BookLibrary.Tests.Unit.csproj", "tests/BookLibrary.Tests.Unit/"]
COPY ["tests/BookLibrary.Tests.Integration/BookLibrary.Tests.Integration.csproj", "tests/BookLibrary.Tests.Integration/"]

RUN dotnet restore "BookLibrary.sln"

COPY . .
WORKDIR "/src/src/BookLibrary.Api"
RUN dotnet publish "BookLibrary.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookLibrary.Api.dll"]
