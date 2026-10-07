namespace SpaceMiner
{
    // Fictional archive entries; these descriptions do not unlock resources or damage rules.
    public static class PlanetLore
    {
        public sealed class Entry
        {
            public readonly string Name, Kind, Description, Archive, Status;
            public Entry(string name,string kind,string description,string archive,string status)
            {Name=name;Kind=kind;Description=description;Archive=archive;Status=status;}
        }
        public static readonly Entry[] All = {
            new Entry("Veyrath","Glut- und Metallwelt",
                "Schwarze Ebenen aus erkaltetem Glas wechseln mit glühenden Bruchzonen. Metallhaltige Stürme ziehen über die sonnennahe Oberfläche.",
                "Die alten Navigatoren nannten Veyrath den ersten Funken. Keine Sonde blieb dort länger als einen Tag aktiv.","Extreme Hitze · keine bestätigte Landestelle"),
            new Entry("Soryn","Bernsteinfarbene Wolkenwelt",
                "Eine schwere Atmosphäre verhüllt kupferfarbene Gebirge. In ihren Wolken schweben winzige Kristalle, die das Licht beider Sonnen brechen.",
                "Frühe Expeditionen meldeten einen goldenen Regen. Die Messgeräte kehrten zurück, ihre Schutzschichten jedoch nicht.","Dichte Atmosphäre · korrosive Wolken"),
            new Entry("Ilythra","Ringtragende Frostwelt",
                "Blaues Gestein und tiefe Eisbecken liegen unter einem schmalen Ring aus hellen Splittern. Seine Schatten wandern wie Zeiger über die Oberfläche.",
                "Ein wiederkehrendes Funksignal wurde einst als Gesang von Ilythra bezeichnet. Wahrscheinlich entstand es durch geladene Ringpartikel.","Eisvorkommen vermutet · Ringdurchflug riskant"),
            new Entry("Aetherys","Zerstörte Heimatwelt · Planet 4",
                "Hier lagen unsere Städte, Wälder und Archive. Heute treiben aufgerissene Kontinentplatten um einen offenen, glühenden Kern.",
                "Die neue Energiequelle sollte den Weg zu den Sternen öffnen. Ihre Kettenreaktion zerbrach Aetherys. Seitdem gibt es keine bestätigte Antwort von der Heimat.","Giftige Trümmerwolken · extreme Resthitze · keine bestätigten Überlebenden"),
            new Entry("Kharuun","Rote Schluchtenwelt",
                "Rostfarbener Staub bedeckt gewaltige Grabenbrüche. Magnetische Entladungen lassen die Schluchten während langer Stürme violett aufleuchten.",
                "Unter einer versunkenen Forschungsstation soll ein versiegeltes Mineralarchiv liegen. Seine letzte Positionsmeldung ist unvollständig.","Staubstürme · schwankendes Magnetfeld"),
            new Entry("Oruvex","Goldener Gasriese",
                "Breite Wolkenbänder umkreisen ein dauerhaftes Sturmauge. In tieferen Schichten werden dichte Flüssigkeitsmeere vermutet.",
                "Oruvex galt als Wächter der äußeren Bahnen. Alte Messbojen lieferten dort mehr Wetterdaten als an jedem anderen Ort des Systems.","Keine feste Oberfläche · gewaltiger Druck"),
            new Entry("Nymara","Türkisfarbener Ringriese",
                "Mehrere Ringe aus Eis und dunklem Staub umgeben die kühle Atmosphäre. Einzelne Lücken verraten den Einfluss kleiner, bislang unkartierter Monde.",
                "Ein verlorenes Observatorium beobachtete von hier die Heimatwelt. Sein Archiv könnte die letzten Stunden vor der Katastrophe enthalten.","Dichte Ringe · Standort des Observatoriums unbekannt"),
            new Entry("Vaelora","Kobaltblauer Eisriese",
                "Dunkle Windbänder ziehen über eine tiefblaue Wolkendecke. Ein schräger, dünner Ring erscheint im Gegenlicht wie eine silberne Narbe.",
                "Fernsonden registrierten regelmäßig helle Polarlichter. Die alten Karten bezeichneten sie als die Laternen von Vaelora.","Tiefe Kälte · starke Höhenwinde"),
            new Entry("Xhal'Tir","Violette Grenzwelt",
                "Am Rand des Systems herrscht langes Zwielicht. Gefrorene Ebenen und dunkle Einschlagbecken speichern die Spuren sehr alter Kollisionen.",
                "Eine einzelne Vermessungsboje meldete dort ein unterirdisches Echo. Ob es ein Hohlraum, ein Messfehler oder etwas anderes war, blieb ungeklärt.","Kaum kartiert · unbestätigte Tiefensignale")
        };
    }
}
