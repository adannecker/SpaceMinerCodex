# Chapter 1: Wiederverwertung

Gespeicherter 10-Knoten-Bildentwurf und gemeinsame Einstiegsübersicht mit Wiederverwertung 0/10: [Techtree-Galerie vom 08.10.2026](Techtree/README.md). PNG im Repository, weiterhin Gestaltungsvorschlag ohne Gameplayimplementierung.

Stand: 08.10.2026. Neuer eigener Techtree-Bereich nach Nutzerentscheidung. Aus Trümmern sollen vollständige Bestandteile wie Akkus und Hüllenplatten gewonnen werden; ein weiterer Pfad zerlegt sie in kleinere Bestandteile und schließlich nutzbare Rohstoffe. Für die gezielte Rohstoffrückgewinnung muss der betreffende Stoff erforscht sein. Konkrete Knoten, Anlagen, Ausbeuten, Mengen und Chapter-1-Grenzen unten sind Vorschläge, keine Implementierung.

## Gemeinsamer Einstieg

1. **Trümmerbewertung:** bekannte/gescannte Funde untersuchen; vorhandene Baugruppen, Zustand und erreichbare Bergungsstellen ermitteln. Erkundung liefert nicht automatisch die vollständige innere Zusammensetzung.
2. **Gezielte Bergung:** ausgewählte Teile lösen, transportieren und in geeigneten Lager-/Werkstattplätzen sichern. Drohnenwerkzeuge, Tragfähigkeit und Arbeitsfreigabe erforderlich; Zuordnung zu den vier vorhandenen Rollen noch offen, keine fünfte Drohnenart beschlossen.
3. **Bauteildiagnose:** Funktion, Schäden und Kompatibilität prüfen; danach zwischen Wiederverwendung, Reparatur, Zerlegung und Materialaufbereitung wählen.

## Drei verzweigte Wege

| Weg | Vorgeschlagene Forschungsknoten | Ergebnis |
| --- | --- | --- |
| A: Vollständige Bestandteile erhalten | Bauteilprüfung → passende Reparatur/Aufarbeitung → Wiederverwendung. | Geprüfte Akkumodule, Hüllenplatten, Motoren, Pumpen, Kabel oder Steuerungen. |
| B: Kleinere Bestandteile gewinnen | Präzisionsdemontage → baugruppenspezifische Zerlegung → Einzelteilprüfung. | Aus einem Akkupaket passende nutzbare Module/Zellen, Leiter und Gehäuse; aus einer Steuerung geprüfte Platinen/Stecker; aus einer Struktur Platten, Profile und Befestigungen. |
| C: Stoffe zurückgewinnen | Materialsortierung → stoff-/produktspezifische Aufbereitung → Trennung/Reinigung → Qualitätsfreigabe. | Geeignete Metalllegierungen, gereinigte Metalle, nutzbare Verbindungen oder ausgewählte Polymer-/Elektrodenwerkstoffe. |

Diese Wege sind Alternativen beziehungsweise kombinierbare Teilaufträge. Eine geprüfte Hüllenplatte muss nicht erst eingeschmolzen werden. Eine Baugruppe kann brauchbare Teile liefern, während der beschädigte Rest in die Materialaufbereitung geht. Zerlegung vernichtet die ursprüngliche Baugruppe; dieselbe Masse darf nicht gleichzeitig als kompletter Akku und als dessen Rohstoffe ausgegeben werden.

## Voraussetzungen aus anderen Bereichen

- **Scanner/Erkundung:** Fundort und nötige Arbeitsdaten; Bekannte Trümmer erlauben keinen pauschalen Zugriff auf ungesehene Gebiete.
- **Drohnen/Robotik:** passende Werkzeuge, Transport und gegebenenfalls Innenhelfer/Dockarbeit.
- **Materialkunde:** Stoffidentifikation, erforschtes Rohstoff-/Werkstoffwissen und geforderte Reinheit.
- **Produktion/Fertigung:** erforderliche Demontage-, Trenn-, chemische oder thermische Anlagen; Forschung ersetzt keine Maschine.
- **Energie/Wärme:** ausreichende Versorgung und Wärmeabfuhr für das gewählte Verfahren.
- **Lagerung/Logistik:** Plätze für vollständige Teile, sortierte Fraktionen und nicht identifizierte Reststoffe.

Vorgeschlagene Freigaberegel: Quelle/Baugruppe passend identifiziert + Zielstoff erforscht + auf dieses Ausgangsmaterial anwendbares Rückgewinnungsverfahren erforscht + passende Anlage/Versorgung/Lagerkapazität vorhanden. Ein Aluminiumforschungsknoten allein schaltet nicht jedes aluminiumhaltige Recyclingverfahren frei.

Für die Wiederverwendung eines vollständigen geprüften Akkumoduls wird dessen Funktion und Kompatibilität benötigt, nicht vorher die komplette Eigenfertigung sämtlicher enthaltenen Stoffe (Vorschlag). Für gezielte Lithiumrückgewinnung sind dagegen Lithiumwissen, erkannte Zellchemie und ein passender Recyclingpfad nötig. Bekannte Starttechnik bleibt Anfangswissen.

Unbekannte Anteile bleiben als gekennzeichnete Probe/Mischfraktion erhalten. Das Wissen um Aluminium erlaubt nicht automatisch, unbekannte Beschichtungen oder Legierungsbestandteile als reine Rohstoffe zu verbuchen. Teilrückgewinnung ist möglich, wenn das Verfahren sie tatsächlich trennt; übrige Stoffe, Verluste und Rückstände müssen bilanziert werden.

## Beispiele zur Besprechung

**Hüllenteil:** bergen → Zustand/Material prüfen → brauchbare Platte aufarbeiten und montieren. Alternative: unbrauchbare Platte demontieren → Beschichtungen/Fremdteile passend abtrennen → Legierungsfraktion sortieren → wiederaufbereiten → geprüfte neue Profile/Platten fertigen. Eine Aluminiumlegierung wird nicht allein durch Einschmelzen zu reinem Aluminium. Gegebenenfalls genügt eine für den Zweck geeignete geprüfte Legierung, ohne Trennung sämtlicher Elemente.

**Akkupaket:** bergen → Diagnose und Chemie/Kompatibilität klären → als Modul wiederverwenden oder geeignetes Paket in Einzelteile zerlegen. Brauchbare geprüfte Module/Zellen können neue passende Baugruppen versorgen. Unbrauchbare Zellen benötigen einen eigenen chemiespezifischen Recyclingprozess; mögliche Ausgänge sind Metallfraktionen und nutzbare Lithium-/Elektrodenverbindungen. Es entstehen nicht automatisch reine Elemente oder neue funktionsfähige Zellen.

**Elektronik:** Steuerung bergen → prüfen/reparieren → vollständig wiederverwenden. Alternative: zerlegen → geeignete Platinen, Stecker und Bauteile prüfen. Weiteres Recycling beschädigter Restplatinen benötigt einen eigenen Materialtrennpfad. Edelmetalle, Reinheiten und Ausbeuten erst anhand der tatsächlichen Elektronik festlegen.

Reale Akkurecyclingverfahren unterscheiden direkte Wiederaufbereitung von Elektrodenmaterial, thermische Verfahren und chemische Rückgewinnung. Ergebnisse können nutzbare Verbindungen oder erhaltenes Kathodenmaterial sein; Elementzerlegung ist nicht für jede Wiederverwertung der sinnvollste Endpunkt. Quellen: [US DOE, Batteries and Recycling](https://afdc.energy.gov/vehicles/electric-batteries), [Argonne, direkte Kathodenwiederverwertung](https://www.anl.gov/article/recell-center-could-save-costly-nickel-and-cobalt-transform-battery-recycling-worldwide), [Argonne, EverBatt](https://publications.anl.gov/anlpubs/2019/07/153050.pdf). Diese terrestrischen Verfahren sind fachliche Orientierung, keine fertig ausgelegte Stationsanlage.

## Erfahrung, Darstellung und erste Kapitelgrenze

Anwendbar auf den beschlossenen gelben Fortschrittsrand: praktische Bergungs-/Demontage-/Aufbereitungserfahrung am jeweils passenden Knoten, mit Level/Bonus im Overlay. Vorschlag: schneller zerlegen, weniger vermeidbare Beschädigung und Verluste. Neue Stoffe oder Verfahren bleiben Forschungsfreischaltungen; Erfahrung ersetzt keine Stoffkenntnis oder notwendige Hilfsstoffe. Grenzwerte, gemeinsame Wissenszuordnung und Lernschwellen offen.

Im Techtree vorgeschlagen: links der gemeinsame Bergungs-/Diagnoseeinstieg, danach drei horizontale Zweige mit separaten Andockpunkten. Externe Voraussetzungen als eigene markierte Eingänge. Das Overlay zeigt die konkreten möglichen Ausgänge und Gründe für Sperren, beispielsweise „Lithium noch nicht erforscht“ oder „Zellchemie unbekannt“. Der gelbe Rand steht für Erfahrung, der Fund-/Materialzustand separat.

Chapter-1-Vorschlag: ganze Teile wiederverwenden, präzise zerlegen und erste passende Metallfraktionen aufbereiten. Komplexe Akku-/Elektronik-/Polymerverfahren schrittweise ausarbeiten; noch keine feste Tier-/Kapitelzuordnung. Konkrete Trümmerfundorte, verfügbare Mengen, Stationsbestände und Bergungsaufträge bleiben offen.

Prüfung am 08.10.2026: Projektquellen abgeglichen, DOE-/Argonne-Primärquellen recherchiert, Dokumentation und Diff-Format geprüft. Keine vollständige Recyclingauslegung, Mengen-/Energiebilanz, Implementierung, Builds oder Spieltests.
