﻿//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace wmib
{
    public partial class core
    {
        public static void InitialiseMod(Module module)
        {
            if (module.Name == null || module.Name == "")
            {
                core.Log("This module has invalid name and was terminated to prevent troubles", true);
                throw new Exception("Invalid name");
            }
            module.Date = DateTime.Now;
            if (Module.Exist(module.Name))
            {
                core.Log("This module is already registered " + module.Name + " this new instance was terminated to prevent troubles", true);
                throw new Exception("This module is already registered");
            }
            try
            {
                lock (module)
                {
                    core.Log("Loading module: " + module.Name);
                    Module.module.Add(module);
                }
                if (module.start)
                {
                    module.Init();
                }
            }
            catch (Exception fail)
            {
                module.working = false;
                core.Log("Unable to create instance of " + module.Name);
                core.handleException(fail);
            }
        }

        public static bool LoadMod(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    System.Reflection.Assembly library = System.Reflection.Assembly.LoadFrom(path);

                    //AppDomain domain = AppDomain.CreateDomain("$" + path);
                    if (library == null)
                    {
                        Program.Log("Unable to load " + path + " because the file can't be read", true);
                        return false;
                    }
                    Type[] types = library.GetTypes();
                    Type type = library.GetType("wmib.RegularModule");
                    Type pluginInfo = null;
                    foreach (Type curr in types)
                    {
                        if (curr.IsAssignableFrom(type))
                        {
                            pluginInfo = curr;
                            break;
                        }
                    }
                    if (pluginInfo == null)
                    {
                        Program.Log("Unable to load " + path + " because the library contains no module", true);
                        return false;
                    }


                    Module _plugin = (Module)Activator.CreateInstance(pluginInfo);

                    //Module _plugin = domain.CreateInstanceFromAndUnwrap(path, "wmib.RegularModule") as Module;

                    _plugin.ParentDomain = core.domain;
                    if (!_plugin.Construct())
                    {
                        core.Log("Invalid module", true);
                        _plugin.Exit();
                        return false;
                    }

                    lock (Domains)
                    {
                        //Domains.Add(_plugin, domain);
                    }

                    InitialiseMod(_plugin);
                    return true;
                }
                Program.Log("Unable to load " + path + " because the file can't be read", true);
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return false;
        }

        public static void SearchMods()
        {
            if (Directory.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                + Path.DirectorySeparatorChar + "modules"))
            {
                foreach (string dll in Directory.GetFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    + Path.DirectorySeparatorChar + "modules", "*.bin"))
                {
                    LoadMod(dll);
                }
            }
            Program.Log("Modules loaded");
        }

        public static Module getModule(string name)
        {
            lock (Module.module)
            {
                foreach (Module module in Module.module)
                {
                    if (module.Name == name)
                    {
                        return module;
                    }
                }
            }
            return null;
        }
    }
}
