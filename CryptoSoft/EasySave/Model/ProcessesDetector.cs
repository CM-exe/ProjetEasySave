using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model {

    #region Event Args Classes
    /// <summary>
    /// Arguments d'événement pour un processus spécifique
    /// </summary>
    public class ProcessEventArgs(string processName) : EventArgs {
        /// <summary>
        /// Nom du processus concerné par l'événement
        /// </summary>
        public string ProcessName { get; } = processName;
    }

    /// <summary>
    /// Arguments d'événement pour une liste de processus
    /// </summary>
    public class ProcessesEventArgs(List<string> processes) : EventArgs {
        /// <summary>
        /// Liste des processus concernés par l'événement
        /// </summary>
        public List<string> Processes { get; } = processes;
    }
    #endregion

    #region Event Delegates
    /// <summary>
    /// Délégué pour l'événement de démarrage d'un processus
    /// </summary>
    public delegate void ProcessStartedEventHandler(object sender, ProcessEventArgs e);

    /// <summary>
    /// Délégué pour l'événement d'arrêt d'un processus
    /// </summary>
    public delegate void ProcessEndedEventHandler(object sender, ProcessEventArgs e);

    /// <summary>
    /// Délégué pour l'événement indiquant qu'aucun processus surveillé n'est en cours d'exécution
    /// </summary>
    public delegate void NoProcessRunningEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Délégué pour l'événement indiquant qu'un ou plusieurs processus surveillés sont en cours d'exécution
    /// </summary>
    public delegate void OneOrMoreProcessRunningEventHandler(object sender, ProcessesEventArgs e);
    #endregion

    #region Interface
    /// <summary>
    /// Interface définissant le contrat pour un détecteur de processus
    /// </summary>
    public interface IProcessesDetector {
        /// <summary>
        /// Vérifie l'état actuel des processus surveillés
        /// </summary>
        /// <returns>True si au moins un processus surveillé est en cours d'exécution</returns>
        public bool CheckProcesses();

        /// <summary>
        /// Retourne true si au moins un processus surveillé est en cours d'exécution
        /// </summary>
        /// <returns>True si au moins un processus surveillé est actif, sinon false</returns>
        public bool HasOneOrMoreProcessRunning();

        /// <summary>
        /// Événement déclenché lors du démarrage d'un processus surveillé
        /// </summary>
        public event ProcessStartedEventHandler? ProcessStarded;

        /// <summary>
        /// Événement déclenché lors de l'arrêt d'un processus surveillé
        /// </summary>
        public event ProcessEndedEventHandler? ProcessEnded;

        /// <summary>
        /// Événement déclenché quand aucun processus surveillé n'est en cours d'exécution
        /// </summary>
        public event NoProcessRunningEventHandler? NoProcessRunning;

        /// <summary>
        /// Événement déclenché quand un ou plusieurs processus surveillés sont en cours d'exécution
        /// </summary>
        public event OneOrMoreProcessRunningEventHandler? OneOrMoreProcessRunning;
    }
    #endregion

    #region ProcessesDetector Implementation
    /// <summary>
    /// Implémentation du détecteur de processus avec surveillance en temps réel
    /// </summary>
    public class ProcessesDetector : IProcessesDetector {

        #region Private Fields
        /// <summary>
        /// Dictionnaire contenant l'état de chaque processus surveillé (nom -> état actuel)
        /// </summary>
        private Dictionary<string, bool> Processes { get; set; } = [];

        /// <summary>
        /// Tâche en arrière-plan pour la surveillance continue
        /// </summary>
        private Task? Task { get; set; } = null!;

        /// <summary>
        /// Dernier état global connu (true = au moins un processus actif, false = aucun processus actif)
        /// </summary>
        private bool _LastState = false;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialise le détecteur de processus et démarre la surveillance
        /// </summary>
        public ProcessesDetector() {
            // Récupération de la liste des processus à surveiller depuis la configuration
            List<string> processes = Configuration.Instance?.Processes.ToList() ?? throw new Exception("Configuration is null");

            // Initialisation du dictionnaire avec tous les processus à l'état "non actif"
            foreach (string process in processes) {
                this.Processes.Add(process, false);
            }

            // Démarrage de la tâche de surveillance en arrière-plan
            this.Task = Task.Run(() => {
                while (true) {
                    // Vérification de l'état actuel des processus
                    bool state = this.CheckProcesses();

                    // Détection des changements d'état global
                    if (state != this._LastState) {
                        this._LastState = state;

                        // Déclenchement des événements selon le nouvel état
                        if (state) {
                            // Au moins un processus est maintenant actif
                            OneOrMoreProcessRunning?.Invoke(this, new ProcessesEventArgs(this.Processes.Keys.ToList()));
                        } else {
                            // Aucun processus n'est plus actif
                            NoProcessRunning?.Invoke(this, EventArgs.Empty);
                        }
                    }

                    // Attente d'une seconde avant la prochaine vérification
                    Task.Delay(1000).Wait();
                }
            });

            // Abonnement aux changements de configuration
            if (Configuration.Instance is not null) {
                Configuration.Instance.ConfigurationChanged += OnConfigurationChanged;
            }
        }
        #endregion

        #region Public Methods
        public bool HasOneOrMoreProcessRunning() {
            // Vérification de l'état des processus et déclenchement des événements appropriés
            return this._LastState;
        }
        #endregion

        #region Configuration Management
        /// <summary>
        /// Gestionnaire d'événement pour les changements de configuration
        /// </summary>
        /// <param name="sender">Source de l'événement</param>
        /// <param name="e">Arguments contenant le nom de la propriété modifiée</param>
        private void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e) {
            if (Configuration.Instance is null) return;

            // Ne traiter que les changements de la liste des processus
            if (e.PropertyName != nameof(Configuration.Instance.Processes)) return;

            // Mise à jour du dictionnaire en préservant l'état des processus existants
            this.Processes = Configuration.Instance.Processes.ToDictionary(
                process => process,
                process => this.Processes.TryGetValue(process, out bool value) && value
            );
        }
        #endregion

        #region Process Detection
        /// <summary>
        /// Vérifie l'état de tous les processus surveillés et déclenche les événements appropriés
        /// </summary>
        /// <returns>True si au moins un processus surveillé est en cours d'exécution</returns>
        public bool CheckProcesses() {
            // Récupération de tous les processus système actuellement en cours d'exécution
            List<Process> runningProcesses = [.. Process.GetProcesses()];

            // Vérification de chaque processus surveillé
            foreach (string process in this.Processes.Keys) {
                // Recherche du processus dans la liste des processus actifs (insensible à la casse)
                bool isRunning = runningProcesses.Any(p =>
                    p.ProcessName.Equals(process, StringComparison.OrdinalIgnoreCase)
                );

                // Détection du démarrage d'un processus
                if (isRunning && !this.Processes[process]) {
                    this.Processes[process] = true;
                    ProcessStarded?.Invoke(this, new ProcessEventArgs(process));
                }
                // Détection de l'arrêt d'un processus
                else if (!isRunning && this.Processes[process]) {
                    this.Processes[process] = false;
                    ProcessEnded?.Invoke(this, new ProcessEventArgs(process));

                    if (this.Processes.Values.All(p => !p)) {
                        // Si tous les processus sont arrêtés, déclenche l'événement NoProcessRunning
                        NoProcessRunning?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            // Retourne true si au moins un processus surveillé est actif
            return this.Processes.Values.Any(p => p);
        }
        #endregion

        #region Events
        /// <summary>
        /// Événement déclenché lors du démarrage d'un processus surveillé
        /// </summary>
        public event ProcessStartedEventHandler? ProcessStarded;

        /// <summary>
        /// Événement déclenché lors de l'arrêt d'un processus surveillé
        /// </summary>
        public event ProcessEndedEventHandler? ProcessEnded;

        /// <summary>
        /// Événement déclenché quand aucun processus surveillé n'est en cours d'exécution
        /// </summary>
        public event NoProcessRunningEventHandler? NoProcessRunning;

        /// <summary>
        /// Événement déclenché quand un ou plusieurs processus surveillés sont en cours d'exécution
        /// </summary>
        public event OneOrMoreProcessRunningEventHandler? OneOrMoreProcessRunning;
        #endregion


    }
    #endregion
}