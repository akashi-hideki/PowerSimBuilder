using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// Phase 3/4: multi-select group save/load.
    /// A .psbgroup file is a ProjectFileV2 JSON subset containing only the
    /// selected components and the connections fully inside the selection.
    /// Positions are stored RELATIVE to the top-left of the selection bounding
    /// box so that loading places the group near the drop point.
    /// </summary>
    public static class GroupManager
    {
        public const string Extension = ".psbgroup";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// Build a ProjectFileV2 from the given selection. Only connections
        /// whose both endpoints are in the selection are included.
        /// </summary>
        public static ProjectFileV2 Extract(
            string groupName,
            IEnumerable<ComponentControl> selectedComponents,
            IEnumerable<Connection> allConnections)
        {
            if (selectedComponents == null) throw new ArgumentNullException(nameof(selectedComponents));
            var comps = selectedComponents.Where(c => c != null).ToList();
            if (comps.Count == 0) throw new InvalidOperationException("No components selected.");

            int minX = comps.Min(c => c.Location.X);
            int minY = comps.Min(c => c.Location.Y);

            var project = new ProjectFileV2
            {
                SchemaVersion = "2.0",
                Metadata = new ProjectMetadata
                {
                    Name = string.IsNullOrEmpty(groupName) ? "group" : groupName,
                    Description = "Multi-select group",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    CreatedBy = "PowerSimBuilder",
                    Dt = 0.001,
                    TEnd = 5.0,
                },
            };

            foreach (var c in comps)
            {
                project.Components.Add(new ComponentInstanceV2
                {
                    InstanceName = c.InstanceName ?? "",
                    Descriptor = c.Descriptor,
                    Parameters = c.Parameters.ToDictionary(p => p.Key, p => p.Value),
                    Location = new PointData
                    {
                        X = c.Location.X - minX,
                        Y = c.Location.Y - minY,
                    },
                });
            }

            var set = new HashSet<ComponentControl>(comps);
            if (allConnections != null)
            {
                foreach (var conn in allConnections)
                {
                    if (conn == null) continue;
                    if (!set.Contains(conn.StartComponent)) continue;
                    if (!set.Contains(conn.EndComponent)) continue;
                    project.Connections.Add(new ConnectionV2
                    {
                        From = conn.StartComponent.InstanceName + "." + conn.StartPort,
                        To = conn.EndComponent.InstanceName + "." + conn.EndPort,
                    });
                }
            }

            return project;
        }

        public static void Save(string path, ProjectFileV2 group)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("path is empty", nameof(path));
            if (group == null) throw new ArgumentNullException(nameof(group));
            var json = JsonSerializer.Serialize(group, JsonOptions);
            File.WriteAllText(path, json, new System.Text.UTF8Encoding(false));
        }

        public static ProjectFileV2 Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Group file not found", path);
            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var group = JsonSerializer.Deserialize<ProjectFileV2>(json, JsonOptions);
            if (group == null) throw new InvalidDataException("Failed to parse group file");
            return group;
        }

        /// <summary>
        /// Phase 4: load a .psbgroup file and append its components and internal
        /// connections to the current canvas. Group positions are offset by <paramref name="offset"/>.
        /// Instance names are made unique by appending _2, _3, ... when they collide.
        /// Returns (componentsAdded, connectionsAdded).
        /// </summary>
        public static (int components, int connections) LoadAndAppend(
            string path,
            CanvasManager manager,
            System.Drawing.Point offset)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            var group = Load(path);

            // Collect existing instance names for uniqueness.
            var existing = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in manager.GetAllComponents())
                if (c?.InstanceName != null) existing.Add(c.InstanceName);

            var byName = new Dictionary<string, ComponentControl>(StringComparer.Ordinal);
            int addedComponents = 0;
            int addedConnections = 0;

            manager.BeginBulkInsert();
            try
            {
                foreach (var ci in group.Components ?? new List<ComponentInstanceV2>())
                {
                    var uniqueName = MakeUnique(ci.InstanceName, existing);
                    existing.Add(uniqueName);

                    var control = new ComponentControl(ci.Descriptor)
                    {
                        InstanceName = uniqueName,
                        Location = new System.Drawing.Point(
                            (ci.Location?.X ?? 0) + offset.X,
                            (ci.Location?.Y ?? 0) + offset.Y),
                    };
                    control.Name = uniqueName;
                    if (ci.Parameters != null)
                    {
                        foreach (var kv in ci.Parameters)
                        {
                            if (control.Parameters.ContainsKey(kv.Key))
                                control.Parameters[kv.Key] = kv.Value;
                            else
                                control.Parameters[kv.Key] = kv.Value;
                        }
                    }
                    manager.AppendComponentNoUndo(control);
                    byName[ci.InstanceName ?? ""] = control;
                    addedComponents++;
                }

                foreach (var cv in group.Connections ?? new List<ConnectionV2>())
                {
                    var (srcName, srcPort) = SplitRef(cv.From);
                    var (dstName, dstPort) = SplitRef(cv.To);
                    if (srcName == null || dstName == null) continue;
                    if (!byName.TryGetValue(srcName, out var src)) continue;
                    if (!byName.TryGetValue(dstName, out var dst)) continue;
                    manager.AppendConnectionNoUndo(src, srcPort, dst, dstPort);
                    addedConnections++;
                }
            }
            finally
            {
                manager.EndBulkInsert();
            }

            return (addedComponents, addedConnections);
        }

        private static string MakeUnique(string baseName, HashSet<string> existing)
        {
            var name = string.IsNullOrEmpty(baseName) ? "comp" : baseName;
            if (!existing.Contains(name)) return name;
            int i = 2;
            while (existing.Contains(name + "_" + i)) i++;
            return name + "_" + i;
        }

        private static (string instance, string port) SplitRef(string reference)
        {
            if (string.IsNullOrEmpty(reference)) return (null, null);
            int idx = reference.LastIndexOf('.');
            if (idx <= 0 || idx == reference.Length - 1) return (null, null);
            return (reference.Substring(0, idx), reference.Substring(idx + 1));
        }
    }
}
