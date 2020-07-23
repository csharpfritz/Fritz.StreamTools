FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.302-alpine3.12 AS build
WORKDIR /src
COPY global.json .
COPY Fritz.StreamTools/Fritz.StreamTools.csproj Fritz.StreamTools/
COPY Fritz.StreamLib.Core/Fritz.StreamLib.Core.csproj Fritz.StreamLib.Core/
COPY Fritz.Chatbot/Fritz.Chatbot.csproj Fritz.Chatbot/
COPY Fritz.Twitch/Fritz.Twitch.csproj Fritz.Twitch/
WORKDIR /src/Fritz.StreamTools
RUN dotnet restore
COPY Fritz.StreamTools/. .
COPY Fritz.StreamLib.Core/. ../Fritz.StreamLib.Core
COPY Fritz.Chatbot/. ../Fritz.Chatbot/.
COPY Fritz.Twitch/. ../Fritz.Twitch/.
RUN dotnet build -c Release -o /app --no-restore

FROM build AS publish
RUN dotnet publish -c Release -o /app 

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Fritz.StreamTools.dll"]
