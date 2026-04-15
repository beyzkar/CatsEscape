# CatsEscape 🐈💨

**CatsEscape** is a professional, fast-paced 2D side-scrolling runner built with Unity. Take control of a nimble cat navigating through various thematic environments, avoiding obstacles, jumping over pitfalls, and competing for the top spot on a global leaderboard.

---

## 🎮 How to Play

### Objectives
- **Survive:** Navigate through 5 unique levels by dodging hurdles, obstacles, and enemies.
- **Collect:** Pick up **Fish** to restore health and gain **XP**.
- **Power-Up:** Find **Potions** to turn into "Big Cat," granting temporary speed and jump boosts.
- **Compete:** Reach the highest XP possible to secure your place on the global leaderboard.

### Controls
| Action | Key (PC/Mac) | Mobile |
| :--- | :--- | :--- |
| **Move Left/Right** | `A / D` or `Left/Right Arrow` | Joystick / Buttons |
| **Jump / Double Jump** | `Space` or `Up Arrow` | Jump Button |
| **Retry** | `Space` (on Game Over screen) | Retry Button |

---

## 🚀 Key Features

-   **5 Thematic Levels:** Progress through specialized environments with increasing difficulty.
-   **Advanced Physics:** Responsive "Better Jump" mechanics for precise platforming.
-   **Professional Leaderboard System:**
    -   **Single Source of Truth (SSOT):** A dedicated Node.js server manages the master leaderboard database.
    -   **Offline Support:** Earn scores while offline! The game uses a **Pending Score Queue** to automatically sync your scores when the connection is restored.
    -   **Local Caching:** Fast loading via a read-only local cache.
-   **Character Selection:** Multiple animated cat skins with unique personalities.

---

## 💻 Tech Stack

### Frontend (Game)
-   **Engine:** Unity 2022.x+
-   **Language:** C#
-   **Rendering:** Universal Render Pipeline (URP)
-   **UI:** TextMeshPro for optimized typography.
-   **Networking:** UnityWebRequest for REST API communication.

### Backend (API)
-   **Environment:** Node.js
-   **Framework:** Express.js
-   **Database:** Local JSON Persistence (Server-side)
-   **Architecture:** RESTful API with SSOT pattern.

---

## ⚙️ Setup & Installation

### 1. Server Setup
Before running the game, start the leaderboard server:
```bash
cd Server
npm install
node index.js
```
The server will start at `http://localhost:8080`.

### 2. Unity Project
1.  Clone the repository and open it in **Unity Hub**.
2.  Open the `Main Menu` scene.
3.  Select the `LeaderboardManager` object in the inspector and ensure the `LeaderboardApiService` is correctly linked with the standard `Base URL` (e.g., `http://localhost:8080`).

---

## 📄 License
This project is for educational and personal use. Designed with ❤️ for cat lovers and game devs.
