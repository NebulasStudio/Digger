# 🤖 PROMPT MASTER ULTRA-DETTAGLIATO PER CODEX (UNITY BUILDER AGENT)

> **Copia questo file o incolla il messaggio qui sotto direttamente nella chat di Codex.**

---

### 💬 MESSAGGIO / PROMPT COMPLETO DA INCOLLARE A CODEX:

```markdown
Sei CODEX, l'agente Unity Builder specializzato per Sandsunder (Repo: NebulasStudio/Digger, branch main, progetto Unity in Game/). Hai accesso alla CLI ed ai comandi nativi MCP di Unity (Unity_RunCommand, AssetDatabase, SceneManager).

IL TUO OBIETTIVO PRIMARIO È AVVIARE IL PROGETTO IN PLAN MODE, LEGGERE IL DOCUMENTO MASTER E RIVOLUZIONARE L'ATTUALE SISTEMA DI ANIMAZIONI ED INTERFACCIA UTENTE PER COSTRUIRE UNA SANDBOX 100% FUNZIONANTE E PRIVA DI BUG.

### 📚 DOCUMENTI DI RIFERIMENTO OBLIGATORI DA LEGGERE SUBITO:
1. `Game/Assets/Sandsunder/Art/CODEX_UNITY_MASTER_HANDOFF.md` (Documento Master Ultra-Dettagliato con audit commit git, sitemap, catalogo inventario asset, database di bilanciamento e guida animazioni)
2. `AGENTS.md` (Regole generali del workspace Sandsunder)
3. `Game/Assets/Sandsunder/Art/HIGGSFIELD_INSTRUCTION_PROTOCOL.md` (Protocollo di uso di Higgsfield AI)

---

### 📋 SINTESI DEL TASK & DIRECTIVE OPERATIVE:

1. **PROCESSO DI PLAN MODE & CHECK INIZIALE**:
   - Avvia l'ispezione in **Plan Mode**.
   - Analizza la cartella `Game/Assets/Sandsunder/Art/` e `Game/Assets/Sandsunder/Editor/`.
   - Esegui `dotnet test Game.sln` per verificare che non ci siano errori di compilazione C#.
   - Utilizza `Unity_RunCommand` per verificare che Unity Editor sia attivo e sincronizzato.

2. **RIDUZIONE SCOPE SANDBOX MVP (OBIETTIVO STABILE E PERFETTO)**:
   Per garantire la massima qualità visiva senza bug, riduciamo l'MVP Sandbox a pochi elementi perfetti:
   - **Personaggio Principale (Nomad)**: Utilizza lo sprite autoritativo `nomad_32.png` (giacca blu `#3466B8`, cappuccio bianco, sciarpa turchese `#26B8C6`). Nessun ritaglio griglia difettoso. Gestisci le animazioni (Idle, Walk, Run, Dig, Stealth Crouch, Roll, Hurt, Death) assicurandoti che lo sprite rimanga solido e proporzionato. Se le griglie esterne di Higgsfield creano pezzi di mantello volante, guida l'utente passo-passo per creare/aggiustare le animazioni a mano con la finestra Animation di Unity (`Window > Animation > Animation`) o tramite script con rect pixel trasparenti.
   - **1 Mob Ostile (Spitter Tartaruga di Cristallo)**: Utilizza `spitter_32.png` ed anima in loop continuo `Spitter_Idle.anim` con passaggio a `Spitter_Charge.anim` in attacco e `Spitter_DeathBurst.anim` alla morte.
   - **1 Pala (Starter Shovel)**: Animazione di scavo fossa + fendente melee.
   - **1 Arma Melee (Scimitarra del Deserto)**: Fendente ad arco 90°.
   - **1 Arma Ranged (Fucile di Ottone)**: Ancorato alle mani del Nomad (`X = ±0.08m`, `Y = 0.05m`), rotazione 360° che segue il mouse, sparo rettilineo con casing e muzzle FX.

3. **RIFACIMENTO INTERFACCIA UTENTE & HUD (STILE DUNGEON TEMPLE DESERTO)**:
   - Ridisegna la UI in stile brochure tempio rovine deserto in pixel art con glassmorphism (`ui_glass_panel.png`).
   - Modalità **TAB Inventario** stile Minecraft con anteprima 3D/paper-doll del Nomad (`nomad_32.png`), griglia zaino e card delle statistiche arma.
   - **Barra della Salute** (Rosso/Oro).
   - **Indicatore Livello O2 (Ossigeno)**: Barra ciano sotterranea che si attiva quando il Nomad scava a Livello -1 e si consuma gradualmente in 100 secondi.

4. **FISICA, OMBRE E TILES DI SCAVO**:
   - Correggi l'ombra a goccia (`BlobShadow`) ancorandola ai piedi del personaggio (`Y = -0.15m`) per evitare che fluttui.
   - Rimuovi qualsiasi sovrascrittura di casse durante lo scavo: lo scavo della sabbia mostra le fosse di scavo (`DigIntact`, `DigCracked`, `DigOpened`) e passa al layer sotterraneo Livello -1.

Leggiti il documento `Game/Assets/Sandsunder/Art/CODEX_UNITY_MASTER_HANDOFF.md` per tutti i dettagli tecnici e procedi in Plan Mode!
```
