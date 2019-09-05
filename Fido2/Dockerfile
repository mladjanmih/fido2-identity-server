FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Fido2/Fido2.csproj", "Fido2/"]
RUN dotnet restore "Fido2/Fido2.csproj"
COPY . .
WORKDIR "/src/Fido2"
RUN dotnet build "Fido2.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Fido2.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Fido2.dll"]