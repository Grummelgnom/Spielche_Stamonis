2-Spieler Top-Down Bullethell

**Projekt im Rahmen des Moduls PRG**  
**Unity-Version:** 6000.0.62f1  
**Netzwerk-Framework:** FishNet  
**Abgabe:** 23.01.2026  
**Team:** Paula Staab / Dennis Simonis

## Projektziel

Entwicklung eines **spielbaren 2-Spieler-Arcade-Bullethell-Spiels** aus der Top-Down-Perspektive.  
Das Spiel muss **online im Multiplayer (Host + Client)** funktionieren und alle unten aufgeführten **Pflichtanforderungen** erfüllen.
_____________________________________________________________________________
#### Wichtig !!!! #####

Wir haben auf den letzten Drücker den Wave-Text verloren, und konnten es nicht mehr fixen.
Das Feature ist jetzt nicht in Main, aber es kann in Simonis5 begutachtet werden.


Zum Spielstart auf Start Game drücken, nicht vorher den Server starten!
Danach den Clienten. Client 2 muckt rum. Hier muss 3 mal auf client start/stopt/start gedrückt werden,
damit das Namensfeld und der Ready Button erscheint.
Auch "relativ" wichtig - den Screen nicht zu klein zu haben ... sonst zeigt der zweite Client
das Namensfeld nicht im Screen.
_____________________________________________________________________________


## Technische Umsetzung & Erfüllung der Pflichtanforderungen (80 Punkte)

### 1. Multiplayer-Basis (FishNet) – 10 Punkte

- FishNet ist korrekt im Unity-Projekt eingerichtet (Package via Git-URL importiert)
- Host/Client-Verbindung ist möglich (über UI-Buttons: Host erstellen / Client beitreten)
- Zwei Instanzen können sich stabil verbinden (getestet im Editor-Multiplayer-View und in Builds)
- NetworkManager ist korrekt konfiguriert (kein Auto-Start, manuelle Initialisierung, Transport & Settings angepasst)

### 2. Spielersteuerung & Synchronisation – 15 Punkte

- Top-Down-Bewegung implementiert (WASD / Pfeiltasten, Input System)
  Einzel- und Spread-Schuss, Bullets als NetworkObjects, server-seitige Kollisionsprüfung
  Linke Maustaste = Einzelschuss
  Rechte Maustaste = Spread-Schuss
  Linke Maustaste gedrückt halten = Dauerschuss, nur wenn Balken aufgeladen.
  WASD Hoch/Runter/Links/Rechts
  Farbänderung am Anfang, vor Ready State mit Taste "C"
- Spieler sind als `NetworkObject` mit `NetworkBehaviour` umgesetzt
- Ownership ist korrekt gesetzt → nur der eigene Spieler ist steuerbar (`IsOwner`-Prüfung)
- Bewegung wird **server-authoritativ** synchronisiert (ServerRpc für Input → Position-Update auf Server)
- Mindestens eine SyncVar implementiert:  
  - `lives` (SyncVar<int>) – Leben  
  - `score` (SyncVar<int>) – Punkte  
  - `hasShield` (SyncVar<bool>) – Schild-Status  
  - `playerColor` (SyncVar<Color>) – Spielerfarbe Taste "C"

### Weitere umgesetzte Pflicht-Features

- **Schießen & Bullethell-Mechaniken**  
  Einzel- und Spread-Schuss, Bullets als NetworkObjects, server-seitige Kollisionsprüfung
  
    
- **Feinde & Waves**  
  Mehrere Feind-Typen, Wave-Spawning, synchronisierte Bewegung & Schießen

- **Power-Ups**  
  Schild-Power-Up (fällt herunter, aktiviert temporären Schutz via SyncVar)

- **Highscore-System**  
  Submit und Anzeige der Top-10 über PHP-API (UnityWebRequest)
  Erstelle einen Ordner bullethell_api in htdocs
  Erzeuge Datenbank mit SQL Datei aus Server-Ordner in Unity-Projekt
  Kopiere die 3 PHP Dateien aus dem Server-Ordner in den bullethell_api Ordner
  Starte Apache Server und MySQL Server

- **Sound & visuelle Effekte**  
  Prozedurale Sounds (SimpleSoundManager), Kamera-Shake bei Treffern

- **Szenenmanagement**  
  Netzwerkweiter Szenenwechsel über FishNetSceneLoader (z. B. zurück ins Menü)

Bekannte Einschränkungen / Nicht umgesetzt

Kein Spectator-Modus
Kein Reconnect nach Disconnect
Highscore-API muss lokal laufen (localhost/bullethell_api)
Keine Controller-Optimierung (nur Tastatur)


Optionaler Punkt                               | Integriert? | Wo / Wie?
-----------------------------------------------|-------------|---------------------------------------------------------------
Sound- & Musik-Manager                         | Ja          | SimpleSoundManager (prozedurale Töne, Singleton, DontDestroyOnLoad, verschiedene Sounds für Laser, Explosion, Power-Up, Ultimate-Loop)
Visuelle Effekte (Kamera-Shake)                | Ja          | CameraShake Singleton + ObserversRpc bei Explosion/Tod/Treffer
Highscore-System mit externer API              | Ja          | HighscoreClient (Submit & Fetch via UnityWebRequest) + HighscoreDisplay (UI-Panel mit Top-10-Liste)
Persistent Highscores (Datenbank)              | Ja          | Über PHP-Backend + MySQL (submit_score.php / get_highscores.php)
UI-Elemente für Multiplayer-Setup              | Ja          | NetworkHudCanvas mit Name-Input, Ready-Button, Player1/Player2-Namen (SyncVars)
Netzwerkweiter Szenenwechsel                   | Ja          | FishnetSceneLoader mit ServerRpc und globalem Load (z. B. zurück ins Menü)
Power-Ups & Ultimate-Mechanik                  | Ja          | Schild-Power-Up (SyncVar hasShield), Ultimate-Meter (SyncVar ultimateMeter), Drain-Rate, Blink-Effekt
Controller-Support (grundlegend)               | Teilweise   | Input System wird verwendet → Controller ist kompatibel (WASD + analoge Sticks möglich, keine vollständige Optimierung)