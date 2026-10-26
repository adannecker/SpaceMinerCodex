# I-75: Grundform und zusätzliche Ansichten für Tripo

**Folgestand:** [Erster Tripo-3D-Entwurf fertig und FBX/4K gesichert](Innendrohne-I75-Tripo-Generierung01-2026-10-25.md). Der spätere ausdrücklich beauftragte Upload wurde zugelassen; der unten dokumentierte Block betrifft den ersten Versuch. Rohmodell mit Formfehlern, noch kein Spieleinbau.

Stand 25.10.2026. Nutzer beauftragt den ersten Tripo-Versuch nur mit der Grunddrohne; Werkzeuge, Transportgestell und Last folgen separat. Anschließend ausdrücklich zusätzliche Ansicht schräg von unten mit gegebenenfalls ausgefahrenen Magnetfüßen sowie eine Rückansicht gewünscht. Grundlage ist die [I-75-Konzeptfamilie](Innendrohne-I75-Konzept-2026-10-25.md); die [aufrechte Wabenbeladung](Innendrohne-I75-Wabentransport-2026-10-25.md) bleibt eine spätere eigene Ausrüstung.

## Gesicherte Bildvorlagen

Aktuell zur gemeinsamen Betrachtung zusammengestellt: [Bildsatz 01 mit vier Einzelansichten](Innendrohne-I75-Tripo-Bildsatz01-2026-10-25.md). Er ergänzt die Seitenansicht und gleicht die Fußhalter in Front und Heck an die ausgefahrene Unteransicht an. Die folgende Tabelle hält die vorangehenden Einzelentwürfe fest.

Alle drei Bilder zeigen jeweils nur eine Drohne auf weißem Hintergrund, ohne menschliche Referenz, Maßpfeile, Werkzeuge oder Fracht. Mit eingebautem ImageGen erzeugt, keine CLI.

| Ansicht | Bild | Vollständiger Prompt |
| --- | --- | --- |
| Vorn/links, Module eingefahren | [PNG](Art/Drohnen/Innendrohne-I75-Grundform-Tripo-2026-10-25-v1.png) | [Prompt](Art/Drohnen/Innendrohne-I75-Grundform-Tripo-2026-10-25-v1.prompt.txt) |
| Schräg unten/vorn, Module ausgefahren | [PNG](Art/Drohnen/Innendrohne-I75-Grundform-underside-2026-10-25-v1.png) | [Prompt](Art/Drohnen/Innendrohne-I75-Grundform-underside-2026-10-25-v1.prompt.txt) |
| Aktuelles Heck V2, Blick auf das tatsächliche Akku-Ende | [PNG](Art/Drohnen/Innendrohne-I75-Grundform-rear-2026-10-25-v2.png) | [Prompt](Art/Drohnen/Innendrohne-I75-Grundform-rear-2026-10-25-v2.prompt.txt) |

Gemeinsam: dunkelblaues gefastes Chassis, helle Metallstoßleisten, verschraubte Paneele, Hebeösen, dezente Cyananzeigen und gelbe Wartungsmarkierungen. Vorn bleibt die leere Universalaufnahme unter dem Doppelsensor. Das aktuelle Heck V2 übernimmt das vorstehende, gefaste Akkugehäuse mit schmaler gelber Statusmarkierung vom tatsächlich hinteren Ende der ursprünglichen Front-/Seitenansicht; Hebeösen nahe diesem Ende, Dach und Seitenwand laufen zur verdeckten Front weg. Die Unterseite zeigt eine geschlossene Bodenplatte, vier Magnetmodule und geschützte Anschlussleitungen.

**Nutzerkorrektur:** Die zuvor als korrigierte Rückansicht bezeichnete [V1](Art/Drohnen/Innendrohne-I75-Grundform-rear-2026-10-25-v1.png) hatte die Heckklappe tatsächlich erneut auf die Front gesetzt; die erste Sichtprüfung hatte diesen räumlichen Fehler nicht erkannt. V1 ist verworfen und darf nicht als gültige Heck-/Tripo-Referenz verwendet werden. Die damaligen Prompts bleiben als Entstehungsnachweis erhalten. V2 wurde ausschließlich aus der ursprünglichen Frontvorlage mit neuem Kamerastandpunkt am gegenüberliegenden Ende erzeugt, nicht erneut durch Übermalen der Frontaufnahme. Keine weitere Nutzerfreigabe von V2 vorweggenommen.

