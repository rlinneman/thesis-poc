using Rel.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rel.Data.Ef6.Migrations
{
    internal static class AssetGenerator
    {
        internal static List<Asset> CreateAssets(int jobId, int count)
        {
            var assets = new List<Asset>();
            var pfx = new[] 
            { 
                "EGRD",      "AHU",   "BLR",   "BYPD",    "BYPV", 
                "CHLR",      "CHWC",  "CONDC", "CP",      "CRAC",
                "CS",        "CWC",   "DDVAV", "DXC",     "ECOIL", 
                "EF",        "EMS",   "ERU",   "FC",      "FH", 
                "FM",        "FPB",   "FTR",   "Furnace", "HD",
                "HD(Type2)", "HP",    "HWC",   "HX",      "ISOVL", 
                "PTAC",      "PUMP",  "RF",    "RH",      "RLF",
                "SCRB",      "SF",    "SGRD",  "STMC",    "TCV", 
                "TDV",       "TSTAT", "UH",    "UV",      "VAV", 
                "WH",        "WT" 
            };
            var r = new Random((int)DateTime.UtcNow.Ticks);

            var areas = GetAreas(r);

            for (; count-- > 0; )
            {
                assets.Add(new Asset()
                {
                    JobId                     = jobId,
                    Name                      = pfx[r.Next(0, pfx.Length)],
                    MinimumDecay              = Math.Round(r.NextDouble() * 1000000) / 10000,
                    MaximumAndMinimumDecay    = Math.Round(r.NextDouble() * 1000000) / 10000,
                    MaxMinDecayWithStepAndTol = Math.Round(r.NextDouble() * 1000000) / 10000,
                    MonotonicTolerance        = Math.Round(r.NextDouble() * 1000000) / 10000,
                    PercentTolerance          = Math.Round(r.NextDouble() * 1000000) / 10000,
                    ServiceArea               = areas[r.Next(0, areas.Length)],
                    StaticTolerance           = Math.Round(r.NextDouble() * 1000000) / 10000
                });
            }

            foreach (var name in assets.GroupBy(_ => _.Name))
            {
                //int i = 0;
                foreach (var asset in name)
                {
                    asset.Name = Guid.NewGuid().ToString();// string.Join("-", name.Key, (++i).ToString());
                }
            }

            return assets;
        }

        private static string[] GetAreas(Random r)
        {
            var serviceAreas = new string[]{
            "104,107,106", "128-ROOM", "145", "1D6W WOMENS RESTROOM", "1st Floor Corridor 126",
            "201B", "206 hot water", "213-217", "216 OFFICE", "2A-220",
            "2ND FLOOR CLASSROOMS/ STORAGE/ ORGAN LOFT", "2nd Floor Office Areas", "303 hot water",
            "409", "4404", "4433/4434/4435", "4447", "6TH FLR Storage", "8'X5' HOOD", "A162",
            "A315 Social Studies", "A6Z164", "A6Z168", "ACPSP HOOD 1", "ADMIN OFFICE",
            "AHU 1 Bypass chilled", "APT. 142 RR", "APT. 212", "ASSOCIATE 370/371", "ASSOCIATE 385",
            "ASSOCIATE 386", "ASSOICIATE 852", "BACK HALLWAY", "Back Office 2-35", "BAKERY OVENS",
            "BAKERY SALES", "BAR SIDE", "Bath 201B", "BEDROOM TYPE A (2013)", "BEDROOM",
            "BESIDE BRASS BELL HOOD", "Boys A040", "BREAK 829", "Break Rm 6 Flr", "BREAK RM D-119",
            "Breakout 307", "BUILDING STORAGE", "BYPASS", "Cafe 314 (VAV 28)", "Cafe 314 (VAV 8)",
            "CAFE SEATING", "Cafeteria 109", "CENTRAL ZONE", "CHECKOUT", "CHIEF 115",
            "CLASS RM - 102", "CLASSROOM 117", "CLASSROOM 126", "CLASSROOM 530", "Classroom C231",
            "Classrooom 207", "Client War Room 236", "CMP 112 EVP", "CO-MANAGER", "CONFERENCE 442",
            "CONFERENCE ROOM #225", "Corridor (212)", "Corridor 2000B", "Counter", "CROSSFIT AREA",
            "Ctr Dine", "CUSTOMER / MAIN", "D.O.N C151", "DELI / PREP 110", "DELI HOOD 2",
            "DELI HOOD H-2", "DELI PREP EF (RIGHTSIDE)", "DELI/BAKERY ", "DENTAL/0FFICE",
            "DINING/KITCHEN", "DIRECTOR 422", "DIRECTOR 461", "DISH HOOD (HD-5)", "DR. A OFFICE 107",
            "DRIVE THRU", "EAST RESTROOMS", "ELA 111", "ELECTRIC RM", "ELECTRICAL BLDG",
            "ELECTRICAL ROOM", "Elev 739", "ELEV. LOBBY 003", "ENTRY/OFFICE", "EXAM \"B\" 114",
             "EXAM 105", "EXAM 132", "EXAM 134", "EXAM 138", "EXAM C138", "EXAM G115", "EXAM M112"};

            var tmp = new HashSet<string>();
            for (int i = r.Next(1, 30); i > 0; )
            {
                var area = serviceAreas[r.Next(0, serviceAreas.Length)];
                if (tmp.Add(area))
                {
                    i -= 1;
                }
            }
            return tmp.ToArray();
        }
    }
}