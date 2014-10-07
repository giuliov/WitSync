. ./common.ps1

# drop current and apply snapshot
Invoke-Sqlcmd -Query "CREATE DATABASE [Tfs_WitSync] ON
( FILENAME = N'$sqlDataPath\Tfs_WitSync.mdf' ),
( FILENAME = N'$sqlDataPath\Tfs_WitSync_log.ldf' )
FOR ATTACH" -ServerInstance .\SQLEXPRESS
cd $env:SystemDrive
& $TfsConfig collection /attach /collectionDb:$env:COMPUTERNAME\SQLEXPRESS`;Tfs_WitSync /noprompt
