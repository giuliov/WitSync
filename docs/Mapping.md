# Mapping file

Mapping file is not strictly required if you want a dumb sync.

If you want fine control, you must writing the _Mapping_ file. It defines in detail the source and target mapping, 
so it is a tedious work.

## Sample mapping

```XML
<Mapping>
  <SourceQuery>Shared Queries\MySourceQuery</SourceQuery>
  <DestinationQuery>Shared Queries\MyDestinationQuery</DestinationQuery>
  <IndexFile>index.xml</IndexFile>
  <AreaMap>
    <!-- map any Area path to root node in target project -->
    <Area SourcePath="*" DestinationPath=""/>
  </AreaMap>
  <IterationMap>	
    <!-- same (relative) path -->
    <Iteration SourcePath="*" DestinationPath="*"/>
  </IterationMap>
  <WorkItemMap SourceType="Feature" DestinationType="Feature" Attachments="true">
    <!-- this is used instead of an index -->
    <IDField Source="System.ID" Destination="Sample.OriginatingID"/>
    <!-- InitalStateOnDestination is used by the CreateThenUpdate option -->
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
  <!-- add more work item type mappings -->
</Mapping>
```


## Index file

WitSync uses the work item ID field (`System.Id`) to uniquely identify the wrok items to syncronize. There are two ways to relate work items on the destination  project with the source:
    - using a field to holds the source ID, useful when the ID is meaningful to users (like a bug number)
    - using an external file to track mapping, useful when replicating a project on a different TFS collection or instance

When you specify an _Index_ file
```XML
<IndexFile>relative_or_absolute_path_to_file</IndexFile>
```
you get the maximum freedom in mapping work item schemas. WitSync looks up the Index file to know if a work item has a correspondent on the destination project; if not found, a new work item is created. On a match, the existing work item on the target project is updated.
You can specify the file on the command line, and this takes precedence and the element in the mapping file is optional.

Make sure to properly backup this file, otherwise the tool will re-create new workitems instead of updating the existing  workitems.


## Source query

