FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY src/TotpDemo/TotpDemo.csproj TotpDemo/
RUN dotnet restore TotpDemo/TotpDemo.csproj
COPY src/TotpDemo/ TotpDemo/
RUN dotnet publish TotpDemo/TotpDemo.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TotpDemo.dll"]