## Maß- und Bewegungsannahmen

Chassislänge weiterhin **0,75 m**, Breite **0,55 m** und Höhe **0,45 m** sind ergänzende vorläufige Vorgaben. In den zwei Zusatzansichten sind vier kurze teleskopische Modulhalter ausgefahren. **0,05 m Hub** wurde als Bildentwurf vorgeschlagen, nicht vom Nutzer als technisches Maß bestätigt. Das ist der Halterhub zwischen Chassis und Magnetmodul, kein bestätigter magnetischer Schwebespalt und keine tragfähige Antriebsauslegung. Bei gleichem Chassis erhöht der Vorschlag die äußere Höhe entsprechend; die Bilder sind nicht vermessbar.

Die Ansichten bleiben illustrativ; verdeckte Rückseiten, Paneele und Proportionen wurden vom Bildmodell ergänzt. Keine garantierte geometrische Übereinstimmung wie bei gerenderten Ansichten eines einzigen CAD-Modells. Besonders die erste Frontansicht zeigt eingefahrene Module; Unteransicht und Heck V2 zeigen unterschiedliche Detailausprägungen der kurzen Halter. Für einen gemeinsamen Tripo-Mehransichtenlauf erst eine einheitliche Stellung/Mechanik wählen und verbleibende Formabweichungen bereinigen. Die neuen Ansichten sind zunächst Designreferenzen; bewegliche Halter und Magnetfüße müssen später als getrennte Objekte mit echten Führungen/Drehpunkten modelliert werden. Tripo-Maßstab anschließend in Blender/Unity auf 0,75 m Chassislänge setzen; 1 Unity-Einheit = 1 m.

## Erster Tripo-Versuch: damals blockiert

Im angemeldeten Tripo-Workspace HD Model/H3.1 war vor dem Versuch ein Preis von **60 Credits** angezeigt; Kontostand **3005**. Keine Zusatzoption wie 8K oder Generate in Parts aktiviert. Der Upload der bereinigten Frontvorlage wurde über den vorgesehenen Dateiauswahldialog versucht. **Die Browser-Sicherheitsfreigabe wurde abgelehnt; `setFiles` konnte die Datei nicht hochladen.** Kein alternativer Uploadweg versucht und keine Tripo-Generierung gestartet. Nach dem Block unveränderter Seitenzustand mit leerem Uploadbereich und 3005 Credits; [gesicherter Browserzustand](Pruefungen/Innendrohne-I75-Tripo-2026-10-25/01-upload-blockiert.jpg). Der Screenshot zeigt die Seite nach dem Block, nicht den Freigabedialog selbst.

Die Fortsetzung erforderte damals einen vom Nutzer selbst ausgeführten Upload oder eine anschließend ausdrücklich zugelassene Uploadaktion. Inzwischen erfolgte der zugelassene Versuch, siehe Folgestand oben. Werkzeuge/Transportgestell nicht Teil dieses Grundformauftrags. Zum damaligen Blockzeitpunkt noch kein FBX/GLB, keine Blender-Bearbeitung und keine Unity-Integration. Die vorhandene interne R-01 und Spielparameter bleiben unverändert.

## Tatsächlich geprüft

Aktuelle Sichtprüfung: isoliertes Grundchassis, leere Frontaufnahme, vier sichtbare Füße in der Unteransicht; Heck V2 anhand des vorstehenden Akkugehäuses und der nun kameranahen Hebeösen mit der ursprünglichen Frontvorlage verglichen. Frühere Rückseitenprüfung V1 durch Nutzerhinweis widerlegt, siehe oben. PNG-Signaturen, Pixelmaße, SHA256 sowie lokale Dokumentlinks geprüft; [Dateinachweis](Pruefungen/Innendrohne-I75-Tripo-2026-10-25.json). Keine technische Last-/Magnet-/Festigkeits-/Bewegungsprüfung, kein Build oder Laufzeittest. Bestehende Änderungen anderer Chats bewahrt, kein Commit/Push.
