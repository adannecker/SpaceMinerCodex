# Doppelsonnen: reale Solarleistung und Stromkonsole

26.10.2026. Auf Nutzerauftrag sind beide Sonnen als Energiequellen definiert und in die laufende Simulation sowie den gemeinsamen Strombildschirm integriert. Die moderate Einstrahlung ist eine Spielweltentscheidung. Sie ersetzt das frühere unbestätigte Beispiel mit1,0/0,5 Sonnenleuchtkräften.

## Sternparameter und Abstand

| Grösse am Stationsursprung | Hauptsonne A | Begleitstern B |
| --- | ---: | ---: |
| Referenztyp | ruhiger K-Zwerg | ruhiger K-Zwerg |
| Oberflächentemperatur | 5000K | 4200K |
| Radius / Sonnenradius | 0,80 | 0,65 |
| Leuchtkraft / Sonnenleuchtkraft | 0,360375 | 0,118446 |
| Strahlungsleistung | 1,380×10²⁶W | 4,534×10²⁵W |
| Physikalischer Abstand | 1,042182AE /155,9Mio.km | 1,017039AE /152,1Mio.km |
| Abstand in der verkürzten Kulisse | 1875,928 Darstellungs-km | 1830,670 Darstellungs-km |
| Einstrahlung vor Panelprojektion | **451,57W/m²** | **155,85W/m²** |

Zusammen **607,42W/m²**, etwa44,6% der Referenzeinstrahlung der Sonne an der Erde. Beide Richtungen liegen12,3894° auseinander. Die bestehenden Vektoren in `RuinedWorld.cs` bleiben erhalten. Explizite Energieskala: **1800 Darstellungs-km entsprechen1 physikalischen AE**. Lokale Bewegungen/Stationsgeometrie bleiben echte Meter und werden nicht mitvergrössert; die Beobachterposition wird vom physikalischen Sternvektor abgezogen. Gerenderte Sternradien/Farben/Lichtintensitäten bleiben Gestaltung und bestimmen keine Wattwerte.

`L/L☉=(R/R☉)²×(T/5772K)⁴`; `G=1361W/m²×(L/L☉)/rAE²`. Referenzleuchtkraft3,828×10²⁶W. Physikalische Referenzen: [NASA: solare Einstrahlung](https://earth.gsfc.nasa.gov/climate/projects/solar-irradiance/science) und [NASA: Space Math V](https://www.nasa.gov/wp-content/uploads/2013/05/space-math-v.pdf). Temperatur/Radius sind die jetzt festgelegten fiktiven Sterne, keine astronomischen Messungen. „Ruhig“ ist ein Referenzzustand: UV-/Teilchendosis und Sonnenstürme sind weiterhin nicht simuliert. Die Energiezahl belegt keine Strahlungssicherheit.

## Photovoltaik im Spiel

Für jeden Stern und Flügel einzeln: `P=Aeff×0,30×0,85×G×max(0,n·s)×Sichtanteil/1000` in kW. Die Werte0,30 Zellwirkungsgrad und0,85 Nettofaktor sind definierte Spielmodellparameter. Der Sichtanteil stammt aus echten Collider-Abfragen; Rückseiten liefern null. Je verbleibendem PV-Segment ein Strahl pro Sonne, maximal zweimal pro Echtzeitsekunde plus bei geänderter Panelposition/-rotation. Damit erhöht500× Spieltempo die Abfragezahl nicht500fach. Station, anderer Flügel und Asteroiden können abschatten; eigene kleine Flügelzierleisten sind ausgenommen. Zellmittelpunkte, harte Schatten und Colliderreichweite1000km sind Näherungen, keine vollständige elektrische String-/Halbschattenrechnung.

Originalfläche pro vollständigem Flügel386,386m²; aktuell A353,267m² und B375,346m². Elektrisch nutzbar ist `min(verbleibendeGeometrie,Originalfläche×Zustand)`: A251,151m² bei65%, B312,973m² bei81%. Fehlende Zellen werden dadurch nicht zusätzlich doppelt als pauschaler Schaden abgezogen.

Die Rechnung ergibt **4,617kW** bei jetziger Neigung, beiden verbundenen Flügeln und ohne zusätzliche Szenenabschattung. Der echte Startzustand liefert **A0kW** (Sektor3 getrennt, zusätzlich teilweise abgeschattet) und **B3,965kW**; mit dem vorhandenen Reaktor1,64kW sind **5,605kW verfügbar**. Das sind verschiedene Randbedingungen, kein Widerspruch. Bei idealer Ausrichtung beider Flügel wären mit aktuellem elektrischem Zustand ohne Schatten zusammen etwa **86,99kW** verfügbar. Mit allen aktuell verbleibenden Segmenten bei voller elektrischer Nutzbarkeit etwa112,35kW. Das ist eine obere Grenze, keine garantierte Einspeisung im aktuellen Netz und keine neu eingebaute Nachführung.

Das gespeicherte Feld `SolarPowerKw=0,5` bleibt aus Kompatibilitätsgründen erhalten, bedeutet jetzt den neutralen Kalibrierwert100%; Skalierung `Wert/0,5`. Es ist kein zusätzliches physikalisches0,5-kW-Limit. Null deaktiviert Solar. Keine Saveformat-, Kapazitäts- oder Raumplanänderung.

## Speicher, Anzeige und Bedienung

Damit hohe PV-Erträge den vorhandenen24-kWh-Akku nicht sofort füllen, ist dessen Ladeleistung auf **0,5C =12kW** begrenzt. Restkapazität und Simulationsdauer begrenzen weiter; volle Bank nimmt nichts mehr auf. Die vorhandene8-kW-Entladegrenze und Verbrauchspriorität bleiben erhalten. Ungenutzter Überschuss wird abgeregelt. Null Simulationszeit erzeugt keine Energie. Das ersetzt keine spätere konkrete Akku-/Laderauslegung.

Strompult in der Zentrale mit **E** öffnen, unten **SONNEN2** wählen. Beide Sterne zeigen Temperatur, Radius, Leuchtkraft, Einstrahlung, physikalischen Abstand und Kulissenabstand. Beim einzelnen Solarfeld stehen beide Beiträge, Winkel und Sichtanteile. Oben bedeutet **VERFÜGBAR** verfügbare Erzeugung; abgeregelte Leistung steht zusätzlich darunter. **AKKUBILANZ** und Voll-/Leer-Prognose verwenden den tatsächlichen begrenzten Speicherfluss. Der Verlauf zeichnet verfügbare Erzeugung und Verbrauch auf.

Das separat startbare Konsolenstudio verwendet denselben Renderer und dieselben Sternparameter in klar gekennzeichneten festen Testfällen. **Strommangel** trennt beide Solarfelder; es gibt keine zweite Kampagnensimulation. Start: `Start-Konsolenstudio.cmd`. Hauptspiel: `Start-Startstation.cmd`, Development: `Start-Startstation-Dev.cmd`.

## Dateien und tatsächlich erfolgte Prüfung

Neuer Energiekern `BinaryStarEnergy.cs`, Zell-/Schattenleser `SolarFieldSampler.cs`, Prüfungen `BinaryStarPowerChecks.cs`; eingebunden über `StationPowerReadModel.cs`, `StationOperations.Power.cs`, `StationWork.cs`, Telemetrie, HUD und gemeinsamen `PowerConsoleView.cs`/Studio. Referenzdaten und `tools/AssessStationEnergy.py` verwenden dieselben Sterne.

Unity**6000.6.5f1**, vorhandene Szenen ohne Setup neu gebaut: Development, Release und Konsolenstudio. Reproduzierbare Editor-Methode `SpaceMiner.Editor.BinaryStarBuild.Build` prüft die gepinnte Version. Finale grafische Development-Läufe: **507 Strom-/Sonnen-/R-01-Prüfungen mit20 Aufnahmen**, **97 Stationsablaufprüfungen**, Konsolenstudio **36 Aufnahmen**, alle Exit0. Zusätzlich22 analytische Checks. Sternübersicht, Solarfeld, grosse Schrift mit gescrolltem Ende und Studio-Langtextbild visuell geprüft. Ein Zwischenlauf scheiterte an nicht restaurierter Test-UI-Auswahl; korrigiert und final neu gebaut/geprüft.

Originale drei Raumpläne und sämtliche eingefrorenen C#-/Meta-Quellen nach den Prüfungen SHA256-abgeglichen. Geprüfte Player nach `Builds/Windows`, `Builds/WindowsRelease` und `Builds/ConsoleStudio` übernommen, Vorgänger jeweils mit `before-binary-stars` gesichert; alle ausgelieferten Dateien SHA256-identisch zu den Prüfbuilds. [Vollständiger Nachweis](Pruefungen/Doppelsonnen-Energie-2026-10-26.json), [Sonnenbild](Pruefungen/Doppelsonnen-Energie-2026-10-26/detail-sun-primary.png).

Die frühere Lastrechnung bleibt für realistischere Stationsverbraucher/3-12-kW-Drohnenlader/4-kWh-I75/40-kWe-Reaktor eine **Auslegung, noch keine vollständige Umsetzung**. Bestehende Lasten und Reaktor wurden in diesem Auftrag nicht durch diese Vorschläge ersetzt. Keine neue natürliche Komplettkampagnen-, OS-Eingabe-, FPS- oder Headsetabnahme. Release gebaut und dateigeprüft; grafische Laufzeitprüfungen auf Development. Kein Commit/Push.
