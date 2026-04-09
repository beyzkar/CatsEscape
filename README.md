# CatsEscape

**CatsEscape** is a stylized, fast-paced 2D side-scrolling runner built with Unity. Take control of a nimble cat navigating through various environments, avoiding obstacles, jumping over pitfalls, and collecting power-ups across 5 unique levels.

##  How to Play

### Controls
*   **Move Left/Right:** `A / D` or `Left / Right Arrow`
*   **Jump / Double Jump:** `Space` or `Up Arrow`
*   **Mobile Support:** On-screen buttons for movement and jumping.

### Objective
Navigate through the levels by dodging hurdles, obstacles, and enemies. Your progress is measured by your cumulative distance and "Obstacles Passed." Reach the goal for each level to unlock the next environment!

---

##  Features

-   **5 Thematic Levels:** Each level features unique background art, varying speeds, and specialized obstacles.
-   **Character Selection:** Multiple cat skins with beautiful animations (Idle, Walk, Jump).
-   **Advanced Physics:** Custom "Better Jump" mechanics for responsive, snappy control. 
-   **Power-Ups:**
    -   **Fish:** Collect to restore hearts and gain XP.
    -   **Potions:** Temporarily turns the cat "Big," granting speed boosts and higher jumps.
-   **Dynamic World:** Parallax scrolling backgrounds and a procedural obstacle spawning system.
-   **Global Speed System:** The game speed scales with level progression and power-up states.
-   **Leaderboard:** Track your High Scores and XP across sessions.

---

##  Tech Stack

*   **Engine:** Unity 2022.x+
*   **Language:** C#
*   **Physics:** Rigidbody2D (Continuous collision detection)
*   **Rendering:** URP / Sprite-based 2D visuals
*   **Animation:** Animator with blending states (Walking, Idle, Airborne)
*   **Audio:** Custom AudioManager for synchronized SFX and Background Music.
*   **UI:** TextMeshPro for optimized, beautiful on-screen text and leaderboards.

---

##  Setup & Installation

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/beyzkar/CatsEscape.git
    ```
2.  **Open in Unity:**
    *   Open Unity Hub.
    *   Add the `CatsEscape` folder.
    *   Ensure you have the required Sprite and Audio assets in the `Assets/` directory.
3.  **Build Settings:**
    *   Target Platform: PC/Mac Standalone or WebGL.
    *   Include `Main Menu`, `Character Selection`, and `Game` scenes in the build.

---


