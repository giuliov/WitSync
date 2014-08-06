# WitSync Manual

WitSync is a simple command line tool that can copy TFS Work Item data from a source to a target project.

It is designed to be idempotent, that is, it can be run multiple times and getting the same result. The tool will pull the work items returned from a Work Item Query on the source TFS Project, compare to matching work items on the target TFS Project and update the latters.

The tool is controlled via a mapping file and the options specified on the command line.

A typical execution is
```
WitSync.exe -a SyncWorkItems -c http://localhost:8080/tfs/DefaultCollection -p SourceProject -d http://localhost:8080/tfs/DefaultCollection -q DestProject -m MyMappingFile.xml –v -t
```

## 1.1	Prerequisites

### 1.1.1	Source and Target

Create a Work Item Query on the source TFS Project and put path to the query in the mapping file. The columns returned are not important, as the mapping file specifies the fields to sync.

Create a Work Item Query on the target TFS Project and put path to the query in the mapping file. This query optimize the process as only the returned items are matched with the source. The columns returned are not important, as the mapping file specifies the fields to sync.

### 1.1.2	Target Work Item types

Each target Work Item type must have an Integer field reserved to host the ID of the source work item.

### 1.1.3	Permissions

You need an account in the source domain, say **TFS_WitSyncReader** that can execute the source WIQuery and another account in the target domain, say **TFS_WitSyncWriter**. It must have permission to execute the target WIQuery and to add and update Work Items in the mapped Areas and Iterations.

Can be the same account when target and source belongs to the same AD domain, or you can use shadow accounts or store credential in the Windows credential store .

## 1.2	Syntax

Syntax is
```
WitSync.exe –a SyncWorkItems -c <source_collection_url> -p <source_project_name> -d <destination_collection_url> -q <destination_project_name> -m <path_to_file> [-v] [-t] [-x BypassWorkItemStoreRules] [-x UseEditableProperty] [-x OpenTargetWorkItem|PartialOpenTargetWorkItem] [-x CreateThenUpdate]
```
The supported values for action are `SyncWorkItems` and `SyncAreasAndIterations`.

`Verbose` option prints detailed output.

`TestOnly` option tries action but does not save any change to target.

### 1.2.1	Advanced options

Permits fine control on `SyncWorkItems` behavior.

_Option_                    | _See also_
----------------------------|--------------------------------
`BypassWorkItemStoreRules`  | Disable Rule validation
`UseEditableProperty`       | Algorithm used to determine when a field is updatable. See [Field.IsEditable](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.field.iseditable.aspx) Property.
`OpenTargetWorkItem`        | Use [WorkItem.Open](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.open.aspx) Method to make the WorkItem updatable.
`PartialOpenTargetWorkItem` | Use [WorkItem.PartialOpen](http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.partialopen.aspx) Method to make the WorkItem updatable.
`CreateThenUpdate`          | WorkItems missing from the target are first added in the initial state specified by `InitalStateOnDestination`, then updated to reflect the state of the source.

## 1.3	Return values

Successful run returns `0`, any other number means error.

## 1.4	Mapping file

The hard work is writing the `Mapping.xml` file. It defines in detail the source and target mapping; here is a simple case

