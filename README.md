| Branch  | Status |
|---------|--------|
| Develop | [![Build status: Develop branch](<https://ci.appveyor.com/api/projects/status/github/giuliov/WitSync?branch=develop&svg=true>)](https://ci.appveyor.com/project/giuliov/witsync) |
| Main | [![Build status: Master branch](https://ci.appveyor.com/api/projects/status/github/giuliov/WitSync?branch=master&svg=true>)](https://ci.appveyor.com/project/giuliov/witsync) |

Latest artifacts: <https://ci.appveyor.com/project/giuliov/witsync/build/artifacts>

# WitSync

WitSync is a command line tool that can copy TFS Work Item data (Global Lists, Areas, Iterations, Work Items) from a source to a target project.
It is designed to be idempotent, that is, you may run it multiple times and get the same results.


## Scenarios
You can use WitSync in multiple ways:

 - Clone Project management data in another collection
 - Synchronize a subset of Work Items between two independently managed Projects
 - Push some Global list on all your Projects
 - Push an Iteration cadence on all your Projects

and probably you may think more.

## What is not designed for

You cannot clone an entire project; WitSync cannot copy data like Version Control, Builds, Tests (see below) or Lab Environments.

#### Test data
Test Cases and Shared Steps were implemented as Work Items and can be managed by WitSync on any TFS version. Test Plans and Test Suites became Work Items with TFS 2013 Update 3, while in previous versions, one must use some specific API. For this reason, WitSync do not support Test Plans and Test Suites pre-2013.3 (this may change in the future).
Test Results are not supported.

#### Why no GUI?
While command line is less user friendly to start, WitSync is a Console application because it easier to automate via Windows Task Scheduler or similar.
The synchronization code has no User Interface code, so one day someone can write a fancy modern UI on top.


## How it works
Synchronization is always in one direction. You can setup a bi-directional sync by running the tool twice swapping the source and destination roles. 
WitSync works in phases:
 - Global Lists
 - Areas
 - Iterations
 - Work Items

The tool is controlled via a configuration file and the options specified on the command line; through the command line you may override the configuration.

A typical execution is
```Batchfile
WitSync.exe -m=MyMappingFile.yml
```

#### Global List phase
Clones the selected Global Lists from source to target.

#### Areas phase
Add new source Areas nodes to the target.

#### Iterations phase
Add new source Iterations nodes to the target.

#### Work Items phase
This is the most complex and tunable. WitSync reads the work items returned from a Work Item Query on the source TFS Project, search for matching work items on the target TFS Project and update the latter, including links and attachments.


## Documentation
Detailed documentation starts [here](docs/Introduction.md).


## Support
The test cases are so many that only a subset have been checked. Versions of WitSync are used in production environments, so it is known to work.
Please submit bug with complete logs, work item type definitions and explain your scenario.


## License
WitSync is release in source code under [MIT](http://opensource.org/licenses/MIT) license. 