The tool should present itself as an account able to get the result from the source query. The source query can be expressed in [WIQL](http://msdn.microsoft.com/en-us/library/bb130306.aspx) or be the name of an existing query (Shared queries are preferred).

If you want to replicate all workitems, you can use a generic query like this

```XML
<WorkItemQuery Version="1">
  <TeamFoundationServer>http://localhost:8080/tfs/DefaultCollection</TeamFoundationServer>
  <TeamProject>SourceProject</TeamProject>
  <Wiql>
    SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags]
    FROM WorkItems
    WHERE [System.TeamProject] = @project
    and [System.WorkItemType] &lt;&gt; ''
    and [System.State] &lt;&gt; ''
  </Wiql>
</WorkItemQuery>
```

this query extracts all work items from SourceProject.
This is also the default if you do not specify the source query: all kind of Work Items are read from the source and compared to destination.

**Note**: query filters only which Work Items are synced, but does not obey the query in filtering the columns. Is the mapping files that determines which fields are copied and how.

## Target query

On the target, the tool must be able to run `Shared Queries\MyDestinationQuery` and to write (Contribute access) in the project. On the first run, the target query must give an empty result; on subsequent runs, it must returns previously synced workitems.

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
    and [System.WorkItemType] &lt;&gt; ''
    and [System.State] &lt;&gt; ''
  </Wiql>
</WorkItemQuery>
```

this query selects all work items from DestProject.
If you do not specify a destination query, by default all kind of Work Items are read from the destination and compared to source.


## Area and Iteration mapping

You have some ways of mapping Area/Iteration paths: one is specific, an Area/Iteration path is mapped exactly to a literal path on target.
```XML
<Area SourcePath="Src\Area 1" DestinationPath="Dest\Area 2"/>
```
You can use the `SyncAreasAndIterations` option to replicate the paths on the destination.

Another option is to use the source wildcard rule: all source paths are mapped to a specific target path.
```XML
<Iteration SourcePath="*" DestinationPath="Dest\Sprint 3"/>
```
Useful in partial-replica scenarios, where you are transferring specific data subsets.

If the destination is empty, it is mapped to the root node, so
```XML
<Iteration SourcePath="*" DestinationPath=""/>
```
is equivalent to
```XML
<Iteration SourcePath="*" DestinationPath="Dest"/>
```
This is the default if you do not specify an Area / Iteration mapping.

The last option uses the destination wildcard rule: source paths are mapped to equivalent target paths, that is identical except for the root node.
```XML
<Area SourcePath="*" DestinationPath="*"/>
```

As you see, the rules have the same syntax and meaning for both Areas and Iterations.


## Work Item Type mapping

A mapping file usually contains some work item mappings.

The syntax is
```XML
  <WorkItemMap SourceType="source_work_item_type" DestinationType="destination_work_item_type" Attachments="boolean">
    <!-- IDField is optional -->
    <IDField Source="System.ID" Destination="field_reference_name_on_destination"/>
    <!-- State table is optional -->
    <States InitalStateOnDestination="optional_name_of_state_on_destination_work_item_type">
    </States>
    <!-- list of Field mapping rules -->
    <Field />
  </WorkItemMap>
```

Note that the work item type can be different, e.g. mapping Bugs to Issues.

To avoid syncing attachments, set Attachments to `false`.

If you do not specify any Work Item mapping, Work Items maps to the same type on the destination, assuming the same set of States and Fields.

### ID
WitSync uses the work item ID field (`System.Id`) to uniquely identify them. There are two ways to relate work items on the destination  project:
    - using a field to holds the source ID, useful when the ID is meaningful to users (like a bug number)
    - using an external file to track mapping, useful when replicating a project on a different TFS collection or instance

The field in the target work item type that holds the source ID must be an integer. If not using the `IDField`, you must specify an _Index_ file and take care of it: make sure to properly backup it, otherwise the tool will re-create new workitems instead of updating the existing  workitems.

### States
State mapping table is optional; when missing the target work item type must have at least the same states of the source.
The `InitalStateOnDestination` is used by the `CreateThenUpdate` option (see [Advanced options](./CommandLineOptions.md)).

```XML
<State Source="Proposed" Destination="New"/>
```

## Field mapping

Field mapping rules specify which fields of the source work item type are copied over the target. Only the writable fields are updated.

Use field names (e.g. Title), reference names do not work (e.g. System.Title).

Field mapping rules can have 5 forms.

### Explicit field mapping
Both source and destination specify a field.
```XML
<Field Source="Description" Destination="Description"/>
```

### Unmapped fields
The field is not mapped, useful for computed fields like "Area ID" whose value is derived from "Area Path".

Source has a name while destination is empty.
```XML
<Field Source="Area ID" Destination=""/>
```

### Fixed value
Destination field takes literal value.
```XML
<Field Source="Blocked" Destination="Blocked" Set="Yes" />
```

### Wildcard rule
Fields that exists on destination are copied. Should appear only once.
```XML
<Field Source="*" Destination="*"/>
```
This is the default Rule if no Field rule is present.

### Translation functions
Source values are converted via some built-in function.
```XML
<Field Source="State" Destination="State" Translate="MapState" />
<Field Source="Area Path" Destination="Area Path" Translate="MapAreaPath"/>
<Field Source="Iteration Path" Destination="Iteration Path" Translate="MapIterationPath"/>
```
The only available functions are:

|Function         | Description                                                             |
|-----------------|-------------------------------------------------------------------------|
|MapState         | Use State mapping                                                       |
|MapAreaPath      | Use Area mapping to convert source Area Path values to target           |
|MapIterationPath | Use Iteration mapping to convert source Iteration Path values to target |


## Link Type mapping

A mapping file can specify Link Type mappings.

The syntax is
```XML
  <LinkTypeMap>
    <LinkType SourceType="source_work_item_type" DestinationType="destination_work_item_type" />
    <!-- wildcard rule -->
    <LinkType SourceType="*" DestinationType="*" />
  </LinkTypeMap>
```

Note that the work item type can be different, e.g. mapping Bugs to Issues.

If you do not specify any Link Type mapping, WitSync will use the type on the destination with identical name.