```XML
<Mapping>
  <SourceQuery>Shared Queries\MySourceQuery</SourceQuery>
  <DestinationQuery>Shared Queries\MyDestinationQuery</DestinationQuery>
  <AreaMap>
    <!-- map any Area path to root node in target project -->
    <Area SourcePath="*" DestinationPath=""/>
  </AreaMap>
  <IterationMap>	
    <!-- same (relative) path -->
    <Iteration SourcePath="*" DestinationPath="*"/>
  </IterationMap>
  <WorkItemMap SourceType="Feature" DestinationType="Feature">
    <IDField Source="System.ID" Destination="Sample.OriginatingID"/>
    <!—InitalStateOnDestination is used by the CreateThenUpdate option -->
    <States InitalStateOnDestination="New">
      <State Source="Proposed" Destination="New"/>
      <State Source="Active" Destination="In Progress"/>
      <State Source="Resolved" Destination="Done"/>
      <State Source="Closed" Destination="Removed"/>
    </States>
    <!-- not mapped (get value from rules or other fields) -->
    <Field Source="Area ID" Destination=""/>
    <Field Source="Iteration ID" Destination=""/>
    <Field Source="Reason" Destination=""/>
    <Field Source="State Change Date" Destination=""/>
    <Field Source="Created Date" Destination=""/>
    <Field Source="Changed Date" Destination=""/>
    <!-- built-in functions (need modifying tool sources to  add/change functions) -->
    <Field Source="State" Destination="State" Translate="MapState" />
    <Field Source="Area Path" Destination="Area Path" Translate="MapAreaPath"/>
    <Field Source="Iteration Path" Destination="Iteration Path" Translate="MapIterationPath"/>
    <!-- explicit mapping -->
    <Field Source="Title" Destination="Title"/>
    <Field Source="Description" Destination="Description"/>
    <Field Source="Assigned To" Destination="Assigned To"/>
    <!-- optional wildcard mapping -->
    <Field Source="*" Destination="*"/>
  </WorkItemMap>
</Mapping>
```

### 1.5.1	Source query

The tool should present itself as an account able to get the result from `Shared Queries\MySourceQuery` on the source

**Note**: query filters only which Work Items are synced, but does not obey the query in filtering the columns. Is the mapping files that determines which fields are copied and how.

### 1.5.2	Target query

On the target, the tool must be able to run `Shared Queries\MyDestinationQuery` and to write (Contribute access) in the project. On the first run, the target query must give an empty result; on subsequent runs, it must returns previously synced workitems.

This way, tool’s work is optimized by not scanning all workitems in the target project.

### 1.5.3	Area and Iteration mapping

You have some ways of mapping Area/Iteration paths: one is specific, an Area/Iteration path is mapped exactly to a literal path on target.
```
<Area SourcePath="Src\Area 1" DestinationPath="Dest\Area 2"/>
```

Another option is to use the source wildcard rule: all source paths are mapped to a specific target path.
```
<Iteration SourcePath="*" DestinationPath="Dest\Sprint 3"/>
```

If the destination is empty, it is mapped to the root node, so
```
<Iteration SourcePath="*" DestinationPath=""/>
```
is equivalent to
```
<Iteration SourcePath="*" DestinationPath="Dest"/>
```

The last option uses the destination wildcard rule: source paths are mapped to equivalent target paths, that is identical except for the root node.
```
<Area SourcePath="*" DestinationPath="*"/>
```

As you see, the rules have the same syntax and meaning for both Areas and Iterations.

### 1.5.4	Work Item Type mapping

A mapping file must contain at least a work item mapping.

A work item mapping must specify a field in the target work item type that holds the source ID.

State mapping is optional; when missing the target work item type must have at least the same state of the source.
The InitalStateOnDestination is used by the CreateThenUpdate option.

### 1.5.4	Field mapping

Field mapping rules can have 5 forms.

Explicit mapping: both source and destination specify a field.
```
<Field Source="Description" Destination="Description"/>
```

Unmapped fields: source has a name while destination is empty. This means that the field is not mapped; useful for computed fields like "Area ID" which gets value from "Area Path".
```
<Field Source="Area ID" Destination=""/>
```

Fixed value: destination field takes literal value.
```
<Field Source="Blocked" Destination="Blocked" Set="Yes" />
```

Wildcard rule: fields that exists on destination are copied. Should appear only once.
```
<Field Source="*" Destination="*"/>
```

Translation functions: Source values are converted via some built-in function.
```
<Field Source="State" Destination="State" Translate="MapState" />
<Field Source="Area Path" Destination="Area Path" Translate="MapAreaPath"/>
<Field Source="Iteration Path" Destination="Iteration Path" Translate="MapIterationPath"/>
```
The only functions available are:
Function         | Description
------------------------------------------------------------------------------------------
MapState         | Use State mapping
MapAreaPath      | Use Area mapping to convert source Area Path values to target
MapIterationPath | Use Iteration mapping to convert source Iteration Path values to target


## 1.6	Building the tool

Requires Visual Studio 2013 and Team Explorer 2013.

## 1.7	Tested scenarios
Tested on TFS 2013 and successfully synced data from 2010 to 2013.
