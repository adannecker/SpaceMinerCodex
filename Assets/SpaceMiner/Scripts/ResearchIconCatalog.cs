using System.Collections.Generic;

namespace SpaceMiner
{
    // Explicit semantic pictograms, in the same numbered order as the approved reference sheets.
    public static class ResearchIconCatalog
    {
        private static readonly Dictionary<string,string[]> Icons=new Dictionary<string,string[]>{
            {"mira",new[]{"laboratory","chart","optimization","monitor","wrench","cpu","battery","fan","server","database","analysis","quadcopter"}},
            {"energie",new[]{"station","solar","solar","solartrack","battery","batteries","distribution","distribution","fastcharge","fan","radiator","station"}},
            {"drohnen",new[]{"miningdrone","pickaxe","cargo","monitor","repairdrone","monitor","wrench","assembly","quadcopter","map","sample","camera","robot","dock","cart","tasks"}},
            {"scanner",new[]{"radar","filter","antenna","map","camera","terrain","sample","spectrum","microscope","radar","route","tasks"}},
            {"produktion",new[]{"monitor","wrench","assembly","analysis","furnace","press","drill","assembly","reactor","molecule","filter","flask","circuit","circuit","wafer","cpu"}},
            {"logistik",new[]{"database","quarantine","analysis","cargo","tank","gastank","cart","dock","route","tasks","cart","route"}},
            {"lebenserhaltung",new[]{"sample","filter","watercycle","spectrum","gastank","filter","monitor","seal","thermometer","cargo","reactor","food"}},
            {"station",new[]{"monitor","wrench","dock","blueprint","distribution","assembly","gauge","habitat","dome","compass","wrench","solartrack"}},
            {"wiederverwertung",new[]{"wreck","salvage","monitor","wrench","recycle","disassemble","analysis","sorting","reactor","analysis"}},
            {"materialien",new[]{"microscope","filter","electrolysis","gastank","gastank","sorting","ingot","ingot","coil","ingot","gem","glass","wafer","powder","filter","graphite","molecule","polymer","filter","battery","powder","gastank","molecule","wafer"}},
            {"verbindungen",new[]{"microscope","molecule","flask","molecule","gastank","molecule","molecule","polymer","seal","powder","battery","powder","flask","electrolyte","molecule","polymer","molecule","polymer","glass","powder","resin"}}
        };
        public static string For(string id)
        {
            int split=id.LastIndexOf('-');
            return Icons[id.Substring(0,split)][int.Parse(id.Substring(split+1))-1];
        }
    }
}
