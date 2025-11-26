# =======================================
# STAGE #1: Make a release
# =======================================

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS release

WORKDIR /converter

COPY ./Shared ./Shared

COPY ./ConverterContainer/ConverterContainer.csproj ./ConverterContainer/Program.cs ./ConverterContainer/

RUN dotnet publish ./ConverterContainer/ConverterContainer.csproj

# =======================================
# STAGE #2: Download Chrome Headless Shell
# =======================================

FROM node:bookworm-slim AS chrome

WORKDIR /chrome

RUN npx @puppeteer/browsers install chrome-headless-shell@132 --path ./

# =======================================
# STAGE #3: Final
# =======================================

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS prod

WORKDIR /converter

COPY --from=release ./converter/ConverterContainer/bin/Release/net8.0/publish/ ./
COPY --from=chrome ./chrome/chrome-headless-shell/linux*/chrome-headless-shell-linux* ./ChromeHeadlessShell

RUN apt-get update \
    && apt-get install -y wget gnupg \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' \
    && apt-get update \
    && apt-get install -y google-chrome-stable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf libxss1 \
      --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENTRYPOINT [ "dotnet", "ConverterContainer.dll" ]