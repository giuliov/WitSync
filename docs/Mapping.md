# Configuration (mapping) file Reference

WitSync is controlled via a configuration or mapping file. The mapping file use the [YAML](http://www.yaml.org/) format to configure the WitSync pipeline. You can configure just a small subset of information, as WitSync assumes you want an hi-fidelity copy of the source.
This page describe the configuration in sections:
 * [Pipeline](#pipeline-configuration)
 * [Areas & Iterations](#areas-and-iterations)
 * [Global lists](#global-lists)
 * [Work Items](#work-items)

At the end, there are some sample configurations.



## Pipeline Configuration

```YAML
sourceConnection:
  collectionUrl: url_to_source_collection
  projectName: source_team_project_name
  user: user_connecting_to_source_specify_the_domain_when_required
  password: user_password
destinationConnection:
  collectionUrl: url_to_target_collection
  projectName: destination_team_project_name
  user: user_connecting_to_target_specify_the_domain_when_required
  password: user_password
pipelineStages:
  - globallists
  - areas
  - iterations
  - workitems
changeLogFile: relative_or_absolute_path_to_changelog_file
logFile: relative_or_absolute_path_to_log_file
logging: Normal|Verbose|Diagnostic
stopPipelineOnFirstError: boolean
testOnly: boolean
```

These parameters can also be set on the command line, the latter wins.

### Test (trial) mode
This is the single most important configuration value. When `testOnly` has value `true`, no data is written on the target TFS Project.

```YAML
testOnly: boolean
```

### Connections
This section configures the connection to source and target TFS.

```YAML
sourceConnection:
  collectionUrl: url_to_source_collection
  projectName: source_team_project_name
  user: user_connecting_to_source_specify_the_domain_when_required
  password: user_password
destinationConnection:
  collectionUrl: url_to_target_collection
  projectName: destination_team_project_name
  user: user_connecting_to_target_specify_the_domain_when_required
  password: user_password
```

The users are optional: if not specified, WitSync will use the running user or what is configured in Windows Credential Store.

### Logging
This section controls the logging level and the log file.

```YAML
logFile: relative_or_absolute_path_to_log_file
logging: Normal|Verbose|Diagnostic
```
Note that WitSync always appends to the log file.

Level can be:

 - Normal
 - Verbose
 - Diagnostic

### ChangeLog
This file is overwritten at each run. It records the list of changed objects in [CSV](http://en.wikipedia.org/wiki/Comma-separated_values) format.
The columns are:

| Column     | Description                                           |
|------------|-------------------------------------------------------|
| Source     | Stage that made the change                            |
| SourceId   | ID of source object                                   |
| TargetId   | ID of destination object                              |
| ChangeType | Type of change (e.g. Add, Update, Delete or Failure)  |
| Message    | Error message in case of failure                      |

The ChangeLog file is useful to create reports or make a complex workflow composed by multiple scripts and tools.
Only successful saves write records in the Change Log.  

### Pipeline

This section defines the synchronization pipeline.

```YAML
pipelineStages:
  - globallists
  - areas
  - iterations
  - workitems
```
The tool arranges in a pre-defined order even if you use a different one.

| Stage       | Description
|-------------|--------------------------------
| globallists |  Syncronize GlobalLists data
| areas       |  Syncronize Areas (see documentation for limits)
| iterations  |  Syncronize Iterations (see documentation for limits)
| workitems   |  Syncronize WorkItems

The `stopPipelineOnFirstError` controls if the pipeline will continue after an error.



## Areas and Iterations

```YAML
areasAndIterationsStage: {}
```

There are no options to configure.



## Global lists

```YAML
globalListsStage:
  include:
  - global_list_to_include
  exclude:
  - global_list_to_exclude
```

You can use the `include` clause, the `exclude` clause or both.

### Include

In this case only listed GlobalLists are copied to the destination.

### Exclude

All GlobalLists are copied to the destination, except for those listed in the `exclude` clause.

### Both
Should not be used. WitSync will copy only the GlobalLists listed in `include` clause and not present in the `exclude` list. 



## Work Items

### Index file

WitSync uses the work item [ID field](#id-field-optional) (`System.Id`) to uniquely identify work items to syncronize. There are two ways to relate work items on the destination  project with the source:
    - using a field to holds the source ID, useful when the ID is meaningful to users (like a bug number)
    - using an external file to track mapping, useful when replicating a project on a different TFS collection or instance

When you specify an _Index_ file

```YAML
workItemsStage:
  indexFile: relative_or_absolute_path_to_index_file
```
you get the maximum freedom in mapping work item schemas. WitSync looks up the Index file to know if a work item has a correspondent on the destination project; if not found, a new work item is created. On a match, the existing work item on the target project is updated.
You can specify the file on the command line, and this takes precedence and the element in the mapping file is optional.

Make sure to properly backup this file, otherwise the tool will re-create new workitems instead of updating the existing  workitems.

### Source query

```YAML
workItemsStage:
  sourceQuery: source query
```

The tool should present itself as an account able to get the result from the source query. The source query can be expressed in [WIQL](http://msdn.microsoft.com/en-us/library/bb130306.aspx) or be the name of an existing query.

If you want to replicate all workitems, you can use a generic query like this

```XML
<WorkItemQuery Version="1">
  <TeamFoundationServer>http://localhost:8080/tfs/DefaultCollection</TeamFoundationServer>
  <TeamProject>SourceProject</TeamProject>
  <Wiql>
    SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags]
    FROM WorkItems
    WHERE [System.TeamProject] = @project
    and [System.WorkItemType] <> ''
    and [System.State] <> ''
  </Wiql>
</WorkItemQuery>
```

this query extracts all work items from SourceProject.
This is also the default if you do not specify the source query: all kind of Work Items are read from the source and compared to destination.

**Note**: query filters only which Work Items are synced, but does not obey the query in filtering the columns. Is the mapping files that determines which fields are copied and how.

### Target query

On the target, the tool must be able to run `Shared Queries\MyDestinationQuery` and to write (Contribute access) in the project. On the first run, the target query must give an empty result; on subsequent runs, it must returns previously synced workitems.

```YAML
workItemsStage:
  destinationQuery: source query
```

This way, tool’s work is optimized by not scanning all workitems in the target project.

If you need to replicate all workitems, use a generic query like this

```XML
<WorkItemQuery Version="1">
  <TeamFoundationServer>http://localhost:8080/tfs/DefaultCollection</TeamFoundationServer>
  <TeamProject>DestProject</TeamProject>
  <Wiql>
    SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags]
    FROM WorkItems
    WHERE [System.TeamProject] = @project
    and [System.WorkItemType] <> ''
    and [System.State] <> ''
  </Wiql>
</WorkItemQuery>
```

this query selects all work items from DestProject.
If you do not specify a destination query, by default all kind of Work Items are read from the destination and compared to source.


### Area and Iteration mapping

You can specify one or more rule to map Area/Iteration paths.

The simplest rule is **specific**, an Area/Iteration path is mapped exactly to a literal path on target.
```YAML
workItemsStage:
  areaMap:
  - sourcePath: Src\Area 1
    destinationPath: Dest\Area 2
```
You can use the `SyncAreasAndIterations` option to replicate the paths on the destination.

Another option is to use the **source wildcard** rule: all source paths are mapped to a specific target path.
```YAML
workItemsStage:
  iterationMap:
  - sourcePath: '*'
    destinationPath: Dest\Sprint 3
```
Useful in partial-replica scenarios, where you are transferring specific data subsets.

**Note**: YAML requires the quotes around the asterisk symbol.

If the destination is empty, it is mapped to the **root** node, so
```YAML
workItemsStage:
  iterationMap:
  - sourcePath: '*'
    destinationPath: ''
```
is equivalent to
```YAML
workItemsStage:
  iterationMap:
  - sourcePath: '*'
    destinationPath: Dest
```
This is the default if you do not specify any Area / Iteration mapping.

The last option uses the **destination wildcard** rule: source paths are mapped to equivalent target paths, that is identical except for the root node.
```YAML
workItemsStage:
  areaMap:
  - sourcePath: '*'
    destinationPath: '*'
```

As you see, the rules have the same syntax and meaning for both Areas and Iterations.


### Work Item Type mapping

A mapping file usually contains some work item mappings.

The syntax is
```YAML
workItemsStage:
  workItemMappings:
  - sourceType: source_work_item_type
    destinationType: destination_work_item_type
    iDField: # optional
      source: System.ID
      destination: field_reference_name_on_destination
    stateList: # State table is optional
      states:
      - source: srcstate1
        destination: deststate1
      - source: srcstate2
        destination: deststate2
    fields:
      # list of Field mapping rules
    attachments: Sync
    defaultRules: true
    rollbackValidationErrors: true
```

Note that you can map different work item types, e.g. _Bugs_ to _Issues_, or _Product Backlog Items_ to _User Stories_.

To avoid syncing attachments, set Attachments to `DoNotSync`.

If you do not specify any Work Item mapping, Work Items maps to the same type on the destination, assuming the same set of States and Fields. This is done via the rules included by default.
If you want to exclude the default rules, set `defaultRules` to `false`.
By default, validation errors are cured by rolling back the culprit value to the original.
If you do not want this, set `rollbackValidationErrors` to `false`.


#### ID Field (_optional_)
WitSync uses the work item ID field (`System.Id`) to uniquely identify them. There are two ways to relate work items on the destination  project:
    - using a field to holds the source ID, useful when the ID is meaningful to users (like a bug number)
    - using an external file to track mapping, useful when replicating a project on a different TFS collection or instance

```YAML
workItemsStage:
  workItemMappings:
    iDField: # optional
      source: System.ID
      destination: field_reference_name_on_destination
```

The field in the target work item type that holds the source ID must be an integer. If not using the `iDField`, you must specify an _Index_ file and take care of it: make sure to properly backup it, otherwise the tool will re-create new workitems instead of updating the existing  workitems.


#### States
State mapping table is optional; when missing the target work item type must have at least the same states of the source.

```YAML
workItemsStage:
  workItemMappings:
    stateList: # State table is optional
      states:
      - source: Proposed
        destination: New
```

#### Field mapping

Field mapping rules specify which fields of the source work item type are copied over the target. Only the writable fields are updated.

Use reference names (e.g. `System.Title`), not field names (e.g. `Title`).

Field mapping rules have many forms, described below.

##### Explicit field mapping
Both source and destination specify a field.
```YAML
workItemsStage:
  workItemMappings:
    fields:
    - source: System.Description
      destination: System.Description
```
##### Explicit field mapping with Default
Both source and destination specify a field plus a default value to use when source value is empty.
```YAML
workItemsStage:
  workItemMappings:
    fields:
    - source: System.Description
      destination: System.Description
      setIfNull: Description missing
```
Null and empty string are considered both null.

##### Unmapped fields
The field is not mapped, useful for computed fields like "Area ID" whose value is derived from "Area Path".

Source has a name while destination is empty.
```YAML
workItemsStage:
  workItemMappings:
    fields:
    - source: System.AreaId
      destination: 
```
Normally this rule is not needed, as WitSync checks at run-time if a field is writable.
  
##### Fixed value
Destination field takes literal value.
```YAML
workItemsStage:
  workItemMappings:
    fields:
    - destination: Microsoft.VSTS.Common.Severity
      set: '3 - Medium'
```

can use if null also

```YAML
workItemsStage:
  workItemMappings:
    fields:
    - destination: Microsoft.VSTS.Common.Severity
      setIfNull: '3 - Medium'
```

##### Wildcard rule
Fields that exists on destination are copied. Must appear only once and last in the list.
```YAML
workItemsStage:
  workItemMappings:
    fields:
    - source: '*'
      destination: '*'
```
This is the default Rule if no Field rule is present.

##### Wildcard unmap rule
Any field with no explicit rule before is skipped.
Source is wildcard while destination is empty.
As the other wildcard rule, should appear only once and last in the list.

```YAML
workItemsStage:
  workItemMappings:
    fields:
    - source: '*'
      destination: 
```

##### Translation functions
Source values are converted via some built-in function.
```YAML
workItemsStage:
  workItemMappings:
    fields:
    - source: System.State
      destination: System.State
      translate: MapState
    - source: System.AreaPath
      destination: System.AreaPath
      translate: MapAreaPath
    - source: System.IterationPath
      destination: System.IterationPath
      translate: MapIterationPath
```
The only available functions are:

|Function         | Description                                                          |
|-----------------|----------------------------------------------------------------------|
|MapState         | Use `stateList` to do the mapping                                    |
|MapAreaPath      | Use `areaMap` to convert source Area Path values to target           |
|MapIterationPath | Use `iterationMap` to convert source Iteration Path values to target |


#### Attachments
You can specify the mode to sync Work Item Attachments.

```YAML
workItemsStage:
  workItemMappings:
    attachments: mode
```

Possible modes are:

| Sync Mode       | Description                                                                 |
|-----------------|-----------------------------------------------------------------------------|
|DoNotSync        | WitSync will not copy attachments                                           |
|AddAndUpdate     | Attachments missing on the destination or with different lengths are copied |
|RemoveIfAbsent   | Attachments present on target but missing in source are removed             |
|ClearTarget      | All destination attachments are removed before copying                      |
|Sync             | Combines AddAndUpdate & RemoveIfAbsent (default)                            |
|FullSync         | Combines AddAndUpdate & ClearTarget                                         |


#### Link Type mapping

A mapping file can specify Link Type mappings.

The syntax is
```YAML
workItemsStage:
  linkTypeMap:
  - sourceType: source_work_item_type
    destinationType: destination_work_item_type
  - sourceType: '*'
    destinationType: '*'
```

Note that the link type can be different, e.g. mapping `Child` to `Custom`.

If you do not specify any Link Type mapping, WitSync will use the type on the destination with identical name.

**Caveat**: External and Related links are not syncronized.

#### Advanced Options for WorkItems stage (Mode)

```YAML
workItemsStage:
  mode: comma_separated_list_of_options
```

| _Option_                   | _Description_                                                                                                                                                                                                                        |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| BypassWorkItemStoreRules   |  Disable Rule validation                                                                                                                                                                                                             |
| CreateThenUpdate           |  WorkItems missing from the target are first added in the initial state, then updated to reflect the state of the source.                                                                                                            |
| OpenTargetWorkItem         |  Use [WorkItem.Open](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.open.aspx) Method to make the WorkItem updatable.                                                             |
| PartialOpenTargetWorkItem  |  Use [WorkItem.PartialOpen](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.partialopen.aspx) Method to make the WorkItem updatable.                                               |
| UseEditableProperty        |  Algorithm used to determine when a field is updatable. By default the tool checks the [Field.IsEditable](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.field.iseditable.aspx) Property.  |



## Sample configurations

This is an example of a full blown mapping file.

```YAML
sourceConnection:
  collectionUrl: http://localhost:8080/tfs/DefaultCollection
  projectName: yourSourceProject
  user: sourceUser
  password: '***'
destinationConnection:
  collectionUrl: http://localhost:8080/tfs/DefaultCollection
  projectName: yourTargetProject
  user: targetUser
  password: '***'
pipelineStages:
  - globallists
  - workitems
changeLogFile: changes.csv
logFile: log.txt
logging: Diagnostic
stopPipelineOnFirstError: true
testOnly: true
areasAndIterationsStage: {}
globalListsStage:
  include:
  - incl1
  - incl2
  exclude:
  - excl3
  - excl4
workItemsStage:
  mode: UseEditableProperty, OpenTargetWorkItem, CreateThenUpdate
  sourceQuery: Shared Queries\MySourceQuery
  destinationQuery: Shared Queries\MyDestinationQuery
  indexFile: index.xml
  areaMap:
  - sourcePath: srcArea1
    destinationPath: dstArea1
  - sourcePath: srcArea2
    destinationPath: dstArea2
  iterationMap:
  - sourcePath: src
    destinationPath: dst
  - sourcePath: '*'
    destinationPath: 
  workItemMappings:
  - sourceType: srctype
    destinationType: desttype
    iDField:
      source: srcID
      destination: dstID
    stateList:
      states:
      - source: srcstate1
        destination: deststate1
      - source: srcstate2
        destination: deststate2
    fields:
    - source: src1
      destination: dst1
    - source: src2
      destination: dst2
      translate: tranFunc2
    - destination: dst3
      set: val3
    - source: src4
      destination: dst4
      setIfNull: set4
    - source: '*'
      destination: '*'
    - source: '*'
      destination: 
    attachments: Sync
    defaultRules: true
  linkTypeMap:
  - sourceType: srclnk1
    destinationType: dstlnk1
  - sourceType: srclnk2
    destinationType: dstlnk2
  - sourceType: '*'
    destinationType: '*'
```
