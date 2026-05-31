FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /build
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef

COPY ./ThuTasty.Web.csproj .
RUN --mount=type=cache,id=thutasty-nuget,target=/root/.nuget/packages \
    dotnet restore

COPY . .
RUN --mount=type=cache,id=thutasty-nuget,target=/root/.nuget/packages \
    dotnet build -o out

RUN --mount=type=cache,id=thutasty-nuget,target=/root/.nuget/packages \
    dotnet ef migrations bundle \
        --output /build/out/efbundle \
        --self-contained \
        --verbose \
        --no-build \
        --configuration Release

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /build/out .
COPY --from=build /build/wwwroot ./wwwroot
COPY ./docker-entrypoint.sh .

CMD ["bash", "/app/docker-entrypoint.sh"]
