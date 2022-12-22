#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 82
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WalliDO.Service/WalliDO.Service.csproj", "WalliDO.Service/"]
RUN dotnet restore "WalliDO.Service/WalliDO.Service.csproj"
COPY . .
WORKDIR "/src/WalliDO.Service"
RUN dotnet build "WalliDO.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WalliDO.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WalliDO.Service.dll"]