# SANDSUNDER — PROMPT: OGNI PERSONAGGIO CON LE SUE ANIMAZIONI (fix mapping controller)

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS
> ⚠️ BUG CONFERMATO: il `NomadAnimatorController.controller` ha lo stato **Idle che punta a `Shovel_Idle`** (la clip della PALA) invece che a `Nomad_Idle`. Questo è il motivo per cui le animazioni appaiono "a caso su un altro personaggio". Leggi AGENTS.md.

## REGOLA FONDAMENTALE
**Ogni personaggio/mob DEVE avere SOLO le proprie animazioni.** Proibito applicare a un personaggio una clip appartenente a un altro. Il mapping è 1:1 e fisso.

---

## PARTE 1 — FIX IL CONTROLLER DEL NOMAD (errore confermato)
Il `NomadAnimatorController.controller` DEVE usare queste clip, e SOLO queste:
- Idle → `Nomad_Idle` (NON Shovel_Idle!)
- Walk → `Nomad_WalkNew`
- Run → `Nomad_RunNew`
- Roll → `Nomad_RollNew`
- Dig → `Nomad_DigNew`
- StealthCrouch → `Nomad_StealthCrouch`
Parametri: `Speed`, `IsMoving`, `IsRolling`, `IsDigging`, `IsStealthed`.

**Passi:**
1. Apri il controller in Unity (Animator window).
2. Per OGNI stato, verifica che `m_Motion` punti alla clip corretta del Nomad (controlla i GUID).
3. FIX lo stato Idle: da `Shovel_Idle` → `Nomad_Idle`.
4. Verifica che i 6 stati usino clip del Nomad e NON clip di armi/mob.

---

## PARTE 2 — ANIMAZIONI ARMI (solo sul WeaponAnimator dell'arma)
Le armi hanno il LORO WeaponAnimator, separato dal controller del corpo. Mapping fisso:
- `shovel.default` → Idle `Shovel_Idle`, Swing `Shovel_Swing`
- `rifle.brass` → Idle `Rifle_Idle`, Fire `Rifle_Fire`, Reload `Rifle_Reload`/`Rifle_Reload_V2`
- `shotgun.heavy` → Idle `Shotgun_Idle`
- `blaster.rune` → Idle `Blaster_Idle`, Fire `Blaster_Fire`
- `sword.scimitar` → Swing `Scimitar_Swing`
- `icon.mortar_sandstorm` → proprio se disponibile
**Le clip delle armi NON devono mai andare sul corpo del personaggio.**

---

## PARTE 3 — ANIMAZIONI MOB (ognuno col suo set)
- **Dune Spitter** → `Spitter_Idle` (loop), `Spitter_DeathBurst` (one-shot su morte). MAI clip del Nomad o delle armi.
- **Sandstorm Golem** → `Golem_Charge` per Charge/Telegraph. MAI altro.
- **Crystal Turtle** → le proprie (se presenti), altrimenti usare lo sprite statico; MAI clip del Nomad.
- Ogni mob usa il PROPRIO sprite (spitter_32, golem, mob_crystal_turtle_64), mai lo sprite del Nomad.

---

## PARTE 4 — ANIMAZIONI MONDO (sul loro oggetto)
- Chest → `Chest_Open` sullo scrigno
- Vaso → `Vase_Break` sul vaso
- Pickup → `Pickup_Bob` sul pickup
MAI sul personaggio.

---

## PARTE 5 — VERIFICA MAPPING COMPLETO (tabella obbligatoria)
Produci una tabella VERIFICATA in Unity (via AssetDatabase + Animator window) per ogni entità animabile:

| Entità | Sprite | Stato/Param | Clip corretta | Verificato? |
|---|---|---|---|---|
| Nomad Idle | nomad_32 | - | Nomad_Idle | SI/NO |
| Nomad Walk | nomad_32 | Speed/IsMoving | Nomad_WalkNew | SI/NO |
| Nomad Run | nomad_32 | Speed | Nomad_RunNew | SI/NO |
| Nomad Roll | nomad_32 | IsRolling | Nomad_RollNew | SI/NO |
| Nomad Dig | nomad_32 | IsDigging | Nomad_DigNew | SI/NO |
| Nomad Stealth | nomad_32 | IsStealthed | Nomad_StealthCrouch | SI/NO |
| Pala | shovel_default | WeaponAnimator | Shovel_Idle/Swing | SI/NO |
| Fucile | rifle_brass | WeaponAnimator | Rifle_Idle/Fire/Reload | SI/NO |
| ... | ... | ... | ... | ... |
| Spitter | spitter_32 | - | Spitter_Idle/DeathBurst | SI/NO |
| Golem | (proprio) | - | Golem_Charge | SI/NO |
| Tartaruga | mob_crystal_turtle_64 | - | (propria) | SI/NO |

⚠️ **Ogni riga con Verificato=NO è un bug da correggere.** Non dichiarare "OK" senza aver verificato in Unity che la clip giusta è assegnata allo stato giusto.

---

## PARTE 6 — ASSICURA CHE TUTTI GLI ASSET SIANO USATI
Passa in rassegna l'inventario e verifica che OGNI asset sia referenziato da qualcosa:
- Tutti gli sprite 32×32 in `Art/Runtime/Processed/` sono usati (armi, mondo, proiettile, mob, UI)?
- Tutti i 21 fogli in `Anims/` producono clip usate?
- Nessun asset "morto" (generato ma mai referenziato).
Riporta la lista degli asset NON usati (se ce ne sono).

---

## PARTE 7 — PROVA A VIDEO
Registra e mostra:
1. Nomad che cammina (Idle/Walk/Run corretti, NON pala, NON scivola).
2. Nomad che dig (Dig corretto).
3. Nomad che roll (Roll corretto).
4. Arma in mano con la sua animazione (es. fucile Fire/Reload).
5. Spitter che attacca/muore (Spitter_Idle/DeathBurst).
6. Golem che carica (Golem_Charge).

## CRITERI DI ACCETTAZIONE
- [ ] Controller Nomad: Idle=Nomad_Idle (FIX confermato), tutti gli stati con clip del Nomad
- [ ] Armi: solo le loro clip sul WeaponAnimator
- [ ] Mob: ognuno col suo sprite e le sue clip
- [ ] Tabella mapping completa, nessuna riga NO
- [ ] Tutti gli asset risultano usati
- [ ] 6 video mostrati
- [ ] Console 0 errori

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). Committa e pusha ogni parte.