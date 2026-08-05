---
name: unity-gameplay-mastery
description: Guida completa all'ingegnerizzazione di gameplay 2D Top-Down in Unity, compresi Unity MCP, fisica Rigidbody2D, Animator Controllers, effetti particellari del terreno, soppressione orme stealth e rendering di silhouette sotterranee.
---

# Unity Gameplay Mastery & MCP Skill

Questa skill fornisce le linee guida definitive per costruire gameplay 2D Top-Down performanti ed eleganti utilizzando Unity ed il server MCP (`unity-mcp`).

## 1. Integrazione Unity MCP (`unity-mcp`)
- **Console & Diagnostic:** Usa `Unity_GetConsoleLogs` per estrarre in tempo reale eventuali avvisi ed errori di compilazione/runtime dalla finestra Console.
- **Scene & Camera View:** Usa `Unity_SceneView_Capture2DScene` e `Unity_SceneView_CaptureMultiAngleSceneView` per catturare lo stato visivo 2D della scena ed ispezionare il piazzamento degli oggetti.
- **Editor Commands:** Esegui comandi `MenuItem` personalizzati via `Unity_RunCommand` (ad es. `Sandsunder/Gameplay/Build Gameplay Lab`).

## 2. Architettura Fisica 2D Rigidbody Top-Down
- **Configurazione Rigidbody2D:**
  - `bodyType = RigidbodyType2D.Dynamic`
  - `gravityScale = 0f`
  - `freezeRotation = true`
  - `collisionDetectionMode = CollisionDetectionMode2D.Continuous`
- **Locomozione & Sliding:**
  - Calcola sempre il vettore di movimento normalizzato: `moveInput.normalized`.
  - Applica la velocità diretta in `FixedUpdate`: `rb.linearVelocity = moveInput.normalized * moveSpeed`.
  - Questo previene scatti o compenetrazioni e permette al personaggio di scivolare (sliding) lungo le pareti in modo naturale.

## 3. Gestione Terreni, Orme & Stealth
- **Soppressione Orme Stealth:**
  - Quando il giocatore attiva la modalità stealth (tasto `Shift`), la routine di spawn delle orme (`SandboxFootprint.cs`) deve sopprimere immediatamente il rilascio delle decals per nascondere la traccia del cammino.
- **Animazione Sabbia Dinamica:**
  - Lo scavo della sabbia deve generare onde di particelle dinamiche (`ParticleSystem`) con l'effetto avvallamento in tempo reale, anziché sovrapporre semplici immagini statiche.

## 4. Silhouette Sotterranee & Trasparenza 2D
- **Crawl Sotterraneo:**
  - Quando il personaggio entra nei tunnel o cammina in profondità (livello -1 / -2), l'altezza dello sprite deve ridursi al 50% (`localScale.y = 0.5f`) ed assumere il colore **silhouette ciano in trasparenza** (`Color(0.20f, 0.90f, 0.95f, 0.65f)`), mostrando chiaramente la presenza del personaggio sottotraccia rispetto al terreno di superficie.
