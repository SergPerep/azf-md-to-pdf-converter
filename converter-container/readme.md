# Build image

```bash
docker build -t md2pdf-container:latest .
```

# Run container locally

Create `.env` and put env variables there: 

```bash 
docker run --env-file .env md2pdf-container:latest
```

# Publish container to registry

```bash 
docker tag md2pdf-container <registry>/md2pdf-container:latest

docker push <registry>/md2pdf-container:latest
```

## Install chrome headless sheel

```bash
npx @puppeteer/browsers install chrome-headless-shell@132 --path ./
mv ./chrome-headless-shell/linux*/chrome-headless-shell-linux* ./ChromeHeadlessShell
rm -r ./chrome-headless-shell
```

## Add env variables

You can do it via User Secrets
Add them to `.env` file too, if you want to run container locally