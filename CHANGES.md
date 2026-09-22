# Changes: UI, Turn Timer, Underdog Buff, Treasure

Branch: `person2-combat-rules` (not committed yet)

This document covers the features added on top of commit `afdd3fb`
("Fix elimination bug, add piece types, add MatchWinManager").

---

## Summary

| # | Feature | What the player sees |
|---|---------|----------------------|
| 1 | Score dashboard | A small bar at the top of the screen with each team's score and a colored bar under the team whose turn it is |
| 2 | Win screen | "Team Blue won !!!!!" / "Team Red won !!!!!" when a team is wiped out |
| 3 | Turn timer (6 s) | A countdown pie in the dashboard. If time runs out, the turn passes to the other team |
| 4 | Underdog buff | When a piece dies, its teammates get +1 ATK. The last piece left has its ATK doubled |
| 5 | Healing treasure | A gold "+" circle sometimes appears. Passing over it heals your team |
| 6 | Stat change popups | "HP -2" / "ATK +3" floats above a piece before its numbers update |
| 7 | Lighter background | Arena background changed from 50% grey to 72% grey |

---

## 1. Score dashboard

**File:** `Assets/Scripts/ScoreDashboard.cs` (new; added to the **Canvas** in the scene)

- Replaces the old `TurnText` ("BLUE TURN") and the `BLUE 0 : 0 RED` text in the middle of the board. Both
  objects are turned off in the scene, not deleted.
- Layout: `BLUE  0  (timer)  0  RED`.
- The team whose turn it is has a colored bar under its half and a colored name. The other team's name is
  greyed out.
- The dashboard is built in code when the game starts, so there is no prefab to maintain.

**Inspector settings:** panel size, top margin, font sizes, team colors, panel color.

## 2. Win screen

**File:** `Assets/Scripts/ScoreDashboard.cs`

- Listens for `MatchWinManager.MatchOver`. It hides the dashboard, dims the screen and shows
  **"Team Blue won !!!!!"** or **"Team Red won !!!!!"** in the team's color.
- Listens for `MatchWinManager.MatchReset`. If `RestartMatch()` is called, the win screen is hidden and the
  dashboard comes back.

## 3. Turn timer

**Files:** `Assets/Scripts/TurnManager.cs`, `Assets/Scripts/ScoreDashboard.cs`

- Each team has **6 seconds** to launch a piece. If it doesn't, the turn goes to the other team.
- The clock **only runs while the team is aiming**. It pauses as soon as a piece is launched and while pieces
  are rolling.
- Every new turn, including an extra action from the ExtraActionOnKill ability, starts with a full clock.
- If the player is dragging when time runs out, the drag is cancelled.
- The timer stops when the match is over and resets on restart.
- **Display:** a small pie in the center of the dashboard. It shrinks as time runs out, is drawn in the
  current team's color, turns yellow for the last 2 seconds, and is hidden while a shot is rolling.

**Inspector settings:**
- TurnManager → **Turn Time Limit** (default 6; set 0 to turn the timer off)
- Canvas → **Timer Pie Size**, **Low Time Warning**

## 4. Underdog buff

**File:** `Assets/Scripts/UnderdogBuffManager.cs` (new; added to the **MatchWinManager** object in the scene)

Rules:
1. When a piece dies, every surviving teammate gets **+1 ATK**.
2. When a team has only **one piece left**, that piece's ATK is **doubled**.

Example: a Blue Striker starts with ATK 2 on a team of three.

| Event | Striker's ATK |
|-------|---------------|
| Start | 2 |
| First teammate dies | 3 |
| Second teammate dies (+1, then doubled as the last piece left) | (3 + 1) × 2 = 8 |

- Applies to both teams in the same way.
- Uses the same `PieceStats.AddAttackPower` as the ally attack boost, so restarting the match clears it.

**Inspector settings:** **Attack Per Fallen Teammate** (1), **Double Last Survivor** (on/off).

## 5. Healing treasure

