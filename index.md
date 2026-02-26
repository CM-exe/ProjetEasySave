---
_layout: landing
---

# Welcome to EasySave Documentation

**EasySave** is a robust, lightweight, and scalable backup management software designed to secure your data efficiently. Whether you need full daily backups or quick differential saves, EasySave provides a reliable and modular solution tailored to your needs.

---

## Key Features

* **Smart Backup Strategies:** Choose between **Complete** (full copy) and **Differential** (only modified files) backup modes to optimize your storage and time.
* **Real-Time & Daily Logging:** Track every file transfer and system state in real-time. Logs can be formatted in **JSON** or **XML** and stored locally or sent to a centralized remote server via TCP.
* **On-the-Fly Encryption:** Seamlessly encrypt specific file extensions using the integrated **CryptoSoft** XOR encryption module.
* **Business Software Detection:** Automatically pauses resource-heavy backups when critical business applications are launched to preserve system performance.
* **Large File Handling:** Prioritizes file extensions and limits concurrent large file processing using advanced semaphore logic to prevent disk bottlenecking.
* **Multilingual Support:** Fully dynamic UI supporting multiple languages (English, French) via a dedicated `LanguageService`.

---

## Architecture Overview

EasySave is built using the **MVVM (Model-View-ViewModel)** architectural pattern in **C# / WPF**, ensuring a clean separation of concerns:

* **Model:** Handles the core business logic, file manipulation, state management, and strategy execution (`CompleteSave`, `DifferentialSave`, `SaveSpace`).
* **ViewModel:** Acts as the reactive bridge, managing UI states (`SaveJobState`), translations, and asynchronous background monitoring.
* **View:** A modern WPF-based graphical interface (and a functional CLI) for smooth user interactions.
* **External Libraries:** Employs modular internal libraries such as `EasyLog` for logging management and `CryptoSoft` for cryptography.


