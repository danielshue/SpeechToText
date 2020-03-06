# Speech (Audio) to Text sample using Azure Function

This code sample provides an example of how one might convert an audio file to text and then provide a transcript, key phrases, and overall sentiment analytics of the content. This example has been tested on audio files up to 5 minutes. It may work longer on longer audio, but need to be aware that Azure Functions may timeout.

In order for this service to work, the following services need to be created in Azure.

1. SQL Server Database (Provisioned with SQL Server. Ensure the firewall is set to allow Azure and your own IP address to allow conections.)
2. Azure Function
3. App Service Plan (Provisioned with Azure Function)
4. Application Insights (Provisioned with Azure Function)
5. Blob Storage Account (Provisioned with Azure Function)
6. Cognitive Services Speech API
7. Cognitive Services Text Analytics

Once the services have been set up, then each of the settings below need to be added to the Azure Function Application Settings & Connection String.

##  Azure Function Application & Connection String Settings

Each of the details needed below can be found or be navigated to from the Azure Portal Overview page for each of the resources.

* AudioBlobConnectionString
This is the connection string which contains the information that the Azure Function uses to listen to the incoming BLOB/Audio file.
It starts with 'DefaultEndpointsProtocol=https;AccountName=...'

* SpeechApiKeyCredential
This is the hash that's passed into the Speech API.

* SpeechApiRegion
This represents the region where the Speech API is located such as 'westus2'.

* TextAnalyticsApiKeyCredential
This is the has that's passed into the Text Analytics Service

* TextAnalyticsEndpoint
This is the URI that's called for the Text Analytics Service such as "https://speech-analytics.cognitiveservices.azure.com/"

* TrancriptionSqlDatabaseConnectionString
The conection string used to storage the transcript details and starts with 'Server=tcp'

Once the settings have been set up, then the service is ready to run. You can use Azure Storage Explore to transfer files into a Container under the Blob stroage called "incoming". Next, connect to the database to review transcription.