**Files (all new):**
- `Assets/Scripts/TreasureManager.cs`: spawning, turn countdown and healing. It is on a new **TreasureManager**
  object in the scene.
- `Assets/Scripts/Treasure.cs`: the treasure object. Pieces pass through it and it detects when one touches it.

How it works:
1. At the start of each turn there is a **30% chance** a treasure appears (only one at a time).
2. It is placed randomly, but never on top of or close to a piece or a wall.
3. It stays for **2 turns**. It pulses, and it **blinks** on its last turn.
4. Pieces pass **through** it. The first living piece to touch it collects it for **that piece's team**:
   the launched piece, a teammate it knocked, or an enemy knocked into it.
5. Reward: every living piece on that team heals **+1 HP**, up to its starting HP.
6. The treasure is removed when the match ends or restarts. It can appear many times in a match.

**Inspector settings:** Spawn Chance, Turns To Stay, Heal Amount, Edge Margin, Clear Radius,
Treasure Radius, Treasure Color.

> Tip: set **Spawn Chance** to 1 while testing so a treasure appears right away.

## 6. Stat change popups

**Files:** `Assets/Scripts/Combat/PieceStatsDisplay.cs`, `Assets/Scripts/FloatingText.cs` (new)

- When a piece's HP or ATK changes, a popup shows the change first, for example **"HP -2"** or **"ATK +3"**.
  The numbers on the piece update after **0.8 s**.
- Colors: HP loss = red, HP gain = green, ATK gain = gold, ATK loss = grey.
- Several changes in the same frame are combined into one popup. For example, the underdog +1 and the
  doubling show as a single "ATK +N".
- If HP and ATK both change, the two popups are stacked so they don't overlap.
- Covers every source: hits, counter damage, ally heal/boost, treasure, underdog buff.
- On match restart the numbers reset without popups.

**Inspector settings (on each piece):** Value Update Delay, Popup Height, Popup Spacing, the four popup colors.

## 7. Lighter background

**File:** `Assets/Scenes/PhysicsTest.unity`

- `Arena → background` sprite color changed from `0.5` grey to `0.72` grey.

---

## Supporting code changes

Small additions to existing scripts so the new features can react to game events:

| Script | Addition | Used by |
|--------|----------|---------|
| `MatchScoreManager.cs` | `event Action ScoreChanged` | ScoreDashboard |
| `TurnManager.cs` | `event Action<PieceTeamSide> TurnChanged`, turn timer (`TurnTimeLimit`, `TimeRemaining`, `IsTimerRunning`) | ScoreDashboard, TreasureManager |
| `PieceStats.cs` | `event Action StatsReset` (fired on match restart, before `StatsChanged`) | PieceStatsDisplay |

## Scene changes (`PhysicsTest.unity`)

- **Canvas:** added `ScoreDashboard` component.
- **TurnText** and **ScoreText:** turned off (replaced by the dashboard).
- **MatchWinManager** object: added `UnderdogBuffManager` component.
- New **TreasureManager** object.
- **background:** lighter grey.

## New files

```
Assets/Scripts/ScoreDashboard.cs
Assets/Scripts/UnderdogBuffManager.cs
Assets/Scripts/TreasureManager.cs
Assets/Scripts/Treasure.cs
Assets/Scripts/FloatingText.cs
(+ matching .meta files)
```

---

## Known issues / open items

- **Treasure fairness:** the treasure appears at the start of a turn, so the team whose turn it is always gets
  the first chance. Possible fixes (not decided yet): first chance to the losing team, alternate Blue/Red, or
  "locked" on the turn it appears.
- **Restart button:** `MatchWinManager.RestartMatch()` resets pieces, turns, the timer, the treasure and the win
  screen, but no button is wired to it yet (Person 3's UI).
- **Kill score on restart:** `MatchScoreManager` scores are not reset by `RestartMatch()` (unchanged existing
  behavior).
- **Light background contrast:** the gold treasure and the green/gold popups stand out less on the lighter grey.
  Consider darker colors or a text outline if they are hard to read.
