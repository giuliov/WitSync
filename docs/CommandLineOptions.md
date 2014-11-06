# Command line options

Syntax is
```Batchfile
WitSync.exe -m <path_to_mapping_file>
```

_Options_                                | _Description_
-----------------------------------------|--------------------------------
   --cl, --changes, --changeLogFile      |  ChangeLog file, e.g. ChangeLog.csv
   -e, --stopOnError                     |  Stops if pipeline stage fails
   --Help                                |  Displays this help text
   -i, --index, --indexFile              |  Index file, e.g. MyIndex.xml
   -l, --log, --logFile                  |  Write complete log to file (always appends)
   -m, --map, --mapping, --mappingFile   |  Mapping file, e.g. MyMappingFile.yml
   -t, --test, --trial                   |  Test and does not save changes to target
   -v, --verbose                         |  Prints detailed output

_Stages_                                 | _Description_
-----------------------------------------|--------------------------------
   --areas, --area                       |  Syncronize Areas (see documentation for limits)
   --globallists, --globallist           |  Syncronize GlobalLists data (use mapping file to filter)
   --iterations, --iteration             |  Syncronize Iterations (see documentation for limits)
   --workitems, --wi, --workitem         |  Syncronize WorkItems

_Connection_                                       | _Description_
---------------------------------------------------|--------------------------------
   -c, --sourceCollection                          |  Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection
   -d, --destinationCollection, --targetCollection |  Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection
   --dp, --destinationPassword, --targetPassword   |  Password for Destination user
   --du, --destinationUser, --targetUser           |  Username connecting to Destination
   -p, --sourceProject                             |  Source Project Name
   -q, --destinationProject, --targetProject       |  Destination Project Name
   --sp, --sourcePassword                          |  Password for Source user
   --su, --sourceUser                              |  Username connecting to Source

_Options for WorkItems stage_            | _Description_
-----------------------------------------|--------------------------------
   --BypassWorkItemValidation            |  Disable Rule validation
   --CreateThenUpdate                    |  WorkItems missing from the target are first added in the initial state specified
                                         |  by InitalStateOnDestination, then updated to reflect the state of the source.
   --DoNotOpenTargetWorkItem             |  Use [WorkItem.Open](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.open.aspx) Method to make the WorkItem updatable.
   --PartialOpenTargetWorkItem           |  Use [WorkItem.PartialOpen] Method to make the WorkItem updatable.
   --UseHeuristicForFieldUpdatability    |  Algorithm used to determine when a field is updatable. By default the tool checks the [Field.IsEditable](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.field.iseditable.aspx) Property.


`test` option does not save any change to target.

Connecting to a VSO (Visual Studio Online) project requires using alternate credentials.
