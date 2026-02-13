> *Projet étudiant de création d'un logiciel de sauvegarde en C# avec le framework WPF.*

# EasySave

## Présentation

EasySave est un logiciel de sauvegarde développé par l'entreprise ProSoft. Ce logiciel permet de créer et d'exécuter des travaux de sauvegarde pour protéger efficacement vos données importantes.

L'application est conçue selon l'architecture MVVM (Model-View-ViewModel) et offre une interface graphique moderne ainsi que de nombreuses fonctionnalités avancées pour répondre aux besoins des utilisateurs professionnels.

## Fonctionnalités principales

-   **Types de sauvegarde** : Sauvegarde complète et différentielle
-   **Exécution parallèle** : Traitement simultané de plusieurs travaux de sauvegarde
-   **Gestion des priorités** : Traitement prioritaire des fichiers selon leur extension
-   **Chiffrement** : Protection des données sensibles via l'outil CryptoSoft
-   **Détection de processus métiers** : Pause automatique des sauvegardes lors de l'exécution de logiciels spécifiques
-   **Interface graphique** : Interface moderne et intuitive en WPF
-   **Contrôle à distance** : Pilotage via une application distante (EasyRemote)
-   **Journalisation** : Suivi détaillé des opérations de sauvegarde

## Architecture

EasySave est structuré selon le modèle MVVM (Model-View-ViewModel) et comprend plusieurs composants :

-   **EasySave** : Application principale avec interface graphique
-   **CryptoSoft** : Utilitaire de cryptage de fichiers
-   **EasyRemote** : Interface de contrôle à distance
-   **Logger** : Composant de journalisation

## Prérequis

-   Windows 10 ou supérieur
-   .NET 8.0 ou supérieur
-   100 Mo d'espace disque minimum
-   4 Go de RAM recommandés

## Installation

1. Téléchargez la dernière version depuis la section "Releases"
2. Extrayez le contenu de l'archive ZIP
3. Exécutez le fichier `EasySave.exe`

## Utilisation

### Création d'un travail de sauvegarde

1. Lancez l'application EasySave
2. Cliquez sur "Ajouter un travail"
3. Renseignez :
    - Un nom pour la sauvegarde
    - Le répertoire source
    - Le répertoire de destination
    - Le type de sauvegarde (complète ou différentielle)
4. Validez en cliquant sur "Enregistrer"

![image](https://github.com/user-attachments/assets/dba8de35-ea8c-4fc1-b370-7b11d640cd55)

### Exécution des travaux

1. Dans la liste des travaux, sélectionnez le(s) travail(aux) à exécuter
2. Cliquez sur "Exécuter" pour lancer la sauvegarde
3. Suivez la progression en temps réel

![image](https://github.com/user-attachments/assets/1d6dbc1f-d6c8-4fe0-95ff-e96e916f16b3)

### Configuration

Accédez aux paramètres pour configurer :

-   Le fichier d'état
-   Le fichier de journalisation
-   Les extensions à chiffrer
-   Les processus métiers bloquants
-   Les extensions prioritaires
-   La clé de chiffrement

![image](https://github.com/user-attachments/assets/6766fede-f34f-4f3e-8efb-15b40d2f795e)

## Fonctionnalités avancées

### Chiffrement des fichiers

EasySave utilise CryptoSoft pour chiffrer les fichiers sensibles. Vous pouvez définir :

-   Les extensions de fichiers à chiffrer
-   La clé de chiffrement (générée ou personnalisée)

### Gestion des priorités

Les fichiers avec des extensions prioritaires sont traités en premier lors des sauvegardes, optimisant ainsi le temps de traitement des données importantes.

### Détection des processus

L'application peut mettre en pause automatiquement les sauvegardes lorsque certains processus métiers sont en cours d'exécution, évitant ainsi les conflits et les problèmes de performance.

### Interface distante

EasySave peut être piloté à distance via l'application EasyRemote, permettant de surveiller et de contrôler les sauvegardes depuis un autre poste.

![image](https://github.com/user-attachments/assets/d45d2a69-cf16-4ab3-a762-9376d232c390)
![image](https://github.com/user-attachments/assets/6f9cb555-2ef4-4f9a-93b1-fda9efe5dfb1)
![image](https://github.com/user-attachments/assets/8ed7d4b8-124a-46a2-b5d1-3591a85676dd)

### Limitation de la taille des fichiers en traitement simultané

Pour optimiser les performances, EasySave limite automatiquement la taille totale des fichiers traités simultanément.

## Journalisation

EasySave génère deux types de fichiers journaux :

-   **Journal d'activité** : Enregistre toutes les actions de sauvegarde avec horodatage, taille des fichiers, temps de transfert, etc.
-   **Fichier d'état** : Suivi en temps réel de l'état des travaux de sauvegarde

![image](https://github.com/user-attachments/assets/d0ac1ba4-4171-4696-9c9b-380b4018cb5a)

## Évolution du logiciel

EasySave a évolué à travers plusieurs versions :

### Version 1.0

-   Application console en .NET Core
-   Création de jusqu'à 5 travaux de sauvegarde
-   Exécution séquentielle des travaux
-   Interface multilingue (français, anglais)

### Version 2.0

-   Interface graphique WPF
-   Détection des processus métiers
-   Chiffrement des fichiers
-   Fichiers journaux au format JSON ou XML

### Version 3.0

-   Sauvegarde en parallèle
-   Gestion des fichiers prioritaires
-   Limitation de la taille des fichiers en traitement simultané
-   Interface distante via socket TCP
-   Mono-instance de CryptoSoft pour optimiser les performances

## Support

Pour toute question ou problème, veuillez contacter l'équipe de développement.

## Licence

Le projet EasySave est développé par ProSoft.
Prix unitaire : 200 €HT
Contrat de maintenance annuel 5/7 8-17h (mises à jour incluses) : 12% du prix d'achat
