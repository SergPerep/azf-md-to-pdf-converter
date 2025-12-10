# Container guide

## Build image

Run from the root folder, otherwise the "Shared" folder will not be copied.

```bash
docker build -f ConverterContainer/DockerFile -t md2pdf-container:latest .
```

## Run container

Create `.env` and put env variables there. Run:

```bash 
docker run --env-file ./ConverterContainer/.env md2pdf-container:latest
```

## Publish container to GitHub Container Registry

```bash 
docker tag md2pdf-container <registry>/md2pdf
docker push <registry>/md2pdf
```

## Install chrome headless sheel

```bash
npx @puppeteer/browsers install chrome-headless-shell@132 --path ./
mv ./chrome-headless-shell/linux*/chrome-headless-shell-linux* ./ChromeHeadlessShell
rm -r ./chrome-headless-shell
```