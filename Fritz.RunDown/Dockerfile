FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY *.sln ./
COPY Fritz.RunDown/Fritz.RunDown.csproj Fritz.RunDown/
RUN dotnet restore
COPY . .
WORKDIR /src/Fritz.RunDown
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app -r linux-x64 --self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Fritz.RunDown.dll"]
