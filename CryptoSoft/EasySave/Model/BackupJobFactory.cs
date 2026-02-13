using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface IBackupJobFactory {
    public static abstract IBackupJob Create(IBackupJobConfiguration configuration);
    public static abstract List<IBackupJob> Create(List<IBackupJobConfiguration> configurations);
} 

public class BackupJobFactory : IBackupJobFactory { 
    /// <summary>
    /// Creates a backup job based on the provided configuration.
    /// The configuration should specify the type of backup job to create.
    /// </summary>
    /// <param name="configuration">The configuration for the backup job.</param>
    /// <returns>An instance of IBackupJob.</returns>
    /// <exception cref="ArgumentException">Thrown when the backup job type is unknown.</exception>
    public static IBackupJob Create(IBackupJobConfiguration configuration) {
        return configuration.Type switch {
            "Differential" => new DifferentialBackupJob(
                                configuration.Name,
                                new DirectoryHandler(configuration.Source),
                                new DirectoryHandler(configuration.Destination)
                            ),
            "Complete" => new CompleteBackupJob(
                                configuration.Name,
                                new DirectoryHandler(configuration.Source),
                                new DirectoryHandler(configuration.Destination)
                            ),
            _ => throw new ArgumentException($"Unknown backup job type: {configuration.Type}"),
        };
    }

    /// <summary>
    /// Creates a list of backup jobs based on the provided configurations.
    /// Each configuration should specify the type of backup job to create.
    /// </summary>
    /// <param name="configurations">The list of configurations for the backup jobs.</param>
    /// <returns>A list of IBackupJob instances.</returns>
    /// <exception cref="ArgumentException">Thrown when the backup job type is unknown.</exception>
    public static List<IBackupJob> Create(List<IBackupJobConfiguration> configurations) {
        List<IBackupJob> jobs = [];
        foreach (IBackupJobConfiguration configuration in configurations) {
            jobs.Add(BackupJobFactory.Create(configuration));
        }
        return jobs;
    }
}

 