# How to Full Text Search Audio and Video Files

This sample shows how to perform full text search over the spoken words within audio and video files.  This sample will leverage Azure Media Services to transcribe the text from the audio and video files and Azure Search to enable the full text search over this text.  

<img src="https://raw.githubusercontent.com/liamca/azure-search-audio-video-search/master/fulltext_search_audio_video.png">

## The Demo

For this sample we will use the Channel9 videos that were created at the [2015 Build Conference](https://channel9.msdn.com/events/build/2015).  This sample is split into 3 sections:
- Transcribing the Videos
- Importing the Tanscribed Text to Azure Search
- Perform Full Text Search

## Transcribing Videos

I will not go into much detail on how to do the transcription since this [blog post](https://azure.microsoft.com/en-us/blog/introducing-azure-media-indexer/) already did a really good job and provides code on how to do this along with a [source code sample](http://aka.ms/indexersample).


The output of this process will be a set TTML files which are in XML format and provide all of the spoken words split up by timeframe.  Here is an example of content from the Day One Keynote.

``<p begin="00:00:21.289" end="00:00:24.769">It's exciting to be here at...san francisco.</p>``<br>
``<p begin="00:00:26.129" end="00:00:30.159">In fact I was just recounting I've been and,</p>``<br>
``<p begin="00:00:30.159" end="00:00:35.439">every...conference of ours since nineteen ninety one in fact the very first one as</p>``<br>
``<p begin="00:00:35.439" end="00:00:39.869">a...developer and after that as an employee at microsoft</p>``<br>

There are 172 videos so rather than forcing you to run transcription for all of these files, I have included the transcribed files in this repository under the <a href="https://github.com/liamca/azure-search-audio-video-search/tree/master/src/TTMLtoSearch/ttml">ttml directory</a>.  If however, you would like to download the audio files to try this yourself, I have included a PowerShell script in this repository called <a href="https://github.com/liamca/azure-search-audio-video-search/blob/master/download_build_sessions_mp3.ps1">download_build_sessions_mp3.ps1</a>.

## Creating the Azure Search Index and Uploading Metadata and Transcribed Text

At this point, we now have all the videos text transcriptions in TTML files and are ready to upload them to an Azure Search index.  If you have not yet created an Azure Services, you can do so for free using the following <a href="https://azure.microsoft.com/en-us/pricing/free-trial/">Azure Free Trial</a>.  After you have created your Azure Service, you will need to update the sample <a href="https://github.com/liamca/azure-search-audio-video-search/tree/master/src">TTMLtoSearch.sln</a> with your Azure Search service name and API Key.  Both of these can be found in the <a href="https://portal.azure.com">Azure Portal</a> by choosing your Azure Search service.  To get the API Key, you can find thisby choosing Keys.

When you launch this application, it will do the following steps:

### Create an Index Called BuildSessions

The Azure Search index is the container for the data to be searched.  Here is the schema of this index:

<pre>
<code>
var definition = new Index()
{
    Name = AzureSearchIndex,
    Fields = new[] 
    { 
        new Field("session_id",     DataType.String)         { IsKey = true,  IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
        new Field("session_title",  DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
        new Field("tags",           DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
        new Field("speakers",       DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
        new Field("date",           DataType.String)         { IsKey = false, IsSearchable = true,  IsFilterable = true,  IsSortable = true,  IsFacetable = false, IsRetrievable = true},
        new Field("url",            DataType.String)         { IsKey = false, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false, IsRetrievable = true},
        new Field("transcribed_text",DataType.String)        { IsKey = false, IsSearchable = true,  IsFilterable = false,  IsSortable = false,  IsFacetable = false, IsRetrievable = true, Analyzer = AnalyzerName.EnMicrosoft},
    }
};
</code>
</pre>

### Upload Session Metadata to Azure Search Index

The next step is to upload metadata for each of the sessions.  This includes things like the session title, speaker names, and url reference to the videos for the sessions and can be found in the file <a href="https://github.com/liamca/azure-search-audio-video-search/blob/master/src/TTMLtoSearch/BuildSessionMetatdata.csv">BuildSessionMetaData.csv</a>.  

After this content is loaded into your Azure Search index a test search for a session with the text 'Azure Search' is executed.  From the image below you can see that there is one relevant result.

<img src="https://raw.githubusercontent.com/liamca/azure-search-audio-video-search/master/search_metadata_only.png">

### Upload Transcribed Text to Azure Search Index

The next step of the application will merge in the transcribed text from each of the videos into a searchable field called transcribed_text.  

Once this completes, the same search with the text 'Azure Search' is executed.  From the image below you can see that now there are multiple sessions that are returned.  This is because there were many sessions where the term "Azure Search" is found thanks to the extra content from the transcribed text.

<img src="https://raw.githubusercontent.com/liamca/azure-search-audio-video-search/master/search_metadata_and_transcribed_text.png">

One thing you might notice about this sample is that the entire transcribed text for a video is uploaded to a sigle documents within Azure Search.  This is convenient because it allows you to find sessions that relates closely to the searched terms.  The downside to this method is that I am not making use of the time included with each snippet of text which would allow me to send users directly to the point in time this text was spoken.

``<p begin="00:00:21.289" end="00:00:24.769">It's exciting to be here at...san francisco.</p>``<br>

In order to do this, you might want to consider creating two indexes.  One index for the session metadata and another index that contains all of the text snippets along with the timeframe that text was spoken.

## How to Make This Even Better

One of the things that I did not cover here is the ability of Azure Search to support Phonetic Search.  This is done by creating a field in the Azure Search index that uses a custom analyzer.  To learn more about how to create a field of this type, please visit the following <a href="https://azure.microsoft.com/en-us/blog/custom-analyzers-in-azure-search/">Azure Blog post</a>.
