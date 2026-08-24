FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["global.json", "."]
COPY ["backend/src/Plus5.Domain/Plus5.Domain.csproj", "backend/src/Plus5.Domain/"]
COPY ["backend/src/Plus5.Application/Plus5.Application.csproj", "backend/src/Plus5.Application/"]
COPY ["backend/src/Plus5.Infrastructure/Plus5.Infrastructure.csproj", "backend/src/Plus5.Infrastructure/"]
COPY ["backend/src/Plus5.Api/Plus5.Api.csproj", "backend/src/Plus5.Api/"]
RUN dotnet restore "backend/src/Plus5.Api/Plus5.Api.csproj"

COPY backend/src backend/src
RUN dotnet publish "backend/src/Plus5.Api/Plus5.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra@sha256:58ec3158d8581044c9ba2115994770118ffd1697fdc49391ad83bd81e7bcd4af AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Plus5.Api.dll"]
