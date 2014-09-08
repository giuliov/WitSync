# Command line options

Syntax is
```Batchfile
WitSync.exe -a <action> -c <source_collection_url> -p <source_project_name> -d <destination_collection_url> -q <destination_project_name> [-m <path_to_mapping_file>] [-v] [-t] [_advanced_options_]
```

_Option_                                        | _Description_
------------------------------------------------|--------------------------------
  -a, -action                                   | Action, one of: SyncWorkItems,SyncAreasAndIterations.
  -c, -sourceCollection                         | Source Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection
  -d, -destinationCollection, -targetCollection | Destination Collection Url, e.g. http://localhost:8080/tfs/DefaultCollection
  -dp, -destinationPassword, -targetPassword    | Password for Destination user
  -du, -destinationUser, -targetUser            | Username connecting to Destination
  -m, -map, -mapping, -mappingFile              | Mapping file, e.g. MyMappingFile.xml
  -i, -index, -indexFile                        | Index file, e.g. MyIndex.xml
  -p, -sourceProject                            | Source Project Name
  -q, -destinationProject, -targetProject       | Destination Project Name
  -sp, -sourcePassword                          | Password for Source user
  -su, -sourceUser                              | Username connecting to Source
  -t, -test, -trial                             | Test and does not save changes to target
  -v, -verbose                                  | Prints detailed output
  -Help                                         | Displays help text

`TestOnly` option tries action but does not save any change to target.

Connecting to a VSO (Visual Studio Online) project requires the alternate credentials.


## Actions

The supported values for action are `SyncWorkItems` and `SyncAreasAndIterations`.

`SyncWorkItems` syncronizes work items.
`SyncAreasAndIterations` replicates source project's Areas and Iterations, mapping file is not used.


## Advanced options (SyncWorkItems only)

These options permit fine control on `SyncWorkItems` behavior.

_Option_                               | _Description_
---------------------------------------|--------------------------------
   -DoNotOpenTargetWorkItem            |   Do not call [WorkItem.Open](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.open.aspx) Method to make the WorkItem updatable. This 
   -PartialOpenTargetWorkItem          |   Use [WorkItem.PartialOpen] Method to make the WorkItem updatable.
   -UseHeuristicForFieldUpdatability   |   Algorithm used to determine when a field is updatable. By default the tool checks the [Field.IsEditable](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.field.iseditable.aspx) Property.
   -BypassWorkItemValidation           |   Disable Rule validation
   -CreateThenUpdate                   |   WorkItems missing from the target are first added in the initial state specified by `InitalStateOnDestination`, then updated to reflect the state of the source.
