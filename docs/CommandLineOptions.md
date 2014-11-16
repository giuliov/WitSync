

# Command line options

Syntax is
```Batchfile
WitSync.exe -m <path_to_mapping_file>
```

 _Command_                     | _Description_
-------------------------------|--------------------------------
  -m, --configuration[=VALUE]  |  Configuration & Mapping file
  -g, --generate[=VALUE]       |  Generate sample configuration file (this option exclude all else)

 _Options_                 | _Description_
---------------------------|--------------------------------
  -e, --stopOnError        |  Stops if pipeline stage fails
  -t, --test               |  Test and does not save changes to target
  -l, --log[=VALUE]        |  Write complete log to file
  -v, --verbosity[=VALUE]  |  Verbosity level: Normal,Verbose,Diagnostic

 _Data_                    | _Description_
---------------------------|--------------------------------
  -i, --index[=VALUE]      |  Index file, e.g. MyIndex.xml
  -c, --changeLog[=VALUE]  |  ChangeLog file, e.g. ChangeLog.csv

 _Connection_                           | _Description_
----------------------------------------|--------------------------------
 --sc, --sourceCollection[=VALUE]       | Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection
 --dc, --destinationCollection[=VALUE]  | Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection
 --sp, --sourceProject[=VALUE]          | Source Project Name
 --dp, --destinationProject[=VALUE]     | Destination Project Name
 --su, --sourceUser[=VALUE]             | Username connecting to Source
 --du, --destinationUser[=VALUE]        | Username connecting to Destination
 --sw, --sourcePassword[=VALUE]         | Password for Source user
 --dw, --destinationPassword[=VALUE]    | Password for Destination user

Connecting to a VSO (Visual Studio Online) project requires that the VSO Project uses alternate credentials.
