# Image de l'API TicketRush. Les étudiants n'ont pas besoin du SDK .NET pour la lancer.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/TicketRush.Api -c Release -o /app/api \
 && dotnet publish tools/TicketRush.FakePayment -c Release -o /app/payment

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/api .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketRush.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS payment
WORKDIR /app
COPY --from=build /app/payment .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketRush.FakePayment.dll"]
