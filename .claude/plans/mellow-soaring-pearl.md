# Hot Potato Gamemode

## Context
New gamemode where all players spawn in identical cars (Dominator muscle car). One random player is "it" (holds the explosive potato) with a 30-second timer. They must physically crash into another player's car to pass the potato. When the timer expires, the "it" player's car explodes and they're permanently eliminated. A new random survivor then gets the potato. Last player standing wins.

## Game Flow
1. All players spawn in identical "dominator" muscle cars
2. 3-2-1 countdown (players frozen)
3. After GO: random player becomes "it" with 30s timer
4. "It" player crashes into another car → potato transfers, fresh 30s timer
5. 2-second cooldown after receiving potato (prevents ping-pong)
6. Timer expires → car explodes, player eliminated (spectator)
7. Server picks new random "it" from survivors
8. Last player standing wins

## Visual Indicators
- **If you're "it"**: green chevron markers (DrawMarker type 2) above ALL other players' heads (targets to crash into)
- **If you're safe**: red chevron marker above the "it" player's head (danger to avoid)
- **Timer display**:
  - "It" player: large pulsing countdown number (top center), goes red & pulses when < 5s
  - Safe players: smaller "POTATO: Xs" text (top center)
- **Role reveal**: NUI overlay - "HOT POTATO!" in red for "it", "Safe" in green for safe

## Collision Transfer Mechanic
- Client-side: `Vehicle.HasCollided` + `MaterialCollidingWith == "CarMetal"` (same pattern as MVB)
- Find closest other player's ped within 8m of car position
- Send `salty:netHPPass` server event with target's server ID
- Server validates (is "it" actually set? is target alive/safe?) and broadcasts team changes
- 2-second cooldown after receiving potato to prevent instant pass-backs

## Files to Create

### 1. `HPClient/Main.cs`
```csharp
namespace HPClient {
    public enum Teams { Safe, It }

    public class Main : BaseGamemode {
        Vehicle Car;
        float potatoEndTime = 0;
        float passCooldown = 0;
        bool pendingPotatoStart = false;
        int itPlayerServerId = -1;
    }
}
```

**Constructor:**
- `base("HP")`, create HUD, Random
- Register event: `salty::HPRoundResult` → `OnRoundResult` (same pattern as ICM)
- `GamemodeRegistry.Register("hp", "Hot Potato", ...)` with color `#ff4444`
- Tags: "Vehicle", "Elimination"; Teams: "Safe", "It"; Features: "Vehicles", "Timer", "Last Man Standing"
- Register `/spawnbot`, `/clearbots` commands (same bot pattern as ICM for solo testing)
- Register debug menu entries (Teams: Set Safe / Set It, Self: Respawn)

**`Start(float gameTime)`:**
- `base.Start(gameTime)`
- `await RunCountdown()` (freeze players during 3-2-1)
- After countdown: if `pendingPotatoStart`, start the 30s timer

**`Update()`:**
- `CantExitVehichles()`
- Force player back into car if ejected
- If `pendingPotatoStart && !CountdownActive`: set `potatoEndTime = GetGameTimer() + 30000`, clear flag
- **Collision detection** (only if team == It && passCooldown expired):
  - `Car.HasCollided` + `MaterialCollidingWith` is "CarMetal" or "Unk"
  - Loop `PlayerList`, find closest ped within 8m
  - `TriggerServerEvent("salty:netHPPass", closest.ServerId)`
  - Set `passCooldown = GetGameTimer() + 2000`
- **Timer check**: if team == It && `GetGameTimer() > potatoEndTime` → `ExplodePotato()`
- `DrawMarkers()` — arrows above relevant players
- `DrawPotatoTimer()` — prominent countdown text
- `base.Update()`

**`SetTeam(int team)`:**
- `base.SetTeam(team)`
- If It: role reveal "HOT POTATO!" red, goal "CRASH into someone!", start timer (or set `pendingPotatoStart` if countdown active)
- If Safe: role reveal "Safe" green, goal "Avoid the hot potato!", clear timer

**`OnDetailUpdate(int ply, string key, object oldValue, object newValue)`:**
- When `key == "team"` and `newValue == It` and `ply != LocalPlayer.ServerId`:
  - Track `itPlayerServerId = ply`
  - Estimate timer: `potatoEndTime = GetGameTimer() + 30000`

**`DrawMarkers()`:**
- Loop all players, get their team from PlayerDetails
- If I'm "it": draw green chevrons (50, 200, 50, 120) above all Safe players at Z+2.5
- If I'm safe: draw red chevron (255, 50, 50, 150) above the It player at Z+2.5

**`DrawPotatoTimer()`:**
- Calculate `remaining = (potatoEndTime - GetGameTimer()) / 1000f`
- If I'm "it": large font (font 7, scale ~1.0), red, pulsing when < 5s, center-top
- If I'm safe: smaller font (font 4, scale ~0.5), show "POTATO: Xs", center-top

**`ExplodePotato()`:**
- Set `potatoEndTime = 0`
- `Car.IsInvincible = false; Car.IsExplosionProof = false; Car.ExplodeNetworked()`
- `LocalPlayer.Character.Kill()`

**`SpawnCar()` async:**
- Delete old car, position player at LastSpawn
- `await World.CreateVehicle("dominator", pos, heading)`
- Set invincible, fireproof, no tire burst, no wheel break, no engine degrade
- Put player in driver seat

