﻿using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using System.IO;
using System.Xml;
using CustomSpawns;

namespace ModIntegration
{
    public static class SubModManager
    {

        public static SubMod[] dependentModsArray { get; private set; }

        public static void LoadAllValidDependentMods()
        {
            if (dependentModsArray == null)
            {
                //construct the array
                var loadedModules = TaleWorlds.Engine.Utilities.GetModulesNames();
                string basePath = Path.Combine(BasePath.Name, "Modules");
                List<SubMod> subMods = new List<SubMod>();
                var all = Directory.EnumerateDirectories(basePath);
                foreach (string path in all)
                {
                    if (Directory.Exists(Path.Combine(path, "CustomSpawns"))) //mod is a custom spawns mod!
                    {
                        //check if mod is a valid M&B mod.
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(Path.Combine(path, "Submodule.xml"));
                        }
                        catch(Exception e)
                        {
                            ErrorHandler.HandleException(new Exception("The submodule in path " + path + " does not have a SubModule.xml file or has an invalid one!"));
                        }
                        //check if mod is a valid Custom Spawns mod. If so, construct the mod.
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(Path.Combine(path, "CustomSpawns", "CustomSpawnsSubMod.xml"));
                            string subModuleName = doc.DocumentElement["SubModuleName"].InnerText;
                            if (loadedModules.Contains(subModuleName)) //load mod only if it is enabled.
                            {
                                SubMod mod = new SubMod(subModuleName, Path.Combine(path, "CustomSpawns"));
                                subMods.Add(mod);
                            }
                        }
                        catch(Exception e)
                        {
                            ErrorHandler.HandleException(new Exception("The submodule in path " + path + " does not have a CustomSpawnsSubMod.xml file or has an invalid one!"));
                        }
                    }
                }
                dependentModsArray = subMods.ToArray();
            }
        }

    }
}
