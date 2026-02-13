using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyRemote.Model {
    public interface IBackupJob {
        public string Name { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
    }
    class BackupJob : IBackupJob {
        public required string Name { get; set; }
        public required string Source { get; set; }
        public required string Destination { get; set; }
        public required string Type { get; set; }
    }
}
