. ./common.ps1

# snapshot Collection
& $TfsConfig collection /detach /collectionName:WitSync
Invoke-Sqlcmd -Query "ALTER DATABASE [Tfs_WitSync] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; EXEC master.dbo.sp_detach_db @dbname = N'Tfs_WitSync'" -ServerInstance .\SQLEXPRESS
cd $env:SystemDrive
Copy-Item "$sqlDataPath\Tfs_WitSync*" "$dataSavePath"

./restartCollection.ps1
