FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM base AS python
RUN apt-get update -y
RUN apt-get install -y python3 python3-pip
RUN pip3 install py-air-control

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["PyAirControlDashboard/PyAirControlDashboard.csproj", "PyAirControlDashboard/"]
RUN dotnet restore "PyAirControlDashboard/PyAirControlDashboard.csproj"
COPY . .
WORKDIR "/src/PyAirControlDashboard"
RUN dotnet build "PyAirControlDashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PyAirControlDashboard.csproj" -c Release -o /app/publish

FROM python AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PyAirControlDashboard.dll"]
