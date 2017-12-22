docker build -t fritz.rundown:%1 -t fritz.rundown:latest -f Fritz.RunDown\Dockerfile .

docker tag fritz.rundown:%1 fritzregistry.azurecr.io/fritz.rundown:%1
docker tag fritz.rundown:%1 fritzregistry.azurecr.io/fritz.rundown:latest

docker push fritzregistry.azurecr.io/fritz.rundown
