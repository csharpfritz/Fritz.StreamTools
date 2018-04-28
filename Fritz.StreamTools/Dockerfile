FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
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
RUN dotnet publish -c Release -o /app -r linux-x64 --self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Fritz.StreamTools.dll"]
