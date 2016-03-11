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