**`PlayerSpawn()`:**
- `base.PlayerSpawn()`, `SpawnCar()`

**`End()`:**
- Delete car, `base.End()`

### 2. `HPClient/Properties/AssemblyInfo.cs`
- Standard assembly info (copy MVBClient pattern, change names to HPClient)

### 3. `HPClient/HPClient.csproj`
- Copy MVBClient.csproj pattern
- RootNamespace: `HPClient`, AssemblyName: `HPClient.net`
- New unique ProjectGuid
- References: CitizenFX.Core (client), GamemodeCityClient, GamemodeCityShared

### 4. `HPServer/Main.cs`
```csharp
namespace HPServer {
    public enum Teams { Safe, It }

    public class Main : BaseGamemode {
        Player ItPlayer;
        float PotatoEndTime = 0;
    }
}
```

**Constructor:**
- `base("HP")`
- Settings: GameLength = 5 min, Name = "Hot Potato", Rounds = 1, PreGameTime = 15s
- Register event: `salty:netHPPass` → `OnPotatoPass(int targetServerId)`

**`Start()`:**
- `base.Start()` (broadcasts game start to clients)
- Assign all players as Safe, spawn everyone
- Pick random player → `AssignPotato(player)`

**`AssignPotato(Player player)`:**
- If old ItPlayer exists, `SetTeam(ItPlayer, Safe)`
- Set `ItPlayer = player`
- `SetTeam(player, It)`
- `PotatoEndTime = GetGameTimer() + 30000`
- `WriteChat` announcement

**`OnPotatoPass(int targetServerId)`:**
- Validate `ItPlayer != null`
- Find target player in PlayerList by Handle
- Validate target is team Safe
- `WriteChat("Hot Potato", ItPlayer.Name + " passed the potato to " + target.Name + "!", ...)`
- `AssignPotato(target)`

**`OnPlayerDied(Player victim, int killerType, Vector3 deathCoords)`:**
- Get victim's team before elimination
- `SetTeam(victim, -1)` (spectator)
- If victim was It: clear ItPlayer
- Count alive: `safePlayers.Count + (ItPlayer != null ? 1 : 0)`
- If alive <= 1: find winner, broadcast `salty::HPRoundResult`, `End()`
- If victim was It and alive >= 2: pick random from safe players → `AssignPotato(newIt)`
- `base.OnPlayerDied()`

**`OnTimerEnd()`:**
- Overall game timer expired → "Time's up!" → draw → `base.OnTimerEnd()`

### 5. `HPServer/Properties/AssemblyInfo.cs`
- Standard assembly info (copy MVBServer pattern, change names to HPServer)

### 6. `HPServer/HPServer.csproj`
- Copy MVBServer.csproj pattern
- RootNamespace: `HPServer`, AssemblyName: `HPServer.net`
- New unique ProjectGuid
- References: CitizenFX.Core (server), GamemodeCityServer, GamemodeCityShared

## Files to Modify

### 7. `GamemodeCity.sln`
- Add HPClient and HPServer project entries (new GUIDs, Debug+Release configs)

### 8. `fxmanifest.lua`
- Add `'HPClient.net.dll'` to client_scripts
- Add `'HPServer.net.dll'` to server_scripts

### 9. `GamemodeCityClient/Main.cs`
- Add `/hp` command registration (same pattern as `/icm`, `/mvb`):
  ```csharp
  RegisterCommand( "hp", new Action<int, List<object>, string>( ( source, args, raw ) => {
      TriggerServerEvent( "salty:netStartGame", "hp" );
  } ), false );
  ```

## Key Patterns Reused
- **Vehicle spawning**: MVBClient `SpawnTruck()`/`SpawnBike()` pattern (`World.CreateVehicle` + invincible settings)
- **Collision detection**: MVBClient `Update()` — `Vehicle.HasCollided` + `MaterialCollidingWith` check
- **Vehicle explosion**: MVBClient `ExplodeVehicle()` — disable invincible, `ExplodeNetworked()`, kill player
- **Player markers**: MVBClient `Update()` — `DrawMarker(2, ...)` above other players' heads via PlayerList loop
- **Team management**: Server `SetTeam()` which calls both `salty:SetTeam` (target player) and broadcasts via PlayerDetails
- **Role reveal**: `HubNUI.ShowRoleReveal()` for NUI overlay
- **Round result**: `HubNUI.ShowRoundEnd()` / `salty::HPRoundResult` event pattern
- **Countdown**: `RunCountdown()` from BaseGamemode (3-2-1-GO with freeze)
- **GamemodeRegistry**: Same registration pattern as ICM/MVB
- **Force into vehicle**: `CantExitVehichles()` + auto-SetIntoVehicle in Update

## Verification
1. Build solution — HPClient.net.dll and HPServer.net.dll produced
2. `/hp` → game starts, all players in dominators, countdown runs
3. One player gets "HOT POTATO!" reveal, 30s timer starts after GO
4. "It" player sees green arrows on all others; safe players see red arrow on "it"
5. "It" crashes into another car → potato transfers with chat message, fresh 30s timer
6. Timer expires → car explodes, player eliminated to spectator
7. New random player gets potato from remaining survivors
8. Last player standing → victory screen, round ends
