. ./common.ps1

# drop current and apply snapshot
& $TfsConfig collection /detach /collectionName:WitSync
Invoke-Sqlcmd -Query "ALTER DATABASE [Tfs_WitSync] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Tfs_WitSync]" -ServerInstance .\SQLEXPRESS
Copy-Item "$dataSavePath\Tfs_WitSync*" $sqlDataPath

./restartCollection.ps1
