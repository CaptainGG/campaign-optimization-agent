FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CampaignAgent.Api/CampaignAgent.Api.csproj", "CampaignAgent.Api/"]
COPY ["CampaignAgent.Core/CampaignAgent.Core.csproj", "CampaignAgent.Core/"]
RUN dotnet restore "CampaignAgent.Api/CampaignAgent.Api.csproj"
COPY . .
RUN dotnet build "CampaignAgent.Api/CampaignAgent.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CampaignAgent.Api/CampaignAgent.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CampaignAgent.Api.dll"]
