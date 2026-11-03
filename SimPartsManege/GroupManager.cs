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
    }
}
