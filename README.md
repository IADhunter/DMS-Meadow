# DressMySlugcatMeadowCompat

A **BepInEx code mod** for *Rain World* that resolves the skin-cloning issue between **Dress My Slugcat (DMS)** and **Rain Meadow** by implementing a client-side profile detour.


## Technical Overview

The core problem stems from how `DressMySlugcat.Customization.For(Player, bool)` handles player indices in an online environment. Rain Meadow introduces custom network clones that confuse the native `PlayerNumber` allocation, forcing DMS to fallback to local player states (cloning Player 1's skin onto everyone else) or rendering them incorrectly.

This mod injects a runtime detour via **MonoMod.RuntimeDetour** to hijack `Customization.For()`. Instead of tampering with network entity parameters or rewriting DMS packet replication, it intercepts the client-side rendering pipeline in real-time.

### 🛠️ Execution Flow
1. **Hook Injection:** Intercepts `DressMySlugcat.Customization.For(Player, bool)` using a static `MonoMod` Hook.
2. **Entity Validation:** Queries `RainMeadow.OnlinePhysicalObject.map` using the player's `abstractCreature` reference to verify if the entity belongs to a remote network client (`!owner.isMe`).
3. **Database Extraction:** Retrieves the remote user's Steam ID (`owner.id.ToString()`) and cross-references it with a local dictionary populated via a parsing cycle of `dms_meadow_skins.txt`.
4. **Profile Redirection:** If a match occurs, the hook queries `DressMySlugcat.SaveManager.Customizations` using the configured slot. It sets up an isolated data structure by copying the visual metadata (`.Copy()`) and merging native tail physics and backup sprites from `SpriteDefinitions.GetSlugcatDefault()`.
5. **Early Return & Fallback:** Forces `PlayerNumber = 0` on the returning instance to bypass local gamepad polling, preventing `NullReferenceException` crashes during network realization. Unmatched clients are safely delegated back to the original method (`orig`).

## ⚙️ Compilation Notes
Target framework: **.NET Framework 4.8**
Dependencies required for compilation:
* `Assembly-CSharp.dll`
* `BepInEx.dll`
* `MonoMod.RuntimeDetour.dll`
* `MonoMod.Utils.dll`
* `DressMySlugcat.dll`
* `RainMeadow.dll`
