# Azure Event Hubs to Service Bus Mirror
Mirrors an Event Hub to Service Bus

## Getting Started
Update the value of appsettings.json with:

| Setting | Description |
| -- | -- |
| ConnectionStrings:EventHubSource | A connection string for an Event Hub namespace (without EntityPath) that will be the source of messages |
| ConnectionStrings:ConsumerGroupStorage | A connection string for a Storage account for use by the Event Hub Processor consumer group |
| ConnectionStrings:ServiceBusDestination | A connection string for a Service Bus (with EntityPath) that will be the destination of messages |
| EventHubName | The name of the source Event Hub in the above namespace |
| ConsumerGroupName | The name of the consumer group |
| StorageContainer | The name of the container in the above Storage account to be used by the processor consumer group |

Then just execute `dotnet run -p EventHubsMirror/EventHubsMirror.csproj`.

## What it does
This app simply receives messages from the source Event Hub and publishes them to a Service Bus.

## Why?!
I was working on a system where some applications used Event Hubs and others Service Bus, so I needed a way to get messages from A to B.
