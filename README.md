# veracode-project-status
Utility to generate high level html report of all your VeraCode projects

## Directories
### source
C# source is present here.

### dependencies
The C# wrapper for VeraCode API provided by VeraCode is present here.
The VeracodeC#API.exe assembly can be downloaded from VeraCode: 
https://docs.veracode.com/r/t_about_Csharp_wrapper.

### resources
Resources like html files, images file used by the utility are present here.

### How to build
You need:
- Visual Studio with .NET Desktop development feature
  

Just build "source\verastat.sln" using Visual Studio or MSBUILD.

### How to run
First of all you need to have your VeraCode id and secret key. VeraCode advises
not to embed these in your application, rather you should store these in a 
credentials file. The credentials file works a lot like Amazon Web Services' 
credentials file where you can have mutliple profiles. This utility assumes you
have the VeraCode credentials file correctly configured.	
To learn more about configuring credentials, checkout the [official docs](https://docs.veracode.com/r/orRWez4I0tnZNaA_i0zn9g/1jVX_qsPK0IAL08ynVQOPQ)</a>.

You need to have .NET Framework 4.5 or above installed on your Windows
machine.
```
verastat -p <VeraCodeProfile> -r <ReportFilename> -o <FolderPath> -t <outputType>
```
- -p: Specifies the profile in the VeraCode credentials file to use to authenticate
- -r: Specifies the name of the report file, the filename will be appended with the current date
- -o: Generates html file with status results in the folder specified
- -t: Specifies the output type, either csv (default) or html

**Note:** You should run the utility from the same folder where it is located. The resources 
folder must be present in the same folder where the verastat utility is present. 
This folder is copied to the output folder when the  
project is built. 
