FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["SW.Serverless.SampleWeb/SW.Serverless.SampleWeb.csproj", "SW.Serverless.SampleWeb/"]
COPY ["SW.Serverless/SW.Serverless.csproj", "SW.Serverless/"]

COPY ["nuget.config", "nuget/"]


RUN dotnet restore "SW.Serverless.SampleWeb/SW.Serverless.SampleWeb.csproj" --configfile nuget/nuget.config
COPY . .
WORKDIR "/src/SW.Serverless.SampleWeb"
RUN dotnet build "SW.Serverless.SampleWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SW.Serverless.SampleWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .




ENTRYPOINT ["dotnet", "SW.Serverless.SampleWeb.dll"]

#https://www.bradjolicoeur.com/Article/azure-devops-private-nuget-feed-with-docker-build