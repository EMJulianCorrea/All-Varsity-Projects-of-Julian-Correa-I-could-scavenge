FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet publish -c Release -o /home/site/wwwroot


FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final

WORKDIR /home/site/wwwroot

COPY --from=build /home/site/wwwroot .

ENV AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:10000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;TableEndpoint=http://host.docker.internal:10002/devstoreaccount1;"

ENV MenuStorageConnection="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://host.docker.internal:10000/devstoreaccount1;QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;TableEndpoint=http://host.docker.internal:10002/devstoreaccount1;"
ENV QueueStorageConnection="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;"

#docker images have been built (Jordan) 
#docker images have been checked (Jordan) 

#Run CoffeeNChill container done (Jordan)

#Done Connecttion/test with Azurite (Jordan)


#Image has been pushed to Docker Hub (Jordan)

#Built and tag the v2.0 image

#Verify Docker Compose end-to-end has been done 