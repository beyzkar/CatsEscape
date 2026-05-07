# Cats Escape 🐈

**Cats Escape** is a high-octane, professional 2D side-scrolling runner built with Unity. Embark on a thrilling journey as a nimble cat escaping through diverse environments, collecting rewards, and outsmarting obstacles. With a robust backend and global leaderboard, every jump counts!

---

## 🚀 Key Features

- **Advanced Level System:** 5 unique levels with increasing difficulty and dynamic object spawning.
- **Dual Authentication:** Secure persistent progress via **Google Login** or instant play with **Guest Mode**.
- **Global Leaderboard:** Real-time leaderboard synchronization powered by a custom Node.js backend.
- **Player Progression:** Track total XP, highest level reached, and detailed player stats on the profile page.
- **Mobile Optimized:** Full Android support including Responsive SafeArea, custom mobile controls, and **Haptic Feedback**.
- **Offline-First Architecture:** Resilient gameplay with API timeouts and guest fallbacks for network fluctuations.
- **Detailed Telemetry:** Integrated activity tracking (Start, Success, Failure, Abandoned) for gameplay analytics.

---

## 🎮 Gameplay Mechanics

- **Hearts:** Protect your lives! Hitting obstacles results in losing hearts.
- **🐟 Fish:** Your primary source of **XP** and health. Collect them to climb the leaderboard!
- **🧪 Potions:** Transform into the **"Big Cat"**! Gain temporary invincibility, increased jump height, and a speed boost.
- **🏠 Home Exit:** Complete Levels 1-4 by reaching the "Home" gate to safely transition to the next stage.
- **Portal System:** Reach the final portal in Level 5 to secure your victory and submit your high score.

---

## 🕹️ How to Play

### Controls
| Action | PC/Mac Controls | Mobile Controls |
| :--- | :--- | :--- |
| **Move Left/Right** | `A / D` or `Left/Right Arrow` | On-screen Joystick / Buttons |
| **Jump / Double Jump** | `Space` or `W` or `Up Arrow` | Jump Button |
| **Pause Game** | `Escape` or `P` | Pause Icon |
| **Retry / Submit** | `Space` (on Game Over) | UI Buttons |

---

## 🛠️ Technical Stack

### Frontend (Unity Engine)
- **Engine:** Unity 6000.3.8f1+ (URP)
- **Language:** C# (Object-Oriented Architecture)
- **Networking:** `UnityWebRequest` for secure REST API communication.
- **Input:** New Unity Input System for cross-platform compatibility.

### Backend (Node.js API)
- **Runtime:** Node.js / Express.js
- **Database:** **MongoDB** (via Mongoose) for persistent player profiles.
- **Security:** **Firebase Admin SDK** for verifying Google ID tokens.
- **Logging:** Custom middleware for real-time request monitoring.

---

## 📦 Setup & Installation

### 1. Backend Setup
1.  Navigate to the `backend` directory.
2.  Install dependencies:
    ```bash
    npm install
    ```
3.  Configure `.env` with your `MONGODB_URI` and `FIREBASE_SERVICE_ACCOUNT_PATH`.
4.  Start the server:
    ```bash
    node server.js
    ```

### 2. Unity Project
1.  Open the project in **Unity Hub** (Version 6000.3.8f1+).
2.  Ensure the `EditorBaseUrl` in the `LeaderboardApiService` is set to your server URL (Default: `http://localhost:5001`).
3.  For Android builds, update the `AndroidBaseUrl` with your machine's local IP address.
4.  Press **Play** and enjoy!

---

## 📜 License
This project was developed for personal growth and educational purposes. All rights reserved.

![Cats Escape Cover](./cats_escape_cover_1777473681622.png)
