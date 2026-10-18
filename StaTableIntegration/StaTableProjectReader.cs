using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// StaTable の XML (design.xml / cooking_heater_controller.xml) を
    /// StaTableProject モデルに変換する。
    /// 対応: canonical XML (v2.2 〜 v3.8)
    /// </summary>
    public class StaTableProjectReader
    {
        // ==============================================================
        // Public API
        // ==============================================================

        /// <summary>ファイルから読み込む</summary>
        public StaTableParseResult ParseFile(string path)
        {
            var result = new StaTableParseResult();
            if (string.IsNullOrEmpty(path))
            {
                result.Errors.Add(I18n.T("StaTableProjectReader.ErrPathRequired"));
                return result;
            }
            if (!File.Exists(path))
            {
                result.Errors.Add(I18n.T("StaTableProjectReader.ErrFileNotFound", path));
                return result;
            }

            string xml;
            try
            {
                xml = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                result.Errors.Add(I18n.T("StaTableProjectReader.ErrFileReadFailed", ex.Message));
                return result;
            }

            result = Parse(xml);
            if (result.Project != null)
            {
                result.Project.SourceFilePath = path;
            }
            return result;
        }

        /// <summary>XML 文字列から読み込む</summary>
        public StaTableParseResult Parse(string xml)
        {
            var result = new StaTableParseResult();
            if (string.IsNullOrEmpty(xml))
            {
                result.Errors.Add(I18n.T("StaTableProjectReader.ErrXmlEmpty"));
                return result;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                result.Errors.Add(I18n.T("StaTableProjectReader.ErrXmlParseFailed", ex.Message));
                return result;
            }

            var root = doc.Root;
            if (root == null || root.Name.LocalName != "Project")
            {
                result.Errors.Add(I18n.T("StaTableProjectReader.ErrProjectRootMissing"));
                return result;
            }

            var project = new StaTableProject
            {
                Name = (string)root.Attribute("name") ?? "",
                SourceXmlHash = ComputeSha256(xml),
            };

            // ProjectSettings
            var codegen = root.Element("ProjectSettings") != null
                ? root.Element("ProjectSettings").Element("CodeGeneration")
                : null;
            if (codegen != null)
            {
                project.Settings = ParseSettings(codegen);
            }
            else
            {
                result.Warnings.Add(I18n.T("StaTableProjectReader.WarnCodeGenerationMissing"));
            }

            // GlobalDefinitions
            var globalDefs = root.Element("GlobalDefinitions");
            if (globalDefs != null)
            {
                project.GlobalDefinitions = ParseGlobalDefinitions(globalDefs);
            }

            // SharedLibraries
            var sharedLibs = root.Element("SharedLibraries");
            if (sharedLibs != null)
            {
                project.SharedLibraries = ParseSharedLibraries(sharedLibs);
            }

            // Tabs
            var tabs = root.Elements("Tab").ToList();
            if (tabs.Count == 0)
            {
                result.Warnings.Add(I18n.T("StaTableProjectReader.WarnTabMissing"));
            }
            foreach (var tab in tabs)
            {
                var sm = ParseTab(tab);
                if (sm != null)
                {
                    project.Tabs.Add(sm);
                }
                else
                {
                    result.Warnings.Add(I18n.T("StaTableProjectReader.WarnTabParseFailed", (string)tab.Attribute("name") ?? "(no name)"));
                }
            }

            result.Project = project;
            return result;
        }

        // ==============================================================
        // Private: Settings
        // ==============================================================

        private StaTableProjectSettings ParseSettings(XElement e)
        {
            return new StaTableProjectSettings
            {
                ProjectName = (string)e.Attribute("project_name") ?? "",
                TableType = (string)e.Attribute("table_type") ?? "",
                GenerationStyle = (string)e.Attribute("generation_style") ?? "",
                OsType = (string)e.Attribute("os_type") ?? "",
                FolderStructure = (string)e.Attribute("folder_structure") ?? "",
                GenerateSuperInclude = ParseBool((string)e.Attribute("generate_super_include")),
                SuperIncludeFile = (string)e.Attribute("super_include_file") ?? "",
                MaxConsecutivePendingEvents = ParseInt((string)e.Attribute("max_consecutive_pending_events"), 16),
            };
        }

        // ==============================================================
        // Private: GlobalDefinitions
        // ==============================================================

        private StaTableGlobalDefinitions ParseGlobalDefinitions(XElement e)
        {
            var defs = new StaTableGlobalDefinitions();

            var vars = e.Element("SystemVariables");
            if (vars != null)
            {
                foreach (var v in vars.Elements("Variable"))
                {
                    defs.SystemVariables.Add(new StaTableVariable
                    {
                        Name = (string)v.Attribute("name") ?? "",
                        Type = (string)v.Attribute("type") ?? "",
                        Unit = (string)v.Attribute("unit") ?? "",
                        DefaultValue = (string)v.Attribute("default_value") ?? "",
                        Group = (string)v.Attribute("group") ?? "",
                        Description = (string)v.Attribute("description") ?? "",
                        Title = (string)v.Attribute("title") ?? "",
                        ArraySize = ParseInt((string)v.Attribute("array_size"), 0),
                    });
                }
            }

            var flags = e.Element("EventFlags");
            if (flags != null)
            {
                foreach (var f in flags.Elements("Flag"))
                {
                    defs.EventFlags.Add(new StaTableEventFlag
                    {
                        Name = (string)f.Attribute("name") ?? "",
                        MinValue = ParseInt((string)f.Attribute("min_value"), 0),
                        MaxValue = ParseInt((string)f.Attribute("max_value"), 0),
                        Group = (string)f.Attribute("group") ?? "",
                        Description = (string)f.Attribute("description") ?? "",
                        Title = (string)f.Attribute("title") ?? "",
                    });
                }
            }

            var intrs = e.Element("Interrupts");
            if (intrs != null)
            {
                foreach (var it in intrs.Elements("Interrupt"))
                {
                    var intr = new StaTableInterrupt
                    {
                        Name = (string)it.Attribute("name") ?? "",
                        Description = (string)it.Attribute("description") ?? "",
                        EventNames = (string)it.Attribute("event_names") ?? "",
                        IsTimer = ParseBool((string)it.Attribute("is_timer")),
                        Title = (string)it.Attribute("title") ?? "",
                    };
                    foreach (var a in it.Elements("Action"))
                    {
                        intr.Actions.Add(new StaTableInterruptAction
                        {
                            Condition = (string)a.Attribute("condition") ?? "",
                            Action = (string)a.Attribute("action") ?? "",
                        });
                    }
                    foreach (var uv in it.Elements("UsedVariable"))
                    {
                        intr.UsedVariables.Add((string)uv.Attribute("name") ?? "");
                    }
                    defs.Interrupts.Add(intr);
                }
            }

            var timers = e.Element("TimerBase");
            if (timers != null)
            {
                foreach (var t in timers.Elements("Timer"))
                {
                    defs.TimerBase.Add(new StaTableTimer
                    {
                        VariableName = (string)t.Attribute("variable_name") ?? "",
                        Unit = (string)t.Attribute("unit") ?? "",
                        DataType = (string)t.Attribute("data_type") ?? "",
                        Title = (string)t.Attribute("title") ?? "",
                        InterruptName = (string)t.Attribute("interrupt_name") ?? "",
                    });
                }
            }

            return defs;
        }

        // ==============================================================
        // Private: SharedLibraries
        // ==============================================================

        private StaTableSharedLibraries ParseSharedLibraries(XElement e)
        {
            var libs = new StaTableSharedLibraries();
            var rfl = e.Element("RoleFunctionLibrary");
            if (rfl != null)
            {
                foreach (var rf in rfl.Elements("RoleFunction"))
                {
                    libs.RoleFunctionLibrary.Add(ParseRoleFunction(rf));
                }
            }
            return libs;
        }

        private StaTableRoleFunction ParseRoleFunction(XElement e)
        {
            return new StaTableRoleFunction
            {
                Name = (string)e.Attribute("name") ?? "",
                Namespace = (string)e.Attribute("namespace") ?? "",
                Title = (string)e.Attribute("title") ?? "",
                Description = (string)e.Attribute("description") ?? "",
                ReturnType = (string)e.Attribute("return_type") ?? "int",
                Arg1Type = (string)e.Attribute("arg1_type") ?? "",
                Arg1Name = (string)e.Attribute("arg1_name") ?? "",
                Arg2Type = (string)e.Attribute("arg2_type") ?? "",
                Arg2Name = (string)e.Attribute("arg2_name") ?? "",
                UsedGlobalVars = SplitList((string)e.Attribute("used_global_vars")),
                UsedEvents = SplitList((string)e.Attribute("used_events")),
                UsedLiterals = SplitList((string)e.Attribute("used_literals")),
            };
        }

        // ==============================================================
        // Private: Tab / StateMachine
        // ==============================================================

        private StaTableStateMachine ParseTab(XElement tab)
        {
            var smElem = tab.Element("StateMachine");
            if (smElem == null) return null;

            var sm = new StaTableStateMachine
            {
                LayerName = (string)smElem.Attribute("layer_name") ?? "",
                LayerPriority = ParseInt((string)smElem.Attribute("layer_priority"), 5),
                LayerDescription = (string)smElem.Attribute("layer_description") ?? "",
                InitialState = (string)smElem.Attribute("initial") ?? "",
            };

            // States
            var statesElem = smElem.Element("States");
            if (statesElem != null)
            {
                foreach (var s in statesElem.Elements("State"))
                {
                    sm.States.Add(new StaTableState
                    {
                        Name = (string)s.Attribute("name") ?? "",
                        Type = (string)s.Attribute("type") ?? "normal",
                        Parent = (string)s.Attribute("parent") ?? "",
                        Do = (string)s.Attribute("do") ?? "",
                        Description = (string)s.Attribute("description") ?? "",
                    });
                }
            }

            // Events
            var eventsElem = smElem.Element("Events");
            if (eventsElem != null)
            {
                foreach (var ev in eventsElem.Elements("Event"))
                {
                    sm.Events.Add(new StaTableEvent
                    {
                        Name = (string)ev.Attribute("name") ?? "",
                        Id = ParseInt((string)ev.Attribute("id"), 0),
                        Kind = (string)ev.Attribute("kind") ?? "",
                        Params = (string)ev.Attribute("params") ?? "",
                        Priority = ParseInt((string)ev.Attribute("priority"), 0),
                        Description = (string)ev.Attribute("description") ?? "",
                        DeliveryType = (string)ev.Attribute("delivery_type") ?? "direct",
                        SourceLayer = (string)ev.Attribute("source_layer") ?? "",
                        DataType = (string)ev.Attribute("data_type") ?? "",
                        DataName = (string)ev.Attribute("data_name") ?? "",
                        Title = (string)ev.Attribute("title") ?? "",
                    });
                }
            }

            // Transitions
            var transElem = smElem.Element("Transitions");
            if (transElem != null)
            {
                foreach (var t in transElem.Elements("Transition"))
                {
                    sm.Transitions.Add(new StaTableTransition
                    {
                        Source = (string)t.Attribute("source") ?? "",
                        Event = (string)t.Attribute("event") ?? "",
                        Condition = (string)t.Attribute("condition") ?? "",
                        Action = (string)t.Attribute("action") ?? "",
                        Target = (string)t.Attribute("target") ?? "",
                        TransitionType = (string)t.Attribute("transition_type") ?? "",
                        Title = (string)t.Attribute("title") ?? "",
                        HasElse = ParseBool((string)t.Attribute("has_else")),
                        ElseTarget = (string)t.Attribute("else_target") ?? "",
                        EarlyReturn = ParseBool((string)t.Attribute("early_return")),
                        Label = (string)t.Attribute("label") ?? "",
                    });
                }
            }

            // RoleFunctions (層ローカル)
            var rfsElem = smElem.Element("RoleFunctions");
            if (rfsElem != null)
            {
                foreach (var rf in rfsElem.Elements("RoleFunction"))
                {
                    sm.RoleFunctions.Add(ParseRoleFunction(rf));
                }
            }

            return sm;
        }

        // ==============================================================
        // Private: Utilities
        // ==============================================================

        private static int ParseInt(string s, int defaultValue)
        {
            int v;
            if (int.TryParse(s, out v)) return v;
            return defaultValue;
        }

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.Equals("true", StringComparison.OrdinalIgnoreCase)
                || s == "1";
        }

        private static List<string> SplitList(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return new List<string>();
            return s.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
        }

        private static string ComputeSha256(string text)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}