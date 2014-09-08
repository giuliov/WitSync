# WitSync Manual

WitSync is a simple command line tool that can copy TFS Work Item data from a source to a target project.

It is designed to be idempotent, that is, it can be run multiple times and getting the same result. The tool will pull the work items returned from a Work Item Query on the source TFS Project, compare to matching work items on the target TFS Project and update the latters.
Command line is less user friendly to start, but easy to automate.

The tool is controlled via a mapping file and the options specified on the command line. On the command line you specify connection information that may vary from a dry-run on a test environment to production. The mapping file contains all the fixed syncronization information, based on structural data.

A typical execution is
```Batchfile
WitSync.exe -a SyncWorkItems -c http://localhost:8080/tfs/DefaultCollection -p SourceProject -d http://localhost:8080/tfs/DefaultCollection -q DestProject -m MyMappingFile.xml –v
```

You can also project only a data subset from the source to the target. This is very useful in scenario where TFS is used at different organization and they want to sync only a couple of work item types; it was indeed the first application for WitSync.

## Prerequisites

The both TFS environments should be prepared before using WitSync and starting synchronizing data.

### Source and Target

Create a Work Item Query on the source TFS Project and put path to the query in the mapping file. The columns returned are not important, as the mapping file specifies the fields to sync.

Create a Work Item Query on the target TFS Project and put path to the query in the mapping file. This query optimize the process as only the returned items are matched with the source. The columns returned are not important, as the mapping file specifies the fields to sync.

### Target Work Item types

Each target Work Item type must have an Integer field reserved to host the ID of the source work item, _unless_ you opt for using a local _Index_ file.
If you use and Index file, make sure to properly backup it, otherwise the tool will re-create new workitems instead of updating the existing  workitems.

### Permissions

You need an account in the source domain, say **TFS_WitSyncReader** that can execute the source WIQuery and another account in the target domain, say **TFS_WitSyncWriter**. It must have permission to execute the target WIQuery and to add and update Work Items in the mapped Areas and Iterations.

You can use a single account when target and source belongs to the same AD domain, or you can use shadow accounts, or store credential in the Windows credential store; finally, you can specify credentials on the command line.


## Command line Syntax

Syntax is
```Batchfile
WitSync.exe –a <action> -c <source_collection_url> -p <source_project_name> -d <destination_collection_url> -q <destination_project_name> [-m <path_to_mapping_file>] [-v[erbose]] [-t[est]] [_advanced_options_]
```
The supported values for action are `SyncWorkItems` and `SyncAreasAndIterations`. This gives flexibility as Area/Iteration can be different in the two projects and you may not want syncronyze it.

`Verbose` option prints detailed output.

`Test` option tries action but does not save any change to target. Use of index file is specified in the mapping.

More details can be found in [Command line options](docs/CommandLineOptions.md).


## Return values

Successful run returns `0`, any other number means error.

The output will contain messages explaning what went wrong. To create a log file, simply redirect the output.


## Mapping file

Mapping file is optional in case you want a hi-fidelity copy of the source, otherwise you have to go through the hard work of writing one. It defines in detail the source and target mapping; a simple case folows.

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
    <!-- sample explicit mapping -->
    <Field Source="Title" Destination="Title"/>
    <Field Source="Description" Destination="Description"/>
    <Field Source="Assigned To" Destination="Assigned To"/>
    <!-- optional wildcard mapping -->
    <Field Source="*" Destination="*"/>
  </WorkItemMap>
  <LinkTypeMap>
    <!-- wildcard rule -->
    <LinkType SourceType="*" DestinationType="*" />
  </LinkTypeMap>
</Mapping>
```

More details can be found in [Mapping file](docs/Mapping.md).


## Building the tool

Requires Visual Studio 2013 and Team Explorer 2013.


## Tested scenarios

The combinations in the following table have been successfully tested.

  Source     | Destination
-------------|-------------
TFS 2010 SP1 | TFS 2013.2
TFS 2013.2   | TFS 2013.2
TFS 2013.2   | VSO

