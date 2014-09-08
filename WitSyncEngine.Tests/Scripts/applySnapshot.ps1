. ./common.ps1

# drop current and apply snapshot
& $TfsConfig collection /detach /collectionName:WitSync
Invoke-Sqlcmd -Query "ALTER DATABASE [Tfs_WitSync] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Tfs_WitSync]" -ServerInstance .\SQLEXPRESS
Copy-Item "$dataSavePath\Tfs_WitSync*" $sqlDataPath
Invoke-Sqlcmd -Query "CREATE DATABASE [Tfs_WitSync] ON
( FILENAME = N'$sqlDataPath\Tfs_WitSync.mdf' ),
( FILENAME = N'$sqlDataPath\Tfs_WitSync_log.ldf' )
FOR ATTACH" -ServerInstance .\SQLEXPRESS

& $TfsConfig collection /attach /collectionDb:$env:COMPUTERNAME\SQLEXPRESS`;Tfs_WitSync /noprompt
