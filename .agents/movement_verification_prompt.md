# Sandsunder - Play Mode Movement Verification Prompt

Per verificare che il gameplay di Sandsunder sia attivo e dinamico senza scivolamenti o fluttuazioni visive:

1. **MOVIMENTO ANIMATO:** Avvia la scena `Assets/Scenes/GameplayLab.unity` in Play Mode e muovi il personaggio con i tasti WASD. Il personaggio deve riprodurre le clip di camminata/corsa (`Nomad_WalkNew`, `Nomad_RunNew`), muovendo gambe e corpo.
2. **ANCORAGGIO ED ORIENTAMENTO ARMA:** Muovi il mouse in cerchio attorno al personaggio. Il fucile in ottone deve rimanere ancorato alla mano (pivoting dinamico sinistra/destra) e ruotare mantenendosi orientato verso il puntatore.
3. **COMBATTIMENTO & PROIETTILI:** Fai fuoco contro un nemico. I proiettili luminosi (dimensione 1.2x0.8 con scia 0.30f) devono scaturire dalla canna dell'arma ed impattare sul bersaglio.
4. **SCAVO DELLA SABBIA:** Premi il tasto di scavo (o fai clic). La cella a terra deve mostrare progressivamente i 3 stadi (intatta -> crepe -> cratere scavato).
5. **TRANSIZIONE SOTTERRANEA:** Premi `Shift` o scendi a profondità 2. Il livello di superficie sfuma e compare la silhouette ciano traslucida `#00F0E6`.
6. **COLLISIONE PARETI NEMICI:** Attira un nemico (Spitter o Turtle) contro un muro di rovine. Il nemico deve arrestarsi fisicamente senza attraversare la parete.
