. $PSScriptRoot\common.ps1

& $TfsServiceControl unquiesce

# drop current and apply snapshot
& $TfsConfig collection /detach /collectionName:WitSync
Invoke-Sqlcmd -Query "ALTER DATABASE [Tfs_WitSync] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Tfs_WitSync]" -ServerInstance .\SQLEXPRESS
cd $env:SystemDrive
Copy-Item "$dataSavePath\Tfs_WitSync*" $sqlDataPath

& "$PSScriptRoot\restartCollection.ps1"
