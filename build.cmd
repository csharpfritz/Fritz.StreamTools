docker build -t fritz.streamtools:%1 -t fritz.streamtools:latest -f Fritz.StreamTools\Dockerfile .

docker tag fritz.streamtools:%1 fritzregistry.azurecr.io/fritz.streamtools:%1
docker tag fritz.streamtools:%1 fritzregistry.azurecr.io/fritz.streamtools:latest

docker push fritzregistry.azurecr.io/fritz.streamtools